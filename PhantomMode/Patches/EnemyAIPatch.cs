using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace PhantomMode.Patches
{
    [HarmonyPatch(typeof(EnemyAI))]
    internal class EnemyAIPatch
    {

        [HarmonyPatch(nameof(EnemyAI.OnCollideWithPlayer))]
        [HarmonyPrefix]
        public static bool OnCollideWithPlayerPrefix(EnemyAI __instance, ref Collider other)
        {
            if (PlayerControllerBPatch.IsPhantomMode)
            {
                PlayerControllerB player = other.gameObject.GetComponent<PlayerControllerB>();
                if (StartOfRound.Instance.localPlayerController.playerClientId == player.playerClientId)
                {
                    PhantomModePlugin.mls.LogInfo("Enemy tried to collide with player, but player is a phantom.");
                    return false;
                }
            }

            return true;
        }

        [HarmonyPatch(nameof(EnemyAI.PlayerIsTargetable))]
        [HarmonyPrefix]
        public static void PlayerIsTargetablePrefix(ref bool cannotBeInShip, ref PlayerControllerB playerScript)
        {
            if (PlayerControllerBPatch.IsPhantomMode)
            {
                playerScript.isInHangarShipRoom = true;
                cannotBeInShip = true;
            }
        }

        [HarmonyPatch(nameof(EnemyAI.GetAllPlayersInLineOfSight))]
        [HarmonyPostfix]
        public static void GetAllPlayersInLineOfSightPostfix(EnemyAI __instance, ref PlayerControllerB[] __result)
        {
            if (!PlayerControllerBPatch.IsPhantomMode)
            {
                return;
            }

            PlayerControllerB[] allPlayerScripts = StartOfRound.Instance.allPlayerScripts;
            ulong playerClientId = StartOfRound.Instance.localPlayerController.playerClientId;
            
            if (__result == null || __result.Length == 0)
            {
                return;
            }

            int phantom = -1;

            for (int i = 0; i < __result.Length; i++)
            {
                if (__result[i] == allPlayerScripts[playerClientId])
                {
                    phantom = i;
                    break;
                }
            }

            if (phantom == -1)
            {
                return;
            }

            PlayerControllerB[] players = (PlayerControllerB[])(object)new PlayerControllerB[__result.Length - 1];
            int pi = 0;
            for (int j = 0; j < __result.Length; j++)
            {
                if (j != phantom)
                {
                    players[pi] = __result[j];
                    pi++;
                }
            }
            __result = players;
        }

        [HarmonyPatch(nameof(EnemyAI.KillEnemy))]
        [HarmonyPrefix]
        public static bool KillEnemyPrefix(ref EnemyAI __instance)
        {
            PhantomModePlugin.mls.LogInfo("Enemy got hit - KillEnemy Patch");
            if (PlayerControllerBPatch.IsPhantomMode && __instance.GetClosestPlayer().playerClientId == StartOfRound.Instance.localPlayerController.playerClientId)
            {
                PhantomModePlugin.mls.LogInfo("Phantom tried to kill enemy");
                return false;
            }

            return true;
        }
    }
}