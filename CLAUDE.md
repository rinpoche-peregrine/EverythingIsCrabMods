# Everything is Crab — BepInEx Modding

## Identity

- **Mod author:** Bungus (Emerson's modding handle — use this everywhere public-facing: plugin GUID, README author, Thunderstore packaging)
- **Gamertag:** PerryGoBrrrr (use for in-game names / player profiles, not mod metadata)
- Do **not** put "Emerson" in any shipped mod metadata
- Plugin GUID format: `com.bungus.everythingiscrab.<modname>`

## Game

- **Title:** Everything is Crab
- **Developer:** Odd Dreams Digital
- **Install path:** `G:\SteamLibrary\steamapps\common\Everything is Crab`
- **Engine:** Unity 6 (6000.2.15f1) + **IL2CPP**
- **Modding framework:** BepInEx 6 Bleeding-Edge build 755 (CoreCLR / .NET 6)

## Project layout

```
Everything is Crab Modding/
  Directory.Build.props        # defines $(GameDir), $(BepInExDir), $(InteropDir), $(PluginsDir)
  EverythingIsCrabPlugin/
    EverythingIsCrabPlugin.csproj
    MyPluginInfo.cs            # plugin GUID/name/version/author constants
    Plugin.cs                  # BasePlugin entrypoint, applies Harmony patches
    SkipIntroPatch.cs          # active patch: skip Odd Dreams + SM intro videos
```

## Build & package

From `EverythingIsCrabPlugin/`:
```
dotnet build -c Release
```
On every build the `DeployPlugin` target auto-copies the DLL into `$(PluginsDir)` (= `G:\SteamLibrary\...\Everything is Crab\BepInEx\plugins`).
On `Release` builds the `PackagePlugin` target additionally assembles a Thunderstore-compatible zip at `../dist/<PackageAuthor>-<PackageName>-<PluginVersion>.zip` from `../Packaging/` (manifest + icon + README + CHANGELOG) plus the built DLL. Bump `<PluginVersion>` in the csproj, the const in `MyPluginInfo.cs`, and `version_number` in `Packaging/manifest.json` together when releasing.

## Tooling installed

- **BepInEx 6 BE 755 IL2CPP** — in the game folder; bootstraps via `winhttp.dll` doorstop
- **UnityExplorer 4.13.6** (yukieiji fork — sinai-dev's archive is too old for Unity 6) in `BepInEx/plugins/sinai-dev-UnityExplorer/`. Default toggle key F7.
- **`ilspycmd`** in the Linux sandbox at `/tmp/tools/ilspycmd` — use to decompile interop DLLs when figuring out class internals. Example:
  ```
  ilspycmd Assembly-CSharp.dll -t GameFlow.PlayIntroSplashScreen
  ```

## Gotchas

- The IL2CPP interop wrappers expose what looks like a private field as a **public property** on the wrapper class. So `AccessTools.Field(typeof(X), "_something")` returns null. Use direct property access (`__instance._something = value`) or `AccessTools.Property(...)`.
- Backing fields for auto-properties become double-underscore prefixed in the wrapper (e.g. `CurrentState` ↔ `__currentState`).
- `Nullable>enable</Nullable>` doesn't work with this project — the 176 interop DLLs include their own `Il2Cppmscorlib` which shadows `System.Runtime.CompilerServices.NullableAttribute`. Keep nullable disabled.
- The sandbox's Windows-mount filesystem occasionally pads shortened file overwrites with null bytes via the `Write` tool. If C# parser errors mention "unexpected character" at the file's end, rewrite via bash heredoc instead.
