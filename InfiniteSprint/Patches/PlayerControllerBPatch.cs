using GameNetcodeStuff;
using HarmonyLib;

namespace InfiniteSprint.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB), "Update")]
    internal class PlayerControllerBPatch
    {
        public static void Postfix(ref float ___sprintMeter) => ___sprintMeter = 1f;
    }
}