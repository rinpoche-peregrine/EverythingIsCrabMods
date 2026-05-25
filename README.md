# Skip Intro

<img src="Packaging/icon.png" width="160" align="right" alt="Skip Intro icon" />

A BepInEx mod for *Everything is Crab*. Skips the two splash videos at startup and goes straight to the main menu, without racing past the game's background loading.

## Requirements

- *Everything is Crab*
- BepInEx 6 BleedingEdge IL2CPP (CoreCLR), build 755 or newer. Get it at [builds.bepinex.dev](https://builds.bepinex.dev/projects/bepinex_be).

The stable BepInEx 5 / 6 packs on Thunderstore do not work. Unity 6 IL2CPP needs the BE build.

## Install

### Easiest: use Crab Mod Manager

1. Get [Crab Mod Manager](https://github.com/rinpoche-peregrine/CrabModManager) and place its exe in your game folder.
2. Run it. Click "Install BepInEx" if you do not have BepInEx yet. Launch the game once afterward, then close it.
3. Download `Bungus-SkipIntro-X.X.X.zip` from the [Releases](../../releases) page or from [Nexus Mods](https://www.nexusmods.com/everythingiscrab/mods/1).
4. Drag the zip onto the Crab Mod Manager window.
5. Launch the game.

### Manual

1. Install BepInEx into your game folder. `winhttp.dll` should sit next to `Everything is Crab.exe`.
2. Launch the game once and close it. BepInEx generates interop assemblies on first launch.
3. Download the zip from [Releases](../../releases) or [Nexus Mods](https://www.nexusmods.com/everythingiscrab/mods/1).
4. Extract the `plugins/Bungus-SkipIntro/` folder into `BepInEx/plugins/`. The DLL ends up at `BepInEx/plugins/Bungus-SkipIntro/SkipIntroPlugin.dll`.
5. Launch the game.

If you use r2modman, Gale, or the Thunderstore App, "Install from file" with the zip also works.

## Features

- Skips the Odd Dreams Digital intro video.
- Skips the publisher splash video.
- Goes straight to the main menu only after all the background loading actually finishes. No race conditions on slow hardware.
- Removable by deleting the plugin folder.

## Config

`BepInEx/config/com.bungus.everythingiscrab.skipintro.cfg`:

- `[Behavior] WaitForExtraReadiness` (default true): Hold the main-menu transition until Analytics, PurchasablesSyncerRuntimeManager, and CosmeticsRuntimeManager all report ready, in addition to the game's own Localisation + RemoteConfig gates. Falls back to a 30s safety timeout if any gate hangs (e.g. analytics consent flow stalled). Set false to skip extra gating and rely only on the game's built-in readiness checks.

## How it works

Harmony postfix on `GameFlow.PlayIntroSplashScreen.Awake`: zeroes `_timeToWaitForVideoPreparation` and `_timeToWaitForVideoToPlay`, stops both `VideoPlayer`s. The game's own state machine then runs unchanged through `Watching1stVideo` → `WaitingForLocalisation` → `WaitingForRemoteConfig` → `Ready`, just on a fast clock.

For the extra readiness gate, a `SplashReadinessGate` MonoBehaviour is attached to the splash GameObject. It polls `UgsAnalytics.AnalyticsInitialized.IsReady`, `PurchasablesSyncerRuntimeManager.Instance.IsReady`, and `CosmeticsRuntimeManager.Instance.IsReady` each frame. A Harmony prefix on `TryMoveToMainMenu` suppresses the splash's natural transition call until all gates report ready, then lets it through. If any gate hangs past 30 seconds, the transition fires anyway and logs which gates were still pending.

## Credits

- Bungus
- Built with [BepInEx](https://github.com/BepInEx/BepInEx) and [HarmonyX](https://github.com/BepInEx/HarmonyX).

## Support

If you find this useful, you can buy me a coffee:

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/Z8Z852YLV)
