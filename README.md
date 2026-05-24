# Skip Intro — Everything is Crab

<img src="Packaging/icon.png" width="160" align="right" alt="Skip Intro icon" />

A BepInEx mod for *Everything is Crab* by Odd Dreams Digital.

## Description

Skips the two splash videos that play at game launch — the Odd Dreams Digital logo and the publisher sting — and drops you straight into the main menu. The full pre-menu sequence normally takes ~15 seconds; with this mod, it's gone.

The mod is non-invasive: it doesn't disable the splash scene entirely or skip any loading work. It just lets the loading state machine advance past the video-wait states immediately, so all the things that legitimately need to happen before the main menu (localisation tables loading, remote config fetch) still happen — they just don't wait on the videos.

## Installation instructions

1. Install **BepInEx 6 BleedingEdge IL2CPP (CoreCLR)** build 755 or newer. Download from [builds.bepinex.dev](https://builds.bepinex.dev/projects/bepinex_be) — pick `BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.755+...zip` (or a newer build number). Extract the contents into your *Everything is Crab* install folder. `winhttp.dll` should end up sitting next to `Everything is Crab.exe`.
2. Launch the game once and close it. BepInEx needs ~30 seconds on first launch to generate `BepInEx/interop/` (the IL2CPP wrapper assemblies). You'll see a console window with log output — that's normal.
3. Download the latest `Bungus-SkipIntro-*.zip` from the [Releases](../../releases) page.
4. Extract it. Drop the `plugins/Bungus-SkipIntro/` folder into `BepInEx/plugins/`. The final path should be `BepInEx/plugins/Bungus-SkipIntro/EverythingIsCrabPlugin.dll`.
5. Launch the game. Intro videos should be skipped.

If you use **r2modman**, **Gale**, or the **Thunderstore App**, the zip is also a valid mod-manager package — use "Install from file" and point it at the zip.

## Main features

- Skips the Odd Dreams Digital intro video at startup
- Skips the publisher splash video at startup
- Transitions directly to the main menu once localisation and remote config are ready
- Doesn't touch any other game flow — fully removable by deleting the plugin folder

## Requirements

- **Everything is Crab** (tested on 1.0.1__8213)
- **BepInEx 6 BleedingEdge IL2CPP (CoreCLR)** — build 755 or newer
  - Stable BepInEx 5 or older 6 releases (the ones on Thunderstore mod-manager packs) will **not** work — *Everything is Crab* uses Unity 6 IL2CPP which only the recent BE builds support
- Windows (other platforms not tested)

## Credits

- **Bungus** — mod author
- Built with [BepInEx](https://github.com/BepInEx/BepInEx) and [HarmonyX](https://github.com/BepInEx/HarmonyX)
