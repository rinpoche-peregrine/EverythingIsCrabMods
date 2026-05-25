using System;
using Cosmetics;
using EIC.Analytics;
using GameFlow;
using Purchasables;
using UnityEngine;

namespace SkipIntroPlugin;

// Polls extra readiness signals each frame. Two responsibilities:
//   1. Maintain the static ExtraGatesReady / SafetyTimedOut flags read by the prefix.
//   2. If the splash's natural TryMoveToMainMenu call was suppressed (because gates
//      weren't ready yet), retry it once after gates become ready.
//
// The prefix sets SkipIntroGatedTransitionPatch.TransitionFired when any call passes
// through. We only retry while that flag is false, so we never double-fire the transition.
public class SplashReadinessGate : MonoBehaviour {
	const float SafetyTimeoutSeconds = 30f;

	internal static bool ExtraGatesReady;
	internal static bool SafetyTimedOut;

	PlayIntroSplashScreen _splash;
	float _spawnedAt;
	bool _loggedReady;
	bool _loggedTimeout;

	public SplashReadinessGate(IntPtr ptr) : base(ptr) { }

	void Start() {
		_spawnedAt = Time.unscaledTime;
		_splash = GetComponent<PlayIntroSplashScreen>();
		ExtraGatesReady = false;
		SafetyTimedOut = false;
		_loggedReady = false;
		_loggedTimeout = false;
	}

	void Update() {
		if (_splash == null) return;

		var gates = CheckGates();
		ExtraGatesReady = gates.AllReady;

		var elapsed = Time.unscaledTime - _spawnedAt;
		if (!gates.AllReady && elapsed >= SafetyTimeoutSeconds) {
			if (!_loggedTimeout) {
				Plugin.Log.LogWarning($"SplashReadinessGate: safety timeout after {SafetyTimeoutSeconds:F0}s. Pending gates: {gates.Pending}. Transitioning anyway.");
				_loggedTimeout = true;
			}
			SafetyTimedOut = true;
		}

		if (gates.AllReady && !_loggedReady) {
			Plugin.Log.LogInfo($"SplashReadinessGate: all extra gates ready in {elapsed:F2}s.");
			_loggedReady = true;
		}

		// Retry only if the splash's own call was previously suppressed and we're now allowed.
		if (!SkipIntroGatedTransitionPatch.TransitionFired
			&& (gates.AllReady || SafetyTimedOut)
			&& _splash.__currentState == PlayIntroSplashScreen.ELoadingState.Ready) {
			try { _splash.TryMoveToMainMenu(); }
			catch (Exception ex) { Plugin.Log.LogError($"SplashReadinessGate retry transition failed: {ex.Message}"); }
		}
	}

	struct GateCheck { public bool AllReady; public string Pending; }

	static GateCheck CheckGates() {
		var pending = "";

		bool analyticsReady = false;
		try {
			var ana = UgsAnalytics.AnalyticsInitialized;
			analyticsReady = ana != null && ana.IsReady;
		} catch { analyticsReady = true; }
		if (!analyticsReady) pending += "Analytics ";

		bool purchasablesReady = false;
		try {
			var p = PurchasablesSyncerRuntimeManager.Instance;
			purchasablesReady = p != null && p.IsReady;
		} catch { purchasablesReady = true; }
		if (!purchasablesReady) pending += "Purchasables ";

		bool cosmeticsReady = false;
		try {
			var c = CosmeticsRuntimeManager.Instance;
			cosmeticsReady = c != null && c.IsReady;
		} catch { cosmeticsReady = true; }
		if (!cosmeticsReady) pending += "Cosmetics ";

		return new GateCheck {
			AllReady = analyticsReady && purchasablesReady && cosmeticsReady,
			Pending = pending.TrimEnd(),
		};
	}
}
