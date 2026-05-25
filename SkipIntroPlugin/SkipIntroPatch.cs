using System;
using GameFlow;
using HarmonyLib;
using UnityEngine.Video;

namespace SkipIntroPlugin;

// Awake postfix: zero the video-prep / video-play timeouts and stop the video players.
// Then attach a SplashReadinessGate component to poll extra readiness signals.
[HarmonyPatch(typeof(PlayIntroSplashScreen), nameof(PlayIntroSplashScreen.Awake))]
public static class SkipIntroAwakePatch {
	[HarmonyPostfix]
	public static void Postfix(PlayIntroSplashScreen __instance) {
		try {
			__instance._timeToWaitForVideoPreparation = 0f;
			__instance._timeToWaitForVideoToPlay = 0f;
			StopVideo(__instance._oddVideoPlayer);
			StopVideo(__instance._smVideoPlayer);

			SkipIntroGatedTransitionPatch.TransitionFired = false;

			if (Plugin.Config.WaitForExtraReadiness.Value) {
				__instance.gameObject.AddComponent<SplashReadinessGate>();
				Plugin.Log.LogInfo("SkipIntroPatch: zeroed video timeouts, stopped video players, attached extra-readiness gate.");
			} else {
				Plugin.Log.LogInfo("SkipIntroPatch: zeroed video timeouts, stopped video players. Extra-readiness gate disabled via config.");
			}
		} catch (Exception ex) {
			Plugin.Log.LogError($"SkipIntroAwakePatch failed: {ex}");
		}
	}

	static void StopVideo(VideoPlayer vp) {
		if (vp == null) return;
		try { vp.Stop(); } catch { }
		try { vp.playOnAwake = false; } catch { }
	}
}

// Prefix on TryMoveToMainMenu. Suppress the call unless the gate is satisfied. When a call
// is allowed through, mark TransitionFired so the gate's Update loop won't retry.
[HarmonyPatch(typeof(PlayIntroSplashScreen), nameof(PlayIntroSplashScreen.TryMoveToMainMenu))]
public static class SkipIntroGatedTransitionPatch {
	internal static bool TransitionFired;

	[HarmonyPrefix]
	public static bool Prefix() {
		if (TransitionFired) return true; // idempotent: let any later calls through
		if (!Plugin.Config.WaitForExtraReadiness.Value) {
			TransitionFired = true;
			return true;
		}
		if (SplashReadinessGate.ExtraGatesReady || SplashReadinessGate.SafetyTimedOut) {
			TransitionFired = true;
			return true;
		}
		return false; // gate not satisfied yet — skip
	}
}
