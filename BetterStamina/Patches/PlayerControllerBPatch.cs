using GameNetcodeStuff;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace BetterStamina.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB), "Update")]
    internal class PlayerControllerBPatch
    {
        private static FieldInfo IsWalking = typeof(PlayerControllerB).GetField("isWalking", BindingFlags.Instance | BindingFlags.NonPublic);

        public static void Postfix(ref PlayerControllerB __instance)
        {
            __instance.sprintTime = 19f;
            float recharge = 22f;
            float depletion = 13f;
            if (IsWalking != null)
            {
                bool walking = (bool) IsWalking.GetValue(__instance);
                if (!__instance.isPlayerControlled || __instance.isPlayerDead)
                {
                    return;
                }

                float multiplier = 1f;

                if (!__instance.isSprinting && __instance.isMovementHindered <= 0)
                {
                    if (walking)
                    {
                        __instance.sprintMeter = Mathf.Clamp(__instance.sprintMeter + Time.deltaTime / depletion * multiplier, 0f, 1f);
                    }  else
                    {
                        __instance.sprintMeter = Mathf.Clamp(__instance.sprintMeter + Time.deltaTime / recharge * multiplier, 0f, 1f);
                    }

                    if (__instance.isExhausted && __instance.sprintMeter > 0.2f)
                    {
                        __instance.isExhausted = false;
                    }
                }
                __instance.sprintMeterUI.fillAmount = __instance.sprintMeter;
            } else
            {
                BetterStaminaPlugin.mls.LogError($"Unable to find the {nameof(IsWalking)} field");
            }
        }
    }
}