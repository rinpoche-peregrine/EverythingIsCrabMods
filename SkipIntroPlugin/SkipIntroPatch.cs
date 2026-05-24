using System;
using GameFlow;
using HarmonyLib;

namespace SkipIntroPlugin;

// Postfix on Start: stop the splash coroutines before they can run, jump CurrentState
// straight to Ready, and trigger the main-menu transition. Start() returns IEnumerator
// (it's the splash coroutine itself), so the postfix fires right after it's queued —
// stopping coroutines here cancels the queued tick before it processes a frame.
[HarmonyPatch(typeof(PlayIntroSplashScreen), nameof(PlayIntroSplashScreen.Start))]
public static class SkipIntroPatch {
	[HarmonyPostfix]
	public static void Postfix(PlayIntroSplashScreen __instance) {
		try {
			__instance.StopAllCoroutines();
			__instance.__currentState = PlayIntroSplashScreen.ELoadingState.Ready;
			__instance.TryMoveToMainMenu();
			Plugin.Log.LogInfo("SkipIntroPatch: stopped coroutines, set CurrentState=Ready, called TryMoveToMainMenu");
		} catch (Exception ex) {
			Plugin.Log.LogError($"SkipIntroPatch failed: {ex}");
		}
	}
}
