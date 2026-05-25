# Changelog

## 0.2.0

- Added a "wait for extra readiness" gate (default on, configurable). The splash transition now holds until Analytics, PurchasablesSyncerRuntimeManager, and CosmeticsRuntimeManager all report ready, in addition to the Localisation + RemoteConfig gates the splash already checks.
- New config: `[Behavior] WaitForExtraReadiness` (default true). Set to false to revert to v0.1.3 behavior.
- 30s safety timeout: if any extra gate hangs (e.g. analytics consent flow stalled), the transition fires anyway and logs which gates were still pending.

## 0.1.3

- Regression fix: 0.1.2 lowered the splash screen's overall safety timeout to 5s. The devs use that timeout (about 30s by default) as a safety net for slow hardware to actually finish loading. Restored: we no longer touch that field at all.
- Net behavior: the only fields we mutate are the two video-wait timeouts. The game's full readiness pipeline (localisation, remote config, the 30s safety net) is now untouched.
- Recommended update.

## 0.1.2

- Fixed a race condition where the previous patch could cause the main menu to load before localisation tables and remote config finished, leading to missing strings or unapplied balance config on some hardware.
- New approach: skip the splash by zeroing the video wait-timeouts and stopping the video players. The game's own async-readiness gating (OnLocalisationReady, OnRemoteConfigReady, TryMoveToMainMenu) is untouched.
- Recommended update for all users.

## 0.1.1

- Renamed plugin assembly to `SkipIntroPlugin.dll` (was `EverythingIsCrabPlugin.dll`).
- If updating manually, delete the old `EverythingIsCrabPlugin.dll` from your `BepInEx/plugins/Bungus-SkipIntro/` folder before installing this version. Mod managers handle this on uninstall + reinstall.

## 0.1.0

- Initial release. Skips both intro videos.
