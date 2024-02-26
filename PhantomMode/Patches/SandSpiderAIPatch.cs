using GameNetcodeStuff;
using HarmonyLib;

namespace PhantomMode.Patches
{
    [HarmonyPatch(typeof(SandSpiderAI), nameof(SandSpiderAI.TriggerChaseWithPlayer))]
    internal class SandSpiderAIPatch
    {
        public static bool Prefix(ref PlayerControllerB playerScript)
        {
            if (PlayerControllerBPatch.IsPhantomMode && StartOfRound.Instance.localPlayerController.playerClientId == playerScript.playerClientId)
            {
                PhantomModePlugin.mls.LogInfo("Spider tried to trigger chase with player, but the player was a phantom.");
                return false;
            }

            return true;
        }
    }
}