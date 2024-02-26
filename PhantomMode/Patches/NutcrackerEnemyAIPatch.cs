using HarmonyLib;

namespace PhantomMode.Patches
{
    [HarmonyPatch(typeof(NutcrackerEnemyAI), "Update")]
    internal class NutcrackerEnemyAIPatch
    {
        private static int LastPlayerFocusedOn = -1;

        public static void Postfix(ref NutcrackerEnemyAI __instance)
        {
            if (PlayerControllerBPatch.IsPhantomMode)
            {
                if ((int) GameNetworkManager.Instance.localPlayerController.playerClientId == __instance.lastPlayerSeenMoving)
                {
                    __instance.lastPlayerSeenMoving = LastPlayerFocusedOn;
                    PhantomModePlugin.mls.LogInfo("Nutcracker spotted phantom player. Ignoring.");
                } else
                {
                    LastPlayerFocusedOn = __instance.lastPlayerSeenMoving;
                }
            }
        }

    }
}