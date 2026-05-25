using System;
using GameFlow;
using HarmonyLib;
using UnityEngine.Video;

namespace SkipIntroPlugin;

// Postfix on Awake: collapse the splash screen's wait-timeouts to zero and stop the
// video players so they don't actually play. We DO NOT touch CurrentState, DO NOT stop
// any coroutines, and DO NOT call TryMoveToMainMenu manually.
//
// The splash's coroutines still run normally and still gate the transition on
// OnLocalisationReady + OnRemoteConfigReady. Once those async systems finish their work
// and the (now-instant) video-wait timeouts have elapsed, the game's own state machine
// flips CurrentState to Ready and TryMoveToMainMenu fires the scene transition cleanly.
[HarmonyPatch(typeof(PlayIntroSplashScreen), nameof(PlayIntroSplashScreen.Awake))]
public static class SkipIntroPatch {
	[HarmonyPostfix]
	public static void Postfix(PlayIntroSplashScreen __instance) {
		try {
			__instance._timeToWaitForVideoPreparation = 0f;
			__instance._timeToWaitForVideoToPlay = 0f;
			// Keep the failsafe upper bound but tighten it. If async readiness for some
			// reason stalls past this, the splash will still force-transition, same as
			// before, but on a fast clock.
			__instance._maxTimeToWaitInTotalBeforeForcingMoveToNextScene = 5f;

			// Stop the video players from actually playing the intro footage.
			StopVideo(__instance._oddVideoPlayer);
			StopVideo(__instance._smVideoPlayer);

			Plugin.Log.LogInfo("SkipIntroPatch: zeroed splash wait-timeouts and stopped video players. Localisation/RemoteConfig readiness gating left intact.");
		} catch (Exception ex) {
			Plugin.Log.LogError($"SkipIntroPatch failed: {ex}");
		}
	}

	static void StopVideo(VideoPlayer vp) {
		if (vp == null) return;
		try { vp.Stop(); } catch { }
		try { vp.playOnAwake = false; } catch { }
	}
}
