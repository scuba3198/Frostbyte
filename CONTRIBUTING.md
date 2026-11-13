# Contributing to Frostbyte

Thanks for helping out! The workflow is the standard **GitHub Flow**: `main` is always releasable, every change
arrives through a short-lived branch and a pull request, and releases are cut automatically.

## Making a change

1. Branch from `main`: `git switch -c fix/tray-icon-blurry` (use `feat/…`, `fix/…`, `docs/…`, `chore/…`).
2. Make your change and build it locally: `powershell -File build.ps1` (add `-Installer` for the installer).
3. Open a pull request. **CI must pass** (it builds the app and the installer).
4. **Title the PR with a [Conventional Commit](https://www.conventionalcommits.org/) message** – it becomes the
   squash-commit message on `main` and decides the next version:

   | Title starts with | Effect | Example |
   |---|---|---|
   | `fix:` | patch release (1.2.0 → 1.2.1) | `fix: tray icon blurry on 150% scaling` |
   | `feat:` | minor release (1.2.0 → 1.3.0) | `feat: check for updates` |
   | `feat!:` or a `BREAKING CHANGE:` footer | major release (1.2.0 → 2.0.0) | `feat!: drop Windows 10 support` |
   | `docs:` `chore:` `ci:` `refactor:` `test:` `style:` | no release by itself | `docs: clarify install steps` |

5. Squash-merge the PR. Delete the branch.

## How releases happen (nobody bumps versions by hand)

- After each merge to `main`, **release-please** opens or updates a pull request called
  **"chore(main): release x.y.z"**. It collects everything merged since the last release, bumps `version.txt` and
  writes `CHANGELOG.md`.
- When you're happy with what's in it, **merge that pull request**. That creates the `vx.y.z` tag and the GitHub
  Release, and CI builds `Frostbyte-Setup-x.y.z.exe` and attaches it. Installed copies will offer the update.
- Don't edit `version.txt`, `CHANGELOG.md` or `.release-please-manifest.json` yourself.

## Project layout

See *How it works → The pieces* in the [README](README.md#how-it-works).
