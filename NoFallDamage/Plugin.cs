using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;
using static NoFallDamage.PluginInfo;

namespace NoFallDamage
{
    [BepInPlugin(MOD_GUID, MOD_NAME, MOD_VERSION)]
    public class NoFallDamagePlugin : BaseUnityPlugin
    {
        private static ManualLogSource mls = BepInEx.Logging.Logger.CreateLogSource(MOD_GUID);
        private static NoFallDamagePlugin Instance;

        void Awake()
        {
            if (Instance == null) Instance = this;
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), MOD_GUID);
            mls.LogInfo("Plugin is awake");
        }
    }
}