<!--
kenspc-init template: 1
Line budget: this file and CLAUDE.md together, 80 lines at init and 200 at most.
Admission rule: a line belongs in this file only when all three hold:
  1. every session needs it;
  2. it cannot be worked out from the code;
  3. without it an agent goes wrong, or it is a safety floor.
Anything else goes in a topic document under docs/, in an app's own AGENTS.md,
or, for a rule specific to Claude Code, in a path-scoped rule.
-->
# Dungeon Descent

## Project

Purpose, users, scope, and non-goals: `docs/product.md`.
A turn-based, Brogue-style ASCII roguelike rendered in a SadConsole/MonoGame window — a personal hobby and learning project.

## Commands

- Build: `dotnet build` (repository root)
- Run: `dotnet run` (repository root) — opens a GUI window, so it needs a display
- Headless check: `dotnet run -- --probe-seed <n>` (repository root) — prints the room count for `Map(n)` and exits without opening a window
- Test: none — there is no test project; verify by building and running
- Lint: TBD(init): lint command

## Rules

- This project has no staging or production environment; if one is added, do not deploy to it, run migrations against it, or read its data.
- Keep secrets out of the repository: no key, token, password, or connection string in any tracked file. `docs/deployment.md` says where each secret lives.

## Documents

| Document | Holds | Changes when |
|---|---|---|
| `AGENTS.md` | Commands, hard rules, this table, workflow | A command changes, a hard rule is added or removed, or a document is added or removed |
| `CLAUDE.md` | The import of this file, and rules specific to Claude Code | A rule specific to Claude Code is added or removed |
| `README.md` | What the project is and how to start, for people | How to start changes |
| `docs/product.md` | Purpose, users, scope, non-goals, terms | The purpose, the users, the scope, or a term changes |
| `docs/architecture/overview.md` | Stack, components and boundaries, data, external integrations, key decisions | A component, a boundary, an integration, or a key decision changes |
| `docs/ui/design-system.md` | Visual style, color roles, typography, components, layout, elevation, do and don't, responsive behavior; where the tokens live | The design system changes, or a token file moves |
| `docs/release.md` | Versioning scheme, version file, when to bump, changelog, tags, who releases | A release rule changes |
| `docs/deployment.md` | Environments, how deploys happen, where configuration and secrets live, migrations, rollback, monitoring | An environment, the pipeline, a secret's location, or a deployment policy changes |

`docs/briefs/`, `docs/plans/`, and `docs/tasks/` are not durable documents; their files are deleted once their work is done.
Guides go in `docs/guides/`, written by `/kenspc-guide`.

## Workflow

- Branches: work directly on `main`; no feature branches.
- Commits: Conventional Commits (`feat:`, `fix:`, `docs:`, `refactor:`, `chore:`, …), with an optional scope naming the feature pass or module, e.g. `feat(font):`.
- Backlog: GitHub Issues, labeled `bug`, `enhancement`, `debt`, and `found-by-agent`.
- New backlog items: in an interactive session, only with the user's agreement; an unattended run lists what it found in its report instead.
- No version number — rules in docs/release.md
