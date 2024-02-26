using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace BetterJetpack.Patches
{
    [HarmonyPatch(typeof(JetpackItem), "Update")]
    internal class JetpackItemPatch
    {
        private static FieldInfo RayHit = typeof(JetpackItem).GetField("rayHit", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo Forces = typeof(JetpackItem).GetField("forces", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo JetpackPower = typeof(JetpackItem).GetField("jetpackPower", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo JetpackActivated = typeof(JetpackItem).GetField("jetpackActivated", BindingFlags.Instance | BindingFlags.NonPublic);

        public static void Prefix(ref JetpackItem __instance)
        {
            if (__instance.playerHeldBy == null || !__instance.IsOwner || __instance.playerHeldBy != GameNetworkManager.Instance.localPlayerController)
            {
                return;
            }

            if (__instance != null)
            {
                if (RayHit != null && Forces != null && JetpackPower != null && JetpackActivated != null)
                {
                    RaycastHit hitInfo = (RaycastHit) RayHit.GetValue(__instance);
                    Vector3 forces = (Vector3) Forces.GetValue(__instance);
                    float power = (float) JetpackPower.GetValue(__instance);
                    bool activated = (bool) JetpackActivated.GetValue(__instance);

                    __instance.playerHeldBy.takingFallDamage = false;
                    __instance.playerHeldBy.averageVelocity = 0;
                    __instance.itemProperties.requiresBattery = false;

                    if (activated)
                    {
                        power = Mathf.Clamp(power + Time.deltaTime * 10f, 0f, 500f);
                    } else
                    {
                        power = Mathf.Clamp(power - Time.deltaTime * 75f, 0f, 1000f);
                        if (__instance.playerHeldBy.thisController.isGrounded)
                        {
                            power = 0;
                        }
                    }

                    forces = Vector3.Lerp(forces, Vector3.ClampMagnitude(__instance.playerHeldBy.transform.up * power, 400f), Time.deltaTime);
                    if (!__instance.playerHeldBy.isPlayerDead && Physics.Raycast(__instance.playerHeldBy.transform.position, forces, out hitInfo, 25f, StartOfRound.Instance.allPlayersCollideWithMask) && forces.magnitude - hitInfo.distance > 50f && hitInfo.distance < 4f)
                    {
                        PlayerControllerB playerHeldBy = __instance.playerHeldBy;
                        playerHeldBy.externalForces += forces;
                        throw new Exception("Sacking original update method.");
                    }
                }
                else
                {
                    BetterJetpackPlugin.mls.LogError($"Unable to patch {nameof(JetpackItem)} because the fields required were not found.");
                }
            } else
            {
                BetterJetpackPlugin.mls.LogError($"Failed to patch since {nameof(__instance)} is null!");
            }
        }
    }
}