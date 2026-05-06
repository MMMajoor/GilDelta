# CLAUDE.md

Notes for future agents working on GilDelta.

## Project at a glance

- **Type:** Dalamud plugin (FFXIV), API 15, .NET via `Dalamud.NET.Sdk/15.0.0`.
- **Goal:** read-only gil tracker — observe gil changes across self / retainers / FC chest, classify each into a category (`SubmarineReturn`, `RetainerSale`, `NpcShopBuy`, etc.), persist as JSONL, surface through a configurable always-on widget plus a four-tab dashboard.
- **Read-only invariant.** No game-state writes anywhere. WalletReader only reads from FFXIVClientStructs; if a future task needs to call any "Execute"/"Send"/"Set" API, stop and confirm scope with the user.

## Code map

```
GilDelta/
├── GilDelta.csproj                    # Dalamud.NET.Sdk/15.0.0; pins output to bin/Release/
├── GilDelta.json                      # plugin manifest shown in Dalamud
├── GilDelta.sln                       # main project + tests/GilDelta.Tests
├── Plugin.cs                          # IDalamudPlugin entry, DI, command registration
├── Service.cs                         # [PluginService] container (PluginInterface, Framework, ClientState, GameGui, CommandManager, Log, Condition, PlayerState)
├── Configuration.cs                   # IPluginConfiguration with v1 settings
├── Wallet/
│   ├── WalletKind.cs                  # enum: Self / Retainer / FreeCompanyChest
│   ├── WalletId.cs                    # readonly record struct (Kind + Identifier)
│   ├── Wallet.cs                      # readonly record struct (Id + Amount)
│   ├── WalletDiff.cs                  # readonly record struct, computes Delta
│   ├── WalletReader.cs                # reads via FFXIVClientStructs (see below)
│   ├── WalletWatcher.cs               # Framework.Update poller, emits OnDiff
│   ├── AddonProbe.cs                  # checks which Watched addons are loaded
│   └── AddonStateTracker.cs           # rolling 2-second history of seen addons
├── Events/
│   ├── GilEventCategory.cs            # 10-member enum (Teleport kept for back-compat though rule was dropped)
│   ├── GilEvent.cs                    # sealed record (Timestamp, Wallet, Amount, Category, Note)
│   ├── GameContext.cs                 # OpenAddons + RecentDiffs + Now, fed to rules
│   ├── IInferenceRule.cs              # TryClassify(diff, ctx, [NotNullWhen(true)] out ev)
│   ├── Inferrer.cs                    # priority-ordered rule chain, swallows rule exceptions
│   ├── EventStore.cs                  # JSONL persistence with v=1 schema, Reclassify with .bak rollback
│   ├── EventLog.cs                    # in-memory cache exposed to Windows
│   └── Rules/
│       ├── PairedTransferRule.cs      # Self↔Retainer paired diffs within 500ms → Deposit/Withdraw
│       ├── SubmarineReturnRule.cs     # FC chest +Δ → SubmarineReturn
│       ├── RetainerSaleRule.cs        # Retainer +Δ (no pair) → RetainerSale
│       ├── NpcShopBuyRule.cs          # Self -Δ + Shop/InclusionShop addon
│       ├── NpcShopSellRule.cs         # Self +Δ + Shop/InclusionShop addon
│       ├── MarketBoardBuyRule.cs      # Self -Δ + ItemSearch addon
│       ├── RepairRule.cs              # Self -Δ + Repair addon
│       └── MiscRule.cs                # fallback (always matches)
├── Theme/                             # MidnightCoin (dark + gold + monospace)
├── Localization/                      # 6-arg T() helper (EN/JA/DE/FR/ZH/KO)
├── Windows/
│   ├── ConfigWindow.cs                # General / Widget / Dashboard / Data tabs
│   ├── Widget/
│   │   ├── WidgetContext.cs           # totals + per-source breakdown + today/week/month
│   │   ├── IWidgetRenderer.cs         # interface keyed by WidgetDensity
│   │   ├── WidgetWindow.cs            # always-on, position-persistent
│   │   ├── MinimalRenderer.cs         # one-line pill
│   │   ├── CompactRenderer.cs         # default; total + delta + breakdown
│   │   ├── TrendRenderer.cs           # Compact + 24h sparkline
│   │   └── TileRenderer.cs            # today / week / month tile grid
│   └── Dashboard/
│       ├── DashboardContext.cs        # daily/weekly bucketing + NetByCategory
│       ├── IDashboardTab.cs           # interface keyed by DashboardTab
│       ├── DashboardWindow.cs         # tab bar; honors DefaultTab on first open only
│       ├── TimelineTab.cs             # last 200 events, reverse chronological
│       ├── ChartTab.cs                # PlotLines (wealth) + PlotHistogram (daily net)
│       ├── BreakdownTab.cs            # category bars with today/7d/30d/all selector
│       └── HeatmapTab.cs              # 12-week × 7-day grid, color = daily net
├── images/
│   ├── icon.svg                       # source design (G monogram on a gold coin)
│   └── icon.png                       # 512×512 deliverable; needs sRGB+gAMA+pHYs chunks
├── tests/GilDelta.Tests/              # xUnit; pure-logic only (no Dalamud refs)
└── .github/workflows/release.yml      # auto-attaches latest.zip on release publish
```

## Game-side wiring (FFXIVClientStructs APIs we use)

These were verified by decompiling `%AppData%\XIVLauncher\addon\Hooks\dev\FFXIVClientStructs.dll`. If the SDK is bumped and one breaks, re-derive from there.

- **Self gil:** `InventoryManager.Instance()->GetInventoryItemCount(itemId: 1)` returns `int`. Item ID 1 is gil.
- **Retainer gil:** iterate `RetainerManager.Instance()->Retainers` (Span<Retainer>). Skip slots where `RetainerId == 0`. Read `Retainer.Gil` (uint) and `Retainer.NameString` (string).
- **FC chest gil:** `InventoryManager.Instance()->GetFreeCompanyGil()` returns `uint`. Reads as 0 outside the FC house / before opening the chest, which is fine — a 0-amount wallet still appears in the breakdown row.
- **FC name:** `InfoProxyFreeCompany.Instance()->NameString`.
- **Open addons:** `Service.GameGui.GetAddonByName(name)` returns `AtkUnitBasePtr`; `addon.Address != 0` means loaded.

## Dalamud SDK 15 gotchas (caught the hard way)

These are the deviations that cost hours during Plan 1 / 2 / 3:

1. **`pi.Inject<T>()` doesn't exist.** Use `pi.Inject(new Service())` (non-generic, takes an instance whose `[PluginService]` static members it fills via reflection). The instance is throwaway because all properties are static.
2. **`IClientState.LocalContentId` is gone.** The active character's content ID lives on `IPlayerState.ContentId`. Service container has both `ClientState` and `PlayerState`.
3. **`Dalamud.Bindings.ImGui` is the active ImGui namespace, NOT `ImGuiNET`.** Includes `ImGui`, `ImGuiCol`, `ImGuiWindowFlags`, `ImGuiTabItemFlags`, etc.
4. **`PlotLines` / `PlotHistogram` signatures differ from ImGui.NET.** Drop the `values_offset` arg and use `ReadOnlySpan<float>`. The working signature is:
   ```
   PlotLines(string label, ReadOnlySpan<float> values, int count,
             string overlay, float scale_min, float scale_max,
             Vector2 size, int stride)
   ```
5. **Default build output is `bin/x64/Release/`** (SDK 15 inserts an `x64` platform segment). Pin it back to `bin/Release/` via the csproj PropertyGroup:
   ```xml
   <PlatformTarget>x64</PlatformTarget>
   <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
   <AppendRuntimeIdentifierToOutputPath>false</AppendRuntimeIdentifierToOutputPath>
   <OutputPath>bin\$(Configuration)\</OutputPath>
   ```
6. **DalamudPackager 15 cleans up loose files after zipping.** The `DefaultDalamudPackagerRelease` target stages files into `bin/Release/GilDelta/`, zips them into `latest.zip`, then deletes the staged copies. Dev Plugin Locations need a real DLL on disk, so we re-extract the zip in an `ExtractStagedZipForDevPlugin` target wired with `AfterTargets="Build"` AND `DependsOnTargets="DefaultDalamudPackagerDebug;DefaultDalamudPackagerRelease"`. The `DependsOnTargets` is what forces ordering — `AfterTargets="DefaultDalamudPackagerRelease"` alone doesn't.
7. **Plugin icon PNG needs sRGB + gAMA + pHYs chunks.** Pillow's default output (only IHDR/IDAT/IEND) shows up as a blank icon in the plugin installer. Inject the three metadata chunks after IHDR. The Python recipe is in git history (commit `a21b837`).
8. **`Wallet` type vs `GilDelta.Wallet` namespace shadow.** Inside any consumer that does `using GilDelta.Wallet;`, the bare name `Wallet` resolves to the namespace, not the type. Test code under `GilDelta.Tests.Windows.*` and any production code under `GilDelta.Windows.*` must qualify as `Wallet.Wallet` or `GilDelta.Wallet.Wallet`.
9. **`IClientState.Logout` event signature.** `(int type, int code)` — both ints. Don't pass an `EventHandler`.

## Inference chain & diagnostic logging

`Plugin.HandleDiff` runs every wallet diff through `Inferrer.Classify(diff, ctx)` where `ctx.OpenAddons` is the **rolling 2-second union** of seen addons (not just "open right now"). This catches the "Shop closed before WalletWatcher detected the next-tick gil change" race. Implementation lives in `AddonStateTracker`.

Each classified event also writes a one-line breadcrumb at `IPluginLog.Information` level:
```
Diff Self() +150 -> NpcShopSell; recentAddons=[Shop]
```
Useful for `/xllog` post-mortem when a misclassification is reported. Prefer adding tests + tightening rules over expanding `AddonProbe.Watched`.

## Known-deferred classification gaps

- **Teleport** rule was dropped in commit `69c3ce1` because the Teleport addon doesn't always open (favorites, world-map right-click, aetheryte interactions). Cost is small (a few hundred gil), Misc fallback is acceptable. The `GilEventCategory.Teleport` enum value and `Strings.CategoryTeleport` are kept for source-compat.
- **MarketBoard sale** doesn't have a dedicated rule. Marketboard sales appear as a Retainer +Δ when the player retrieves them, so they're classified as `RetainerSale`. Splitting them out would require correlating with retainer market queue state — possible but deferred to v1.x.
- **Quest reward** and similar small Self +Δ events fall through to `Misc`. Hard to detect without addon hooks.

## Persistence

- One JSONL file per character at `%AppData%\XIVLauncher\pluginConfigs\GilDelta\events\<contentId>.jsonl`.
- Schema: `{"v":1,"ts":"<iso>","wallet":{"kind":"...","id":"..."},"amount":<long>,"category":"...","note":"rule=<RuleName>; ..."}`.
- Append-only on the live path. `EventStore.Reclassify(from, to, inferrer)` is the only mutation; it preserves the original as `<path>.jsonl.bak` before atomic-rename.
- `EventLog` is the in-memory cache. Windows always read from there, never disk.

## Build

```
dotnet build -c Release
```

- Output: `bin/Release/GilDelta/GilDelta.dll` (loadable) + `bin/Release/GilDelta/latest.zip` (distribution).
- The `NETSDK1057` preview-SDK info note is harmless.

```
dotnet test
```

xUnit project at `tests/GilDelta.Tests/`. Pure-logic only — no Dalamud references. ImGui rendering is verified in-game, not by tests. Current count: ~60 tests covering Wallet types, Inferrer + every rule, RuleChain integration, EventStore (append/load/reclassify), EventLog, WidgetContext, DashboardContext.

## Distribution

- Index repo: [`yagi2/dalamud-plugins`](https://github.com/yagi2/dalamud-plugins). End users add the index URL to Dalamud once and get every yagi2 plugin in their installer. New release = bump `AssemblyVersion`/`Changelog` in that repo's `repo.json`.
- Release artifacts: GHA workflow at `.github/workflows/release.yml` triggers on `release: published`, builds on `windows-latest`, downloads Dalamud reference assemblies from `https://goatcorp.github.io/dalamud-distrib/latest.zip` (stable, NOT `stg/`), uploads `bin/Release/GilDelta/latest.zip` via `gh release upload --clobber`.

## Reference plugins

- `..\RepeatBuy` — yagi2's first Dalamud plugin. Same Dalamud SDK 15. Uses an older addon-driven flow (single command) without any DI surface; useful for csproj/manifest reference only.
- `..\Restocker` — yagi2's second plugin. Larger codebase, similar shape to GilDelta. Particularly useful for ImGui binding patterns and the icon convention.

## What not to do

- Don't write to game state. WalletReader is read-only; stay that way.
- Don't expand `AddonProbe.Watched` speculatively. Add an addon name only when a `Diff ... -> Misc; recentAddons=[]` log line shows the addon was missing during a real misclassification.
- Don't bypass the rolling 2-second window — it exists to absorb the WalletWatcher / addon-close timing race.
- Don't commit anything under `docs/superpowers/` or `.superpowers/` — those are for local agent process artifacts and are intentionally `.gitignore`d.
- Don't skip `<PlatformTarget>x64</PlatformTarget>` and the AppendTargetFrameworkToOutputPath / AppendRuntimeIdentifierToOutputPath / OutputPath trio. Without them SDK 15 routes the build to `bin/x64/Release/` and Dev Plugin Locations break silently.
- Don't ship a PNG icon without sRGB + gAMA + pHYs chunks. The plugin installer will leave the icon slot blank.
