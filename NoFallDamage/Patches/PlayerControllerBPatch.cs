using GameNetcodeStuff;
using HarmonyLib;

namespace NoFallDamage.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB), "Update")]
    internal class PlayerControllerBPatch
    {
        public static void Postfix(ref bool ___takingFallDamage) => ___takingFallDamage = false; 
    }

}