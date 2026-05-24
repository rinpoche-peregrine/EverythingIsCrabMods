# Skip Intro

Skips the two splash videos (Odd Dreams Digital logo + publisher sting) at the start of *Everything is Crab*, dropping you straight into the main menu.

## Requirements

- **Everything is Crab** (tested against 1.0.1__8213)
- **BepInEx 6 BleedingEdge IL2CPP (CoreCLR)** — build 755 or newer. Get it from [builds.bepinex.dev](https://builds.bepinex.dev/projects/bepinex_be).

The stable BepInEx 5 / 6 releases on Thunderstore won't work — Everything is Crab uses Unity 6 IL2CPP which needs a recent BE build.

## Install (manual)

1. Install BepInEx 6 BE 755 (IL2CPP, CoreCLR) into the game folder. The `winhttp.dll` should sit next to `Everything is Crab.exe`.
2. Launch the game once so BepInEx generates `BepInEx/interop/` (takes ~30s the first time).
3. Drop `EverythingIsCrabPlugin.dll` into `BepInEx/plugins/Bungus-SkipIntro/`.
4. Launch.

## Install (mod manager)

If you have r2modman / Gale / Thunderstore App configured for Everything is Crab, use "Install from file" and select this zip.

## Uninstall

Delete the `Bungus-SkipIntro` folder from `BepInEx/plugins/`.

## How it works

Patches `GameFlow.PlayIntroSplashScreen.Start` with a Harmony postfix that stops the splash coroutine, forces `CurrentState = Ready`, and calls `TryMoveToMainMenu()`. Side effect: a brief `Ready -> Watching1stVideo` log line as the dying splash scene takes one more tick before being destroyed. Cosmetic, no functional impact.

## Author

Bungus

## Source

<https://github.com/rinpoche-peregrine/EverythingIsCrabMods>
