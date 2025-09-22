# Contributing — Pull Request Rules

Thanks for improving UnityBLE! To help us review quickly and keep the repo healthy, follow these rules when opening a Pull Request (PR).

## Before You Start
- Open an issue for any non‑trivial change to align on scope first.
- Keep PRs small and focused; one change per PR.
- Target the default branch unless maintainers request otherwise.

## Branch & Commit
- Branch name: `feature/<short-topic>`, `fix/<short-topic>`.
- Write clear commits (present tense). Squash locally if you can.
- Don’t bump versions; maintainers handle releases.

## Coding & Style
- Match existing code style and folder layout.
- Add/adjust tests for behavior changes.
- Update docs (`README.md`, docs/, samples) for user-facing changes.
- Update `CHANGELOG.md` under the “Unreleased” section.

## PR Checklist
Include this in your PR description and ensure each item is done:
- [ ] Linked issue (e.g., `Closes #123`).
- [ ] Clear description of what/why + screenshots/logs if UI or runtime is affected.
- [ ] Backwards compatibility considered (note breaking changes explicitly).
- [ ] Tests added or rationale for no tests.
- [ ] Docs and `CHANGELOG.md` updated as needed.

## Review & CI
- Ensure the project builds and tests pass locally before submitting.
- Be responsive to review comments; keep discussion focused and constructive.
- Avoid force‑pushes after review starts unless requested; use follow‑up commits.

## License & Ownership
By submitting a PR, you confirm your contribution is your own work and you agree to license it under the repository’s license.

Thank you for contributing!
