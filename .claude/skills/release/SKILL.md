---
name: release
description: Ship pending work from develop to main. Commits any uncommitted changes (after confirmation), pushes develop, waits for the GitHub Actions build to go green, then merges develop into main and pushes main (which publishes the prod `:latest` image). Use when the user says "release", "releasen", "ship", "deploy", "auf main" or similar.
---

# Release develop → main

Goal: get the current state of `develop` onto `main` once CI is green. `develop` is the working branch (publishes `:dev`); `main` is prod (publishes `:latest`). Pure git-and-CI plumbing — no version bumps, no tags, no changelog.

## Preconditions

- Current branch must be `develop`. If not, abort and tell the user. Do not switch branches yourself.
- `origin` remote must exist. If `gh` is not authenticated, abort with the auth hint.
- The user must have explicitly said "release"/"ship"/equivalent. Don't run this skill speculatively.

## Procedure

### 1. Handle uncommitted changes

Run `git status` and `git diff`.

- **No uncommitted changes**: skip to step 2.
- **Uncommitted changes present**:
  1. Show a short summary of what's modified (file list + 1-2 sentences on what the diff does).
  2. Propose a commit message in the repo's style (see `git log --oneline -5` — this repo uses short German subjects).
  3. Ask the user: "Diese Änderungen committen mit Message 'X'? Oder welche Files weglassen?" Always ask — never auto-commit, even if the diff looks clean. The user might have WIP drift mixed in (e.g. `.claude/settings.local.json`).
  4. Stage only the files the user confirmed (specific paths, never `git add -A`/`.`).
  5. Commit. **No** `Co-Authored-By` or other AI branding (project rule). No `--no-verify`.
  6. If a hook fails, fix the underlying issue and create a new commit — never `--amend` after a failed hook.

### 2. Push develop

- If `git status` reports "Your branch is up to date with 'origin/develop'" and there are no local commits ahead, skip to step 3.
- Otherwise: `git push origin develop`.

### 3. Wait for the build

- Find the run for the latest develop commit: `gh run list --branch develop --limit 1 --json databaseId,headSha,status,conclusion`. Confirm `headSha` matches `git rev-parse HEAD`.
- If no run yet, retry every few seconds — push triggers may take a moment.
- Once you have the run id, wait for completion. You can use `gh run watch <id>` (run in background, you get notified on exit) to wait, but **do NOT trust its exit code** — `gh run watch --exit-status` has been observed to exit 0 even when the run actually failed.
- Builds on this repo take ~3-6 minutes — set a generous timeout (~15min).

### 4. Branch on outcome

- **Always re-read the authoritative result before deciding**, never the `gh run watch` exit code: `gh run view <id> --json status,conclusion --jq '{status,conclusion}'`. Only `status == "completed"` && `conclusion == "success"` counts as green. Anything else (`failure`, `cancelled`, `timed_out`, `startup_failure`, still `in_progress`) is NOT green.
- **Green**: proceed to step 5.
- **Not green**: abort the release.
  - Run `gh run view <id> --log-failed` (or `gh run view <id>` for a summary) and surface a concise failure summary to the user (which job, which step, top error lines).
  - Do **not** touch main. Do **not** retry without the user's say-so.
  - Note: the `codeql` jobs depend on repo Code-Scanning settings and may be red independently of the build. If only CodeQL is red but `Build Docker Image` and `Push Docker Image` are green, tell the user and ask whether to proceed anyway — don't silently treat the overall run as failed.

### 5. Merge into main and push

`main` history uses merge commits (`Merge branch 'develop'`), not fast-forwards. So the procedure is the standard one:

```bash
git checkout main
git pull origin main
git merge develop      # creates a "Merge branch 'develop'" commit
git push origin main
git checkout develop   # always return the user to develop
```

Don't use `git push origin develop:main` — that requires a fast-forward, which fails once `main` has its own merge commit on top.

If `git merge develop` reports conflicts, abort the release and tell the user — don't try to resolve. (Conflicts shouldn't happen in this flow but if they do, the history is unusual and needs human attention.)

If the user gets stuck on main because something failed mid-flow, run `git checkout develop` to put them back.

### 6. Report

One short summary: commit SHA released, link to the successful build run (`gh run view <id> --json url --jq .url`), confirmation that main is now at that SHA. The push of `main` triggers a fresh build that publishes the prod `:latest` image to GHCR and Docker Hub.

## Notes

- Don't open a PR — this is a direct ship workflow, not a review workflow. If the user wants review, they'll open a PR manually.
- Don't push tags or create releases. Out of scope.
- If the user invokes this with a dirty working tree on a feature branch, the precondition check catches it — don't try to be clever.
