using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace PhantomMode.Patches
{
    [HarmonyPatch(typeof(ForestGiantAI), nameof(ForestGiantAI.OnCollideWithPlayer))]
    internal class ForestGiantAIPatch
    {
        public static bool Prefix(ref Collider other)
        {
            if (PlayerControllerBPatch.IsPhantomMode)
            {
                PlayerControllerB player = other.gameObject.GetComponent<PlayerControllerB>();
                if (StartOfRound.Instance.localPlayerController.playerClientId == player.playerClientId)
                {
                    PhantomModePlugin.mls.LogInfo("Stopped Forest Giant from eating phantom!");
                    return false;
                }
            }

            return true;
        }
    }
}