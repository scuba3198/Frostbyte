// Frostbyte sensors - CPU/GPU/SSD temperature, CPU speed and battery for the main window.
//
// CPU temperature is read the accurate way: the Intel digital thermal sensor (MSR 0x1B1 package, 0x1A2 TjMax) through
// the PawnIO driver (https://pawnio.eu, signed, installed separately) using its IntelMSR module (LGPL-2.1, embedded as the
// "IntelMSR.bin" resource). Without PawnIO it falls back to the coarse Windows ACPI thermal-zone value, shown with a "~".
// GPU temperature comes from the graphics driver (D3DKMT), SSD temperature from the drive's own SMART data.
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

static class Native
{
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern SafeFileHandle CreateFileW(string name, uint access, uint share, IntPtr sa, uint disp, uint flags, IntPtr tmpl);
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool DeviceIoControl(SafeFileHandle h, uint code, byte[] inBuf, int inLen, byte[] outBuf, int outLen, out int ret, IntPtr ov);
    [DllImport("kernel32.dll")] public static extern UIntPtr SetThreadAffinityMask(IntPtr thread, UIntPtr mask);
    [DllImport("kernel32.dll")] public static extern IntPtr GetCurrentThread();

    public const uint GENERIC_READ = 0x80000000, GENERIC_WRITE = 0x40000000, SHARE_RW = 3, OPEN_EXISTING = 3;
}

// Minimal PawnIO client: talks to the driver's device directly (same protocol the PawnIO library uses).
sealed class PawnIo : IDisposable
{
    const uint DeviceType = 41394u << 16;
    const uint IoctlLoad = DeviceType | (0x821 << 2), IoctlExecute = DeviceType | (0x841 << 2);
    const int FnNameLen = 32;
    SafeFileHandle h;

    public static bool IsInstalled()
    {
        try
        {
            foreach (var view in new[] { Microsoft.Win32.RegistryView.Registry64, Microsoft.Win32.RegistryView.Registry32 })
                using (var b = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, view))
                using (var k = b.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\PawnIO"))
                    if (k != null) return true;
        }
        catch { }
        return false;
    }

    public static PawnIo Open(byte[] module)
    {
        if (module == null || module.Length == 0) return null;
        var h = Native.CreateFileW(@"\\?\GLOBALROOT\Device\PawnIO", Native.GENERIC_READ | Native.GENERIC_WRITE, Native.SHARE_RW, IntPtr.Zero, Native.OPEN_EXISTING, 0, IntPtr.Zero);
        if (h.IsInvalid) return null;
        int ret;
        if (!Native.DeviceIoControl(h, IoctlLoad, module, module.Length, null, 0, out ret, IntPtr.Zero)) { h.Dispose(); return null; }
        return new PawnIo { h = h };
    }

    public bool Execute(string fn, long[] input, long[] output)
    {
        var inp = new byte[FnNameLen + input.Length * 8];
        var nm = Encoding.ASCII.GetBytes(fn); Buffer.BlockCopy(nm, 0, inp, 0, Math.Min(nm.Length, FnNameLen - 1));
        Buffer.BlockCopy(input, 0, inp, FnNameLen, input.Length * 8);
        var outp = new byte[output.Length * 8]; int ret;
        if (!Native.DeviceIoControl(h, IoctlExecute, inp, inp.Length, outp, outp.Length, out ret, IntPtr.Zero)) return false;
        Buffer.BlockCopy(outp, 0, output, 0, Math.Min(ret, outp.Length));
        return ret >= 8;
    }
    public void Dispose() { if (h != null) h.Dispose(); h = null; }
}

class SensorReadings
{
    public double CpuTemp = double.NaN; public bool CpuApprox;
    public double CpuGhz = double.NaN, CpuMaxGhz = double.NaN;
    public double GpuTemp = double.NaN, SsdTemp = double.NaN;
    public int Battery = -1; public bool Charging, HasBattery;
    public string SsdName = "SSD", GpuName = "GPU";
}

static class Sensors
{
    static PawnIo msr; static bool msrTried; static int tjMax = 100;
    static PerformanceCounter perf; static double baseGhz = double.NaN;
    static string ssdName;

    // true when the accurate (driver) path is active; the UI uses it to offer the PawnIO download
    public static bool CpuAccurate { get { return msr != null; } }
    public static bool PawnIoMissing { get { return !PawnIo.IsInstalled(); } }

    static byte[] LoadModule()
    {
        try
        {
            using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("IntelMSR.bin"))
            {
                if (s == null) return null;
                var m = new MemoryStream(); s.CopyTo(m); return m.ToArray();
            }
        }
        catch { return null; }
    }

    static bool ReadMsr(uint reg, out long val)
    {
        var o = new long[1]; val = 0;
        // the thermal MSRs are per-core; pin to CPU 0 for a stable package reading
        var old = Native.SetThreadAffinityMask(Native.GetCurrentThread(), (UIntPtr)1);
        try { if (!msr.Execute("ioctl_read_msr", new long[] { reg }, o)) return false; val = o[0]; return true; }
        finally { if (old != UIntPtr.Zero) Native.SetThreadAffinityMask(Native.GetCurrentThread(), old); }
    }

    static double CpuTempMsr()
    {
        if (!msrTried)
        {
            msrTried = true;
            try
            {
                msr = PawnIo.Open(LoadModule());
                long t; if (msr != null && ReadMsr(0x1A2, out t)) { int v = (int)((t >> 16) & 0xFF); if (v > 0) tjMax = v; }
            }
            catch { msr = null; }
        }
        if (msr == null) return double.NaN;
        long s;
        if (!ReadMsr(0x1B1, out s) || ((s >> 31) & 1) == 0) return double.NaN;   // bit 31 = reading valid
        return tjMax - ((s >> 16) & 0x7F);                                       // bits 22:16 = degrees below TjMax
    }

    static double AcpiTemp()
    {
        try
        {
            var cat = new PerformanceCounterCategory("Thermal Zone Information");
            double max = double.NaN;
            foreach (var inst in cat.GetInstanceNames())
                using (var c = new PerformanceCounter("Thermal Zone Information", "Temperature", inst))
                { double v = c.NextValue() - 273.15; if (v > 0 && v < 130 && (double.IsNaN(max) || v > max)) max = v; }
            return max;
        }
        catch { return double.NaN; }
    }

    static void CpuSpeed(SensorReadings r)
    {
        try
        {
            if (perf == null)
            {
                perf = new PerformanceCounter("Processor Information", "Processor Frequency", "_Total");
                perf.NextValue();
                using (var k = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0"))
                    baseGhz = Convert.ToDouble(k.GetValue("~MHz")) / 1000.0;
            }
            r.CpuGhz = perf.NextValue() / 1000.0; r.CpuMaxGhz = baseGhz;
        }
        catch { }
    }

    public static SensorReadings Read()
    {
        var r = new SensorReadings();
        r.CpuTemp = CpuTempMsr();
        if (double.IsNaN(r.CpuTemp)) { r.CpuTemp = AcpiTemp(); r.CpuApprox = !double.IsNaN(r.CpuTemp); }
        CpuSpeed(r);
        r.GpuTemp = Gpu.Temperature();
        r.SsdTemp = Ssd.Temperature(out ssdName); if (ssdName != null) r.SsdName = ssdName;
        try
        {
            var ps = System.Windows.Forms.SystemInformation.PowerStatus;
            r.HasBattery = ps.BatteryChargeStatus != System.Windows.Forms.BatteryChargeStatus.NoSystemBattery;
            if (r.HasBattery && ps.BatteryLifePercent <= 1f) r.Battery = (int)Math.Round(ps.BatteryLifePercent * 100);
            r.Charging = ps.PowerLineStatus == System.Windows.Forms.PowerLineStatus.Online;
        }
        catch { }
        return r;
    }
}

// GPU temperature through the graphics driver's performance data (D3DKMT), no vendor SDK needed.
static class Gpu
{
    [StructLayout(LayoutKind.Sequential)] struct Luid { public uint Low; public int High; }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct AdapterInfo { public uint Handle; public Luid Luid; public uint NumSources; public int Software; }
    [StructLayout(LayoutKind.Sequential)]
    struct EnumAdapters2 { public uint Num; public IntPtr Adapters; }
    [StructLayout(LayoutKind.Sequential)] struct CloseAdapter { public uint Handle; }
    [StructLayout(LayoutKind.Sequential)]
    struct QueryAdapterInfo { public uint Handle; public int Type; public IntPtr Data; public uint Size; }
    [StructLayout(LayoutKind.Sequential)]
    struct PerfData
    {
        public uint PhysicalAdapterIndex; public ulong MemoryFrequency, MaxMemoryFrequency, MaxMemoryFrequencyOC,
            MemoryBandwidth, PCIEBandwidth, FanRPM, Power, Temperature, PowerStateOverhead;
    }
    [DllImport("gdi32.dll")] static extern int D3DKMTEnumAdapters2(ref EnumAdapters2 e);
    [DllImport("gdi32.dll")] static extern int D3DKMTQueryAdapterInfo(ref QueryAdapterInfo q);
    [DllImport("gdi32.dll")] static extern int D3DKMTCloseAdapter(ref CloseAdapter c);
    const int KmtQaiAdapterPerfData = 62;

    public static double Temperature()
    {
        try
        {
            var e = new EnumAdapters2();
            if (D3DKMTEnumAdapters2(ref e) != 0 || e.Num == 0) return double.NaN;
            int sz = Marshal.SizeOf(typeof(AdapterInfo));
            e.Adapters = Marshal.AllocHGlobal(sz * (int)e.Num);
            try
            {
                if (D3DKMTEnumAdapters2(ref e) != 0) return double.NaN;
                double best = double.NaN;
                for (int i = 0; i < e.Num; i++)
                {
                    var ai = (AdapterInfo)Marshal.PtrToStructure(new IntPtr(e.Adapters.ToInt64() + i * sz), typeof(AdapterInfo));
                    if (ai.Software == 0)
                    {
                        var pd = new PerfData(); IntPtr p = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(PerfData)));
                        try
                        {
                            Marshal.StructureToPtr(pd, p, false);
                            var q = new QueryAdapterInfo { Handle = ai.Handle, Type = KmtQaiAdapterPerfData, Data = p, Size = (uint)Marshal.SizeOf(typeof(PerfData)) };
                            if (D3DKMTQueryAdapterInfo(ref q) == 0)
                            {
                                pd = (PerfData)Marshal.PtrToStructure(p, typeof(PerfData));
                                double t = pd.Temperature / 10.0;
                                if (t > 0 && t < 150 && (double.IsNaN(best) || t > best)) best = t;
                            }
                        }
                        finally { Marshal.FreeHGlobal(p); }
                    }
                    var c = new CloseAdapter { Handle = ai.Handle }; D3DKMTCloseAdapter(ref c);
                }
                return best;
            }
            finally { Marshal.FreeHGlobal(e.Adapters); }
        }
        catch { return double.NaN; }
    }
}

// SSD temperature: the drive's own temperature property via IOCTL_STORAGE_QUERY_PROPERTY (works for NVMe and SATA).
static class Ssd
{
    const uint IoctlStorageQueryProperty = 0x2D1400;
    const int StorageDeviceTemperatureProperty = 52, PropertyStandardQuery = 0;

    public static double Temperature(out string name)
    {
        name = null;
        double best = double.NaN;
        for (int i = 0; i < 4; i++)
        {
            try
            {
                using (var h = Native.CreateFileW(@"\\.\PhysicalDrive" + i, 0, Native.SHARE_RW, IntPtr.Zero, Native.OPEN_EXISTING, 0, IntPtr.Zero))
                {
                    if (h.IsInvalid) continue;
                    var q = new byte[12]; BitConverter.GetBytes(StorageDeviceTemperatureProperty).CopyTo(q, 0); BitConverter.GetBytes(PropertyStandardQuery).CopyTo(q, 4);
                    var o = new byte[512]; int ret;
                    if (!Native.DeviceIoControl(h, IoctlStorageQueryProperty, q, q.Length, o, o.Length, out ret, IntPtr.Zero) || ret < 24) continue;
                    // STORAGE_TEMPERATURE_DATA_DESCRIPTOR: Version, Size, CriticalTemp, WarningTemp, InfoCount(u16), Reserved, then per-info records
                    int count = BitConverter.ToUInt16(o, 12);
                    if (count < 1 || ret < 24 + 16) continue;
                    short t = BitConverter.ToInt16(o, 24 + 2);   // STORAGE_TEMPERATURE_INFO: Index(u16), Temperature(i16), ...
                    if (t > 0 && t < 150 && (double.IsNaN(best) || t > best)) best = t;
                }
            }
            catch { }
        }
        return best;
    }
}

