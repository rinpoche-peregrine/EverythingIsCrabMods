using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using System.Linq;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace SkipIntroPlugin;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin {
	internal static new ManualLogSource Log;
	internal static new ModConfig Config;

	public override void Load() {
		Log = base.Log;
		Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} loading. Crab time.");
		Config = new ModConfig(base.Config);

		try {
			Il2CppInterop.Runtime.Injection.ClassInjector.RegisterTypeInIl2Cpp<SplashReadinessGate>();
			Log.LogInfo("SplashReadinessGate registered.");
		} catch (System.Exception ex) {
			Log.LogError($"Failed to register SplashReadinessGate: {ex.Message}");
		}

		var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
		harmony.PatchAll(typeof(Plugin).Assembly);
		Log.LogInfo($"Harmony patches applied: {harmony.GetPatchedMethods().Count()}");
	}
}

public class ModConfig {
	public ConfigEntry<bool> WaitForExtraReadiness;

	public ModConfig(ConfigFile cfg) {
		WaitForExtraReadiness = cfg.Bind("Behavior", "WaitForExtraReadiness", true,
			"Hold the splash-screen transition until Analytics, Purchasables, and Cosmetics systems report ready, in addition to the game's own Localisation + RemoteConfig gates. Adds a small extra wait on slow hardware in exchange for a guaranteed-fully-loaded main menu. Falls back to a 30s safety timeout if any gate hangs.");
	}
}
