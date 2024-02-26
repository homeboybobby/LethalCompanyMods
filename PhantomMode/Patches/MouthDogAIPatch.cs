using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace PhantomMode.Patches
{
    [HarmonyPatch(typeof(MouthDogAI), nameof(MouthDogAI.DetectNoise))]
    internal class MouthDogAIPatch
    {

        public static bool Prefix(ref Vector3 noisePosition)
        {
            if (PlayerControllerBPatch.IsPhantomMode)
            {
                PlayerControllerB[] allPlayerScripts = StartOfRound.Instance.allPlayerScripts;
                ulong playerClientId = StartOfRound.Instance.localPlayerController.playerClientId;
                PlayerControllerB player = allPlayerScripts[playerClientId];
                
                if (Common.ArePointsClose(noisePosition, player.transform.position, 2f))
                {
                    PhantomModePlugin.mls.LogInfo("Heard player, but player is a phantom. Ignoring.");
                    return false;
                }
            }

            return true;
        }

    }
}