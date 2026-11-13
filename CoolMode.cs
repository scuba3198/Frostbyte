// CoolMode - switches a Windows power plan that keeps the CPU out of turbo boost.
//
// Only documented Windows power settings are used (powrprof.dll); the result can be checked on any PC
// with `powercfg /qh <scheme> SUB_PROCESSOR`. Independent code, not affiliated with any other product.
//   enable : remember the active scheme, duplicate SCHEME_BALANCED (falls back to the active
//            scheme if Balanced is missing) into a fixed-GUID scheme, set its name/description,
//            write exactly five processor values, make it the active scheme.
//   disable: re-activate the remembered scheme (Balanced as fallback) and delete the scheme.
// The five values (subgroup 54533251-82be-4824-96c1-47b60b740d00, AC and DC unless noted):
//   be337238-0d82-4146-a960-4f3749d470c7  boost mode      = 0   (AC, DC)
//   893dee8e-2bef-41e0-89c6-b55d0929964c  min proc state  = 5   (AC only)
//   bc5038f7-23e0-4960-96da-33abaf5935ec  max proc state  = 99  (AC, DC)  -> no turbo
// This tool uses its own fixed scheme GUID so it can coexist with other power plans.
// Usage: CoolMode.exe on | off | toggle | status      (needs administrator)
using System;
using System.IO;
using System.Runtime.InteropServices;

static class CoolMode
{
    const string Name = "Frostbyte";
    const string Desc = "Special mode that significantly reduces CPU temperature and extends battery life.";
    static readonly Guid SchemeGuid = new Guid("c001c001-0000-4c00-8000-00c001c001c0");
    static readonly Guid Balanced = new Guid("381b4222-f694-41f0-9685-ff5bb260df2e");
    static readonly Guid SubProcessor = new Guid("54533251-82be-4824-96c1-47b60b740d00");
    static readonly Guid BoostMode = new Guid("be337238-0d82-4146-a960-4f3749d470c7");
    static readonly Guid ThrottleMin = new Guid("893dee8e-2bef-41e0-89c6-b55d0929964c");
    static readonly Guid ThrottleMax = new Guid("bc5038f7-23e0-4960-96da-33abaf5935ec");
    const uint ERROR_ALREADY_EXISTS = 0xB7;

    [DllImport("powrprof.dll")] static extern uint PowerGetActiveScheme(IntPtr r, out IntPtr g);
    [DllImport("powrprof.dll")] static extern uint PowerSetActiveScheme(IntPtr r, ref Guid g);
    [DllImport("powrprof.dll")] static extern uint PowerDuplicateScheme(IntPtr r, ref Guid src, ref IntPtr dst);
    [DllImport("powrprof.dll")] static extern uint PowerDeleteScheme(IntPtr r, ref Guid g);
    [DllImport("powrprof.dll")] static extern uint PowerWriteACValueIndex(IntPtr r, ref Guid s, ref Guid sub, ref Guid set, uint v);
    [DllImport("powrprof.dll")] static extern uint PowerWriteDCValueIndex(IntPtr r, ref Guid s, ref Guid sub, ref Guid set, uint v);
    [DllImport("powrprof.dll", CharSet = CharSet.Unicode)] static extern uint PowerWriteFriendlyName(IntPtr r, ref Guid s, IntPtr sub, IntPtr set, string n, uint size);
    [DllImport("powrprof.dll", CharSet = CharSet.Unicode)] static extern uint PowerWriteDescription(IntPtr r, ref Guid s, IntPtr sub, IntPtr set, string n, uint size);
    [DllImport("kernel32.dll")] static extern IntPtr LocalFree(IntPtr h);

    static readonly string StateFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "CoolMode", "previous.txt");

    static void Check(uint rc, string what) { if (rc != 0) throw new Exception(what + " failed, error " + rc); }
    static Guid Active()
    {
        IntPtr p; Check(PowerGetActiveScheme(IntPtr.Zero, out p), "GetActiveScheme");
        Guid g = (Guid)Marshal.PtrToStructure(p, typeof(Guid)); LocalFree(p); return g;
    }
    internal static bool IsOn() { return Active() == SchemeGuid; }
    static uint Len(string s) { return (uint)((s.Length + 1) * 2); }

    internal static void Enable()
    {
        Guid cur = Active();
        if (cur == SchemeGuid) { Console.WriteLine("Cool mode already ON."); return; }

        // Create scheme: duplicate Balanced into our fixed GUID (fallback: active scheme).
        Guid mine = SchemeGuid;
        IntPtr dst = Marshal.AllocHGlobal(16); Marshal.StructureToPtr(mine, dst, false);
        Guid src = Balanced;
        uint rc = PowerDuplicateScheme(IntPtr.Zero, ref src, ref dst);
        if (rc == 2) { src = cur; rc = PowerDuplicateScheme(IntPtr.Zero, ref src, ref dst); }
        Marshal.FreeHGlobal(dst); // dst still points at our buffer when a GUID was preset
        if (rc != 0 && rc != ERROR_ALREADY_EXISTS) throw new Exception("DuplicateScheme failed, error " + rc);

        Check(PowerWriteFriendlyName(IntPtr.Zero, ref mine, IntPtr.Zero, IntPtr.Zero, Name, Len(Name)), "WriteFriendlyName");
        Check(PowerWriteDescription(IntPtr.Zero, ref mine, IntPtr.Zero, IntPtr.Zero, Desc, Len(Desc)), "WriteDescription");

        // The processor values written to the new scheme.
        Guid sub = SubProcessor, boost = BoostMode, min = ThrottleMin, max = ThrottleMax;
        uint sum = 0;
        sum += PowerWriteACValueIndex(IntPtr.Zero, ref mine, ref sub, ref boost, 0);
        sum += PowerWriteDCValueIndex(IntPtr.Zero, ref mine, ref sub, ref boost, 0);
        sum += PowerWriteACValueIndex(IntPtr.Zero, ref mine, ref sub, ref min, 5);
        sum += PowerWriteDCValueIndex(IntPtr.Zero, ref mine, ref sub, ref max, 99);
        sum += PowerWriteACValueIndex(IntPtr.Zero, ref mine, ref sub, ref max, 99);
        if (sum != 0) throw new Exception("writing processor settings failed");

        Directory.CreateDirectory(Path.GetDirectoryName(StateFile));
        File.WriteAllText(StateFile, cur.ToString());
        Check(PowerSetActiveScheme(IntPtr.Zero, ref mine), "SetActiveScheme");
        Console.WriteLine("Cool mode ON  (scheme " + mine + ", previous " + cur + ")");
    }

    internal static void Disable()
    {
        if (!IsOn()) { Console.WriteLine("Cool mode already OFF."); return; }
        Guid prev = Balanced;
        try { if (File.Exists(StateFile)) prev = new Guid(File.ReadAllText(StateFile).Trim()); } catch { }
        if (prev == SchemeGuid) prev = Balanced;
        uint rc = PowerSetActiveScheme(IntPtr.Zero, ref prev);
        if (rc != 0) { prev = Balanced; Check(PowerSetActiveScheme(IntPtr.Zero, ref prev), "SetActiveScheme(Balanced)"); }
        Guid mine = SchemeGuid; PowerDeleteScheme(IntPtr.Zero, ref mine);
        try { File.Delete(StateFile); } catch { }
        Console.WriteLine("Cool mode OFF (restored " + prev + ")");
    }

    static int Main(string[] a)
    {
        try
        {
            string cmd = a.Length > 0 ? a[0].ToLowerInvariant() : "toggle";
            switch (cmd)
            {
                case "on": Enable(); break;
                case "off": Disable(); break;
                case "toggle": if (IsOn()) Disable(); else Enable(); break;
                case "status": Console.WriteLine(IsOn() ? "ON" : "OFF"); return IsOn() ? 1 : 0;
                default: Console.WriteLine("Usage: CoolMode.exe on|off|toggle|status"); return 2;
            }
            return 0;
        }
        catch (Exception e) { Console.Error.WriteLine("Error: " + e.Message + " (run as administrator)"); return 3; }
    }
}
