using BepInEx;
using BepInEx.Logging;
using System.Linq;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace SkipIntroPlugin;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin {
	internal static new ManualLogSource Log;

	public override void Load() {
		Log = base.Log;
		Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} loading. Crab time.");
		var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
		harmony.PatchAll(typeof(Plugin).Assembly);
		Log.LogInfo($"Harmony patches applied: {harmony.GetPatchedMethods().Count()}");
	}
}
