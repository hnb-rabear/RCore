---
name: changelog-before-push
description: "MANDATORY — activate BEFORE pushing to main/master. You MUST analyze the git diff, draft a CHANGELOG.md entry, and get user approval before pushing. No push to main/master is complete without a CHANGELOG update."
---

# Changelog Before Push

## The Iron Rule

```
NEVER push to main/master without updating CHANGELOG.md first.
```

Every push to `main` or `master` — whether it contains a new feature, bug fix, refactor, or config change — **MUST** have a corresponding CHANGELOG.md entry written and approved before the push executes.

---

## Multi-Package Project Structure

This project contains **multiple Unity packages**, each with its own `CHANGELOG.md`, `README.md`, and `package.json`. You MUST identify which package(s) are affected and update the correct files.

### Package Map

| Package | Path | package.json |
|---------|------|-------------|
| **RCore Main** | `Assets/RCore/Main/` | `com.rabear.rcore.main` |
| **Ads** | `Assets/RCore/Services/Ads/` | `com.rabear.rcore.services.ads` |
| **Firebase** | `Assets/RCore/Services/Firebase/` | `com.rabear.rcore.services.firebase` |
| **Game Services** | `Assets/RCore/Services/GameServices/` | `com.rabear.rcore.services.gameservices` |
| **IAP** | `Assets/RCore/Services/IAP/` | `com.rabear.rcore.services.iap` |
| **Notification** | `Assets/RCore/Services/Notification/` | `com.rabear.rcore.services.notifications` |
| **Sub** | `Assets/RCore/Sub/` | `com.rabear.rcore.sub` |

Each package has:
- `<package>/CHANGELOG.md` — version history
- `<package>/package.json` — version number
- Root `README.md` — project-wide documentation (updated for major changes only)

---

## The Process

### Step 1: Analyze the Diff

```bash
git diff --stat HEAD
git log --oneline origin/main..HEAD
```

### Step 2: Identify Affected Package(s)

From the diff, determine which package(s) have changes:
- Files in `Assets/RCore/Main/` → update `Assets/RCore/Main/CHANGELOG.md`
- Files in `Assets/RCore/Services/Ads/` → update `Assets/RCore/Services/Ads/CHANGELOG.md`
- Files spanning multiple packages → update EACH package's CHANGELOG separately

### Step 3: Determine Version Bump (per package)

Follow SemVer (`MAJOR.MINOR.PATCH`) **independently for each affected package**:

| Change Type | Bump |
|-------------|------|
| Breaking API change, major architectural shift | **MAJOR** |
| New feature, new system, new attribute | **MINOR** |
| Bug fix, perf improvement, small refactor, UI polish | **PATCH** |

Read current version from each affected package's `package.json`.

### Step 4: Draft the CHANGELOG Entry

Create entry at the **top** of the affected package's `CHANGELOG.md`, following Keep a Changelog format:

```markdown
## [X.Y.Z]
### Added
- **Feature Name**: Description of what it does and why.

### Changed
- **Component**: What changed and impact.

### Fixed
- **Bug Name**: What was broken and how it was fixed.
```

**Rules:**
- ✅ Write in **English**
- ✅ Focus on **user impact**, not class/function names
- ✅ Each bullet starts with `**[Short Name]:**`
- ✅ Group related changes into single bullets
- ❌ Don't list internal implementation details
- ❌ Don't leave version placeholder — use real version

### Step 5: Update package.json

Bump `"version"` in each affected package's `package.json` to match its CHANGELOG entry.

### Step 6: Present Draft to User

Display draft for review before writing to file. If multiple packages are affected, show all entries together.

### Step 7: Write & Commit

After user confirms:
1. Write entries to each affected `CHANGELOG.md`
2. Update each affected `package.json` version
3. Stage all changed files
4. Commit and push

---

## Red Flags — STOP

| Situation | Action |
|-----------|--------|
| About to `git push origin main` without CHANGELOG | **STOP. Write CHANGELOG first.** |
| User says "push now" | Still write CHANGELOG — just draft shorter |
| Changes span multiple packages | Update EACH package's CHANGELOG separately |
| "Just a config/docs change" | Still needs entry — use Changed or Fixed category |
| Unsure about version bump type | Ask user: "Is this PATCH or MINOR?" |
