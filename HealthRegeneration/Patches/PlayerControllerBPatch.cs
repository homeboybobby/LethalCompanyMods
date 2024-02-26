using GameNetcodeStuff;
using HarmonyLib;
using System.Collections;
using UnityEngine;

namespace HealthRegeneration.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB), "Update")]
    internal class PlayerControllerBPatch
    {

        private static bool IsHealing;

        public static void Prefix(ref PlayerControllerB __instance)
        {
            if (!(__instance.health < 100) || IsHealing) return;
            __instance.StartCoroutine(Heal(__instance, 10));
        }

        private static IEnumerator Heal(PlayerControllerB player, float healRate)
        {
            IsHealing = true;
            yield return new WaitForSeconds(healRate);
            
            if (player.health < 100 && !player.isPlayerDead)
            {
                int HealAmountCritical = 1;
                int HealAmount = 5;

                if (player.criticallyInjured || player.health <= 10)
                {
                    player.health += HealAmountCritical;
                } else
                {
                    if (player.health + HealAmount >= 100)
                    {
                        int HealedHealth = 100 - player.health;
                        player.health = 100;
                        player.DamagePlayerServerRpc(-HealedHealth, player.health);
                    } else
                    {
                        player.health += HealAmount;
                        player.DamagePlayerServerRpc(-HealAmount, player.health);
                    }

                    HUDManager.Instance.UpdateHealthUI(player.health, false);
                }

                HealthRegenerationPlugin.mls.LogInfo($"Updated Health {player.health} at {Time.time}");
            }
            
            IsHealing = false;
        }
    }
}