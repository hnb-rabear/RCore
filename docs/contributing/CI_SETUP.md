# CI setup

Two pieces of out-of-band setup are required before the CI workflows in `.github/workflows/` can run a green build. This document is the runbook for the solo maintainer.

## 1. Unity license secrets

`unity-test.yml` and `benchmark.yml` invoke `game-ci/unity-test-runner`, which needs a valid Unity license to run the Editor in batch mode. Without these secrets every PR run errors out on the licensing step.

### Adding the license secrets

In the repo on GitHub: **Settings → Secrets and variables → Actions → New repository secret**.

Add the secrets for the license tier the project actually owns:

**Personal license** (typical for solo maintainers / small studios):

- `UNITY_LICENSE` — full contents of the `.ulf` file (XML). Activate via Unity Hub → Sign In with your Unity ID, then export from `~/.local/share/unity3d/Unity/Unity_lic.ulf` (Linux/WSL) or `C:\ProgramData\Unity\Unity_lic.ulf` (Windows).
- `UNITY_EMAIL` — Unity ID email.
- `UNITY_PASSWORD` — Unity ID password.

**Pro / Plus license**:

- `UNITY_SERIAL` — Pro/Plus serial number (e.g. `SB-XXXX-XXXX-XXXX-XXXX-XXXX`).
- `UNITY_EMAIL` — Unity ID email.
- `UNITY_PASSWORD` — Unity ID password.

Either tier works — `unity-test-runner` checks `UNITY_LICENSE` first, then falls back to `UNITY_SERIAL`.

### Verifying the license

After the secrets are set, rerun a workflow from **Actions → Unity Tests → Run workflow**. Watch the first job's first step: it should report `Activated license` rather than the licensing error.

## 2. PublicAPI Roslyn analyzer

The `PublicAPI.{Shipped,Unshipped}.txt` files under each Runtime asmdef are currently review-only. To make the Roslyn analyzer enforce them in the actual compile (so adding a public member without updating `Unshipped.txt` produces a build error), the DLL needs to land in the project.

> **Status**: the DLLs are already committed under `Assets/RevCore/_Analyzers/` but the `RoslynAnalyzer` label is intentionally absent (the analyzer is dormant). Activation requires seeding `PublicAPI.Shipped.txt` first, scheduled as a v1.0 release task. See `Assets/RevCore/_Analyzers/README.md` for the full reasoning.

### Installing the analyzer

1. Download `Microsoft.CodeAnalysis.PublicApiAnalyzers` from NuGet:
   <https://www.nuget.org/packages/Microsoft.CodeAnalysis.PublicApiAnalyzers/>
2. The `.nupkg` is a zip — rename a copy to `.zip` (or `Expand-Archive` in PowerShell) and navigate to `analyzers/dotnet/`. Take both `Microsoft.CodeAnalysis.PublicApiAnalyzers.dll` and `Microsoft.CodeAnalysis.PublicApiAnalyzers.CodeFixes.dll`. (The 3.3.4 release puts these directly under `analyzers/dotnet/`, not `analyzers/dotnet/cs/`; the `cs/` folder only holds localized resource DLLs.)
3. Drop both DLLs into `Assets/RevCore/_Analyzers/` (folder already exists with a README).
4. In Unity's Project view, select each DLL one at a time. In the Inspector:
   - **Include Platforms**: untick every platform (critical — analyzers must never ship into a runtime build).
   - **Asset Labels**: add the label `RoslynAnalyzer` (capital A — Unity recognizes that exact label).
5. Save. Unity reimports. The analyzer activates on the next compile.

The 8 Runtime asmdefs each already have a `csc.rsp` next to them that points the compiler at their `PublicAPI.*.txt` files via `/additionalfile`. No further per-module wiring is needed.

### Verifying the analyzer

Add a new public method to any Runtime assembly and do not update `PublicAPI.Unshipped.txt`. Trigger a Unity compile (e.g. by re-saving the file). The Console should report:

```text
RS0016: Symbol '<your new symbol>' is not part of the declared API.
```

Use the Roslyn Quick Fix → **Add to public API**. Unity's Inspector exposes the fix; alternatively, manually add the symbol line to `PublicAPI.Unshipped.txt`.

## What does not need setup

- The `lint.yml` and `docs-coverage.yml` workflows. They run pure-Python tooling against the repo and do not require Unity.
- The `release-drafter.yml` workflow. It reads PR labels and titles only.
