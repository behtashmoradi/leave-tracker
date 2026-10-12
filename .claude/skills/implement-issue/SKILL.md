name: implement-issue
description: Implement a GitHub issue end to end on a feature branch and open a PR. Use when asked to implement or pick up an issue by number.

Implement GitHub issue #$ARGUMENTS.

Read the issue: gh issue view $ARGUMENTS --comments
Write a short plan (files, migration, tests). Stop and wait for approval.
Create branch feature/$ARGUMENTS-<short-slug> from an up-to-date main.
Write failing tests first, then the implementation. Run dotnet build and dotnet test until green.
Self-review the full diff against CLAUDE.md: SQL parameterised, validation present, no secrets, no unrelated changes.
Commit in small logical commits, push, and open a PR with gh pr create using the PR template. Body must contain "Closes #$ARGUMENTS".
Report the PR URL. Do not merge.