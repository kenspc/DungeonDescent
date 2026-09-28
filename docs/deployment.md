# Deployment

## Environments

| Name | Purpose | URL | Hosting | Database | Who can deploy |
|---|---|---|---|---|---|
| Local | Development and play | None | The developer's machine, via `dotnet run` | None | Not applicable: nothing is deployed |

## How deploys happen

Nothing is deployed or distributed: the game is built and run from source, and there is no CI pipeline. Before any public distribution, the font's CC BY-SA 4.0 license requires attribution; see `assets/fonts/README.md`.

## Configuration and secrets

Locations only: this document holds no secret value.

There are no secrets: the game uses no outside service, key, or credential. Its only configuration is the command-line flags (`--font`, `--probe-seed`) and the SDK pin in `global.json`.

## Migrations

Not applicable: there is no database and no save file.

## Rollback

Not applicable: nothing is deployed.

## Monitoring

Not applicable: nothing runs outside the developer's machine.
