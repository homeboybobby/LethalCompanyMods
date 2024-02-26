using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine.InputSystem;
using static PhantomMode.PluginInfo;

namespace PhantomMode
{
    [BepInPlugin(MOD_GUID, MOD_NAME, MOD_VERSION)]
    public class PhantomModePlugin : BaseUnityPlugin
    {
        public static ManualLogSource mls = BepInEx.Logging.Logger.CreateLogSource(MOD_GUID);
        private static PhantomModePlugin Instance;
        
        void Awake()
        {
            if (Instance == null) Instance = this;
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), MOD_GUID);
            mls.LogInfo("Plugin is awake");
            SetConfig();
        }

        private void SetConfig()
        {
            ConfigEntry<string> StartPhantomModeButton = Config.Bind("Key Bindings", "Enable Button", "M");
            ConfigEntry<string> TeleportToDeadBodyButton = Config.Bind("Key Bindings", "Teleport To Dead Body", "P");
            ConfigEntry<string> ToggleBrightModeButton = Config.Bind("Key Bindings", "Toggle Bright Mode", "C");
            ConfigEntry<string> TeleportToEntranceButton = Config.Bind("Key Bindings", "Teleport To Entrance", "B");
            ConfigEntry<string> SwitchToSpectateModeButton = Config.Bind("Key Bindings", "Switch To Spectate Mode", "R");
            ConfigEntry<string> ToggleFreeRoamButton = Config.Bind("Key Bindings", "Toggle Free Roam", "O");
            
            ConfigEntry<float> WaitTimeBetweenInteractions = Config.Bind("Phantom Mode", "Interaction Delay", 45f);
            ConfigEntry<float> FreeRoamFlightSpeed = Config.Bind("Phantom Mode", "Free Roam Flight Speed", 0.3f);

            Variables.WaitTimeBetweenInteractions = WaitTimeBetweenInteractions.Value;
            Variables.FreeRoamSpeed = FreeRoamFlightSpeed.Value;

            Variables.StartPhantomModeButton = ValidateAndAssignButton(StartPhantomModeButton, "M");
            Variables.TeleportToDeadBodyButton = ValidateAndAssignButton(TeleportToDeadBodyButton, "P");
            Variables.ToggleBrightModeButton = ValidateAndAssignButton(ToggleBrightModeButton, "C");
            Variables.TeleportToEntranceButton = ValidateAndAssignButton(TeleportToEntranceButton, "B");
            Variables.SwitchToSpectateModeButton = ValidateAndAssignButton(SwitchToSpectateModeButton, "R");
            Variables.ToggleFreeRoamButton = ValidateAndAssignButton(ToggleFreeRoamButton, "O");
        }

        private string ValidateAndAssignButton(ConfigEntry<string> configEntry, string defaultButton)
        {
            if (Enum.IsDefined(typeof(Key), configEntry.Value))
            {
                return configEntry.Value;
            }

            mls.LogError($"{configEntry.Value} is not a valid key. Choose another key.");
            configEntry.Value = defaultButton;
            return defaultButton;
        }

        public static Key GetButton(string buttonName) => (Key)Enum.Parse(typeof(Key), buttonName);
    }

}