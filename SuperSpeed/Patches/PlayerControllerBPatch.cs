using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using System.Collections;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SuperSpeed.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerControllerBPatch
    {
        private static readonly FieldInfo SprintMultiplierField = typeof(PlayerControllerB).GetField("sprintMultiplier", BindingFlags.Instance | BindingFlags.NonPublic);

        private static float IncreasedSprintMultiplier = 10f;
        private static float ÏncreasedMaxSprintSpeed = 1500f;
        private static float SprintMultiplier;
        private static float MaxSprintSpeed;
        private static bool AdjustingSpeed;
        private static int SpeedMode;

        [HarmonyPatch("Start")]
        [HarmonyPrefix]
        public static void StartPrefix()
        {
            SprintMultiplier = Variables.DefaultSprintMultiplier;
            MaxSprintSpeed = Variables.DefaultMaxSprintSpeed;
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        private static void UpdatePrefix(PlayerControllerB __instance)
        {
            if (Reflection.GetFieldValue<bool>(__instance, "isWalking"))
            {
                object speed = SprintMultiplierField.GetValue(__instance);
                if (__instance.isSprinting)
                {
                    float sprintSpeed = (float)speed * SprintMultiplier;
                    if (sprintSpeed < MaxSprintSpeed)
                    {
                        SprintMultiplierField.SetValue(__instance, sprintSpeed);
                    }
                }
                else
                {
                    if ((float)speed > Variables.MaxWalkSpeed)
                    {
                        SprintMultiplierField.SetValue(__instance, Variables.MaxWalkSpeed);
                    }
                    float walkSpeed = (float)speed * Variables.WalkMultiplier;
                    if (walkSpeed < Variables.MaxWalkSpeed)
                    {
                        SprintMultiplierField.SetValue(__instance, walkSpeed);
                    }
                }
            }
            if (__instance.IsOwner && !__instance.inTerminalMenu && !__instance.isTypingChat && Keyboard.current[SuperSpeedPlugin.GetSuperSpeedButton()].wasPressedThisFrame && !AdjustingSpeed)
            {
                AdjustingSpeed = true;
                __instance.StartCoroutine(SpeedToggle(__instance));
            }
        }

        private static IEnumerator SpeedToggle(PlayerControllerB player)
        {
            yield return new WaitForSeconds(1f);

            if (SpeedMode == 0)
            {
                SuperSpeedPlugin.mls.LogMessage("Enabled Super Speed!");
                HUDManager.Instance.DisplayTip("Super Speed", "Activated");
                SpeedMode = 1;
                SprintMultiplier = IncreasedSprintMultiplier;
                MaxSprintSpeed = ÏncreasedMaxSprintSpeed;
            }
            else if (SpeedMode == 1)
            {
                SuperSpeedPlugin.mls.LogMessage("Disabled Super Speed!");
                HUDManager.Instance.DisplayTip("Super Speed", "Deactivated");
                SpeedMode = 0;
                SprintMultiplier = Variables.DefaultSprintMultiplier;
                MaxSprintSpeed = Variables.DefaultMaxSprintSpeed;
            }

            AdjustingSpeed = false;
        }
    }
}