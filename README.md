# Skip Intro

<img src="Packaging/icon.png" width="160" align="right" alt="Skip Intro icon" />

A BepInEx mod for *Everything is Crab*. Skips the two splash videos at startup and goes straight to the main menu.

## Requirements

- *Everything is Crab*
- BepInEx 6 BleedingEdge IL2CPP (CoreCLR), build 755 or newer. Get it at [builds.bepinex.dev](https://builds.bepinex.dev/projects/bepinex_be).

The stable BepInEx 5 / 6 packs on Thunderstore do not work. Unity 6 IL2CPP needs the BE build.

## Install

1. Install BepInEx into your game folder. `winhttp.dll` should sit next to `Everything is Crab.exe`.
2. Launch the game once and close it. BepInEx generates the interop assemblies on first launch.
3. Download `Bungus-SkipIntro-X.X.X.zip` from the [Releases](../../releases) page.
4. Extract it into `BepInEx/plugins/`. The DLL ends up at `BepInEx/plugins/Bungus-SkipIntro/SkipIntroPlugin.dll`.
5. Launch the game.

If you use r2modman, Gale, or the Thunderstore App, "Install from file" with the zip also works.

## Features

- Skips the Odd Dreams Digital intro video.
- Skips the publisher splash video.
- Goes straight to the main menu after localisation and remote config load.
- Removable by deleting the plugin folder.

## How it works

Harmony postfix on `GameFlow.PlayIntroSplashScreen.Start`. Stops the splash coroutine, sets `CurrentState = Ready`, calls `TryMoveToMainMenu()`.

## Credits

- Bungus
- Built with [BepInEx](https://github.com/BepInEx/BepInEx) and [HarmonyX](https://github.com/BepInEx/HarmonyX).
