# Sync Workflow: Keep YOUR Features AND the Author's Updates

This document explains the automated workflow (`.github/workflows/sync-upstream.yml`)
that keeps your fork's custom features in sync with the original author's (`upstream`)
changes — without losing either side.

---

## The Problem

You forked GZ::CTF and added custom features. Meanwhile the original author keeps
shipping fixes and new features. You want **both**:

- The author's latest updates (security patches, new challenges, bug fixes)
- Your own custom features (your fork's reason to exist)

If you just merge everything blindly, histories get messy and conflicts pile up.
The solution is **branch separation + automated sync PRs**.

---

## The Model

```
upstream/main  ──●──●──●──●──●──●   (original author's new features)
                       \        \
your main     ──────────●─────────  (clean mirror of upstream, NO custom code)
                         \
your feature  ────────────●──●──●    (your custom features)
```

| Branch | Purpose | Custom code? |
|--------|---------|--------------|
| `main` | Mirror of `upstream/main` | ❌ Never |
| `my-feature` | Your custom work | ✅ Yes |

---

## What the Workflow Does

File: `.github/workflows/sync-upstream.yml`

It runs on a daily schedule (03:17 UTC) **or** manually (`workflow_dispatch`), and has two jobs:

### Job 1 — `sync-main`
Fast-forwards your fork's `main` to `upstream/main`.
- Adds `upstream` remote
- `git fetch upstream`
- `git merge --ff-only upstream/main` (fails safely if not a fast-forward)
- Pushes to your `origin/main`

> Because `main` has no custom commits, this is always a clean fast-forward.

### Job 2 — `sync-features`
For each feature branch (default `my-feature`, configurable), it:
1. Fetches upstream + origin
2. **Tests** the merge to detect conflicts *before* opening a PR
3. If clean → opens/updates a PR: `main` → `my-feature` titled
   `⬆️ Sync upstream/main into my-feature`
4. If conflict → posts an error telling you to resolve manually

You review the PR, then **merge it** (rebase or squash preferred).

---

## Setup (One-Time)

### 1. Create a Personal Access Token (PAT)
The workflow needs a token with `repo` scope to open cross-repo PRs:
1. GitHub → Settings → Developer settings → Personal access tokens → Fine-grained or Classic
2. Scope: `repo` (full control of private repos) — or `public_repo` if fork is public
3. Copy the token.

### 2. Add it as a repo secret
In **your fork** (MoatazMahmoud404/GZCTF):
- Settings → Secrets and variables → Actions → New repository secret
- Name: `GH_PAT`
- Value: the token you copied

> If `GH_PAT` is absent, the workflow falls back to `GITHUB_TOKEN`, which can
> push to your own fork but **cannot** open PRs across forks reliably. Set `GH_PAT`.

### 3. Push the workflow file
```bash
git checkout main
git fetch upstream && git merge --ff-only upstream/main
git checkout -b add-sync-workflow
# (the workflow file is already in .github/workflows/sync-upstream.yml)
git add .github/workflows/sync-upstream.yml
git commit -m "ci: add upstream sync workflow"
git push origin add-sync-workflow
# Open PR -> main, merge it
```

---

## Daily Usage

You don't need to do anything — the cron runs daily. To trigger manually:

1. Go to your fork → Actions → **Sync Upstream & Features**
2. Click **Run workflow**
3. Optionally set `feature_branches` input (comma-separated, e.g. `my-feature,team-mode`)
4. Watch the `sync-features` job open/update PRs

Then on each feature branch PR:
- Review the diff
- Click **Rebase and merge** (or Squash) to keep history clean
- If a conflict PR appeared, resolve it locally (see below)

---

## Manual Sync (Local Equivalent)

If you prefer the terminal, or the workflow reports a conflict:

```bash
# 1. Sync your main
git checkout main
git fetch upstream
git merge --ff-only upstream/main
git push origin main

# 2. Bring updates into your feature branch
git checkout my-feature
git fetch upstream
git rebase upstream/main          # clean linear history
#   -- OR --
git merge upstream/main           # preserves branch point

# 3. Resolve conflicts if any
#    edit conflicted files, then:
git add <file>
git rebase --continue             # (or git merge --continue)

# 4. Push (force needed after rebase)
git push origin my-feature --force-with-lease
```

---

## Conflict Resolution Tips

- **Rebase** replays your commits on top of upstream → linear, PR-friendly.
- **Merge** keeps the exact fork point → no force-push, but messier graph.
- The workflow's "test merge" step tells you *which* branch conflicts so you
  fix it locally instead of discovering it in a broken PR.
- After resolving, rebuild with the steps in `EDIT_AND_REBUILD.md` §4–7.

---

## Contributing Back

When your feature is ready and synced:
1. Ensure `my-feature` is up to date with `upstream/main` (via the workflow or manually)
2. Open a PR: `my-feature` → `GZTimeWalker/GZCTF` `develop`
3. Follow the PR template; CI must pass (`ci.yml`, `work.yml`)

---

## Customizing

| Variable | Where | Default | Meaning |
|----------|-------|---------|---------|
| `UPSTREAM_REPO` | workflow `env` | `GZTimeWalker/GZCTF` | Author's repo |
| `BASE_BRANCH` | workflow `env` | `main` | Branch to mirror/sync from |
| `feature_branches` | dispatch input | `my-feature` | Which branches to sync |
| cron schedule | `on.schedule` | `17 3 * * *` | Daily run time |

To change the synced branch to `develop`, edit `BASE_BRANCH` and the dispatch default.
