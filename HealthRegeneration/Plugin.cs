using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;
using static HealthRegeneration.PluginInfo;

namespace HealthRegeneration
{
    [BepInPlugin(MOD_GUID, MOD_NAME, MOD_VERSION)]
    public class HealthRegenerationPlugin : BaseUnityPlugin
    {
        public static ManualLogSource mls = BepInEx.Logging.Logger.CreateLogSource(MOD_GUID);
        private static HealthRegenerationPlugin Instance;

        void Awake()
        {
            if (Instance == null) Instance = this;
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), MOD_GUID);
            mls.LogInfo("Plugin is awake");
        }
    }
}