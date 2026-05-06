# GilDelta

Dalamud plugin for FFXIV that tracks your gil flow across **your own wallet, every retainer, and the FC chest**. Each change is auto-classified (submarine return, retainer sale, NPC purchase, marketboard buy, repair, …) and surfaced through a configurable always-on widget plus a four-tab dashboard.

Read-only — never writes back to game state.

<p align="center"><img src="images/icon.png" alt="GilDelta icon" width="160"></p>

## Install

In-game, open Dalamud settings (`/xlsettings`) → **Experimental → Custom Plugin Repositories**, then add:

```
https://raw.githubusercontent.com/yagi2/dalamud-plugins/main/repo.json
```

Save, then open the plugin installer (`/xlplugins`) and search for **GilDelta**.

> The URL above is yagi2's shared plugin index — every plugin under `yagi2/dalamud-plugins` appears in it, so adding it once is enough for any future plugins as well.

## Features

- **Per-character event log.** All gil deltas land in a JSONL at `%AppData%\XIVLauncher\pluginConfigs\GilDelta\events\<contentId>.jsonl`. Every event records `category`, `wallet`, `amount`, `note` (rule name + reason), and `ts` with local timezone offset.
- **Always-on widget**, four densities — Minimal (one-line pill), Compact (default), Trend (compact + sparkline), Tile (today / week / month grid).
- **Dashboard** with four tabs:
  - *Timeline* — reverse-chronological event list with hoverable rule notes.
  - *Chart* — running wealth line + daily-net histogram for the last 30 days.
  - *Breakdown* — net by category for today / 7d / 30d / all.
  - *Heatmap* — 12-week × 7-day grid colored by daily net.
- **Eight inference rules** classifying retainer transfers, submarine returns, NPC shop buy/sell, marketboard buy, retainer sales, and repair. Anything that doesn't match falls through to `Misc`.
- **Six-language UI** — English, 日本語, Deutsch, Français, 简体中文, 한국어.
- **Diagnostic logging** — every classified event gets a one-line `IPluginLog.Information` breadcrumb so misclassifications are debuggable from `/xllog`.

## Slash commands

| Command | Action |
|---|---|
| `/gildelta` | Toggle the dashboard |
| `/gildelta config` | Toggle the settings window |
| `/gd` | Alias for `/gildelta` |

## Settings

In `/gildelta config`:

- **General** — language, theme.
- **Widget** — show/hide, density, hide-in-cutscene, hide-in-duty, lock position, reset position.
- **Dashboard** — default tab.
- **Data** — retention days (`0` = forever).

## Build

```
dotnet build -c Release
```

Output:
- `bin/Release/GilDelta/GilDelta.dll` — loadable plugin DLL.
- `bin/Release/GilDelta/latest.zip` — distribution archive (DalamudPackager).

The Dalamud SDK uses your local Dalamud install at `%AppData%\XIVLauncher\addon\Hooks\dev\` for reference assemblies; no separate setup required.

## Install (dev)

Point Dalamud's *Dev Plugin Locations* at `bin/Release/GilDelta/GilDelta.dll`.

## Release process (maintainer)

1. Bump `<Version>` in `GilDelta.csproj` and `Changelog` in `GilDelta.json`.
2. Commit and push to `main`.
3. Create a GitHub Release tagged `vX.Y.Z`. **No need to attach a binary** — the `Release` GHA workflow builds on `windows-latest` and uploads `latest.zip` automatically.
4. Update the matching `AssemblyVersion` / `Changelog` entry in [`yagi2/dalamud-plugins/repo.json`](https://github.com/yagi2/dalamud-plugins) and push to `main`.

Once both pushes land, Dalamud picks up the new version on its next plugin-list refresh.

## License

MIT.
