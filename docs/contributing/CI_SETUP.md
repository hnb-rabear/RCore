# CI setup

Two pieces of out-of-band setup are required before the CI workflows in `.github/workflows/` can run a green build. This document is the runbook for the solo maintainer.

## 1. Unity license secrets

`unity-test.yml` and `benchmark.yml` invoke `game-ci/unity-test-runner`, which needs a valid Unity license to run the Editor in batch mode.

> **Current status**: both workflows are set to `workflow_dispatch` only (manual trigger). The `pull_request` / `push` triggers are commented out so PRs don't show a red X while no license is configured. Restore them after the secrets below are set.

### Adding the license secrets

In the repo on GitHub: **Settings → Secrets and variables → Actions → New repository secret**.

The two license tiers have different setup paths.

#### Pro / Plus (paid) — easy path

Add three secrets:

- `UNITY_SERIAL` — Pro/Plus serial number (e.g. `SB-XXXX-XXXX-XXXX-XXXX-XXXX`).
- `UNITY_EMAIL` — Unity ID email.
- `UNITY_PASSWORD` — Unity ID password.

#### Personal (free) — manual `.ulf` flow

Unity 2020+ stopped storing the Personal `.ulf` as a plain file on disk; the Unity Licensing Client now caches an encrypted blob via the Unity ID sign-in. The `license.unity3d.com/manual` web flow that game-ci docs reference now accepts Pro/Plus serials only (the Personal `.alf`-upload path on that page has been deprecated as of 2026).

Today's working flow for Personal on CI:

1. Locally generate the activation request: in a clean folder, run

       Unity.exe -batchmode -createManualActivationFile -nographics -logfile activation.log -quit

   That writes `Unity_v<version>.alf` next to the log file.
2. Sign in to <https://id.unity.com/>.
3. Visit <https://license.unity3d.com/manual> while signed in. If the page still only shows the Plus/Pro serial input, the Personal upload path is not currently available — fall back to one of:
   - Use the game-ci-supported workflow `game-ci/unity-request-activation-file@v2` followed by `game-ci/unity-activate@v2` (some recent releases handle the activation server-side without the manual upload step).
   - Wait for Unity to restore the Personal flow.
   - Upgrade to Pro/Plus and use the serial path above.
4. Save the resulting `.ulf` file. Open it in a text editor and copy the full XML contents into the GitHub secret `UNITY_LICENSE`. Also add `UNITY_EMAIL` and `UNITY_PASSWORD`.

`unity-test-runner` checks `UNITY_LICENSE` first, then falls back to `UNITY_SERIAL`.

### Verifying the license

After the secrets are set:

1. Restore the auto-triggers in `unity-test.yml` and `benchmark.yml` (uncomment the `pull_request` and `push` blocks at the top of each file).
2. Open **Actions → Unity Tests → Run workflow** and trigger a manual run, or open a PR with a code change.
3. Watch the first job's first step: it should report `Activated license` rather than the licensing error.

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

    RS0016: Symbol '<your new symbol>' is not part of the declared API.

Use the Roslyn Quick Fix → **Add to public API**. Unity's Inspector exposes the fix; alternatively, manually add the symbol line to `PublicAPI.Unshipped.txt`.

## What does not need setup

- The `lint.yml` and `docs-coverage.yml` workflows. They run pure-Python tooling against the repo and do not require Unity.
- The `release-drafter.yml` workflow. It reads PR labels and titles only.
