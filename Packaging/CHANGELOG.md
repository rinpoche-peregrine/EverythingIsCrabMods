# Changelog

## 0.1.2

- Fixed a race condition where the previous patch could cause the main menu to load before localisation tables and remote config finished, leading to missing strings or unapplied balance config on some hardware.
- New approach: skip the splash by zeroing the video wait-timeouts and stopping the video players. The game's own async-readiness gating (OnLocalisationReady, OnRemoteConfigReady, TryMoveToMainMenu) is untouched.
- Recommended update for all users.


## 0.1.1

- Renamed plugin assembly to `SkipIntroPlugin.dll` (was `EverythingIsCrabPlugin.dll`).
- If updating manually, delete the old `EverythingIsCrabPlugin.dll` from your `BepInEx/plugins/Bungus-SkipIntro/` folder before installing this version. Mod managers handle this on uninstall + reinstall.

## 0.1.0

- Initial release. Skips both intro videos.
