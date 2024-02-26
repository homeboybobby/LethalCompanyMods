using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine.InputSystem;
using static SuperSpeed.PluginInfo;

namespace SuperSpeed
{
    [BepInPlugin(MOD_GUID, MOD_NAME, MOD_VERSION)]
    internal class SuperSpeedPlugin : BaseUnityPlugin
    {
        public static ManualLogSource mls = BepInEx.Logging.Logger.CreateLogSource(MOD_GUID);
        private static SuperSpeedPlugin Instance;

        void Awake()
        {
            if (Instance == null) Instance = this;
            SetConfig();
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), MOD_GUID);
            mls.LogInfo("Plugin is awake");
        }

        void SetConfig()
        {
            ConfigEntry<float> SprintMultiplier = Config.Bind("General", "Sprint Multiplier", 1.05f);
            ConfigEntry<float> MaxSprintSpeed = Config.Bind("General", "Max Sprint Speed", 15f);
            ConfigEntry<float> WalkMultiplier = Config.Bind("General", "Walk Multiplier", 1.05f);
            ConfigEntry<float> MaxWalkSpeed = Config.Bind("General", "Max Walk Speed", 8f);
            ConfigEntry<string> SuperSpeedButton = Config.Bind("General", "Super Speed Key", "M");

            Variables.DefaultSprintMultiplier = SprintMultiplier.Value;
            Variables.DefaultMaxSprintSpeed = MaxSprintSpeed.Value;
            Variables.WalkMultiplier = WalkMultiplier.Value;
            Variables.MaxWalkSpeed = MaxWalkSpeed.Value;
            Variables.SuperSpeedButton = SuperSpeedButton.Value;
        }

        public static Key GetSuperSpeedButton() => (Key)Enum.Parse(typeof(Key), Variables.SuperSpeedButton);
    }
}