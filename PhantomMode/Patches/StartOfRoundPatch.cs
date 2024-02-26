using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace PhantomMode.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal class StartOfRoundPatch
    {
        [HarmonyPatch(nameof(StartOfRound.ReviveDeadPlayers))]
        [HarmonyPrefix]
        public static void ReviveDeadPlayersPrefix(ref StartOfRound __instance)
        {
            PhantomModePlugin.mls.LogInfo("Revived dead players.");
            PlayerControllerBPatch.ResetPhantomMode(__instance.localPlayerController);
        }

        [HarmonyPatch("OnPlayerConnectedClientRpc")]
        [HarmonyPrefix]
        private static void OnPlayerConnectedClientRpcPrefix(StartOfRound __instance, ref ulong clientId)
        {
            PhantomModePlugin.mls.LogMessage($"Player connected, clientId: {clientId}");
            if (__instance.localPlayerController == null)
            {
                PlayerControllerBPatch.ResetPhantomMode(null);
            }
        }

        [HarmonyPatch("ShipLeave")]
        [HarmonyPrefix]
        public static void ShipLeavePrefix(StartOfRound __instance)
        {
            PhantomModePlugin.mls.LogMessage("Rekill player locally called because ship is taking off");
            if (PlayerControllerBPatch.IsPhantomMode)
            {
                PlayerControllerBPatch.ResetPhantomMode(__instance.localPlayerController);
                PlayerControllerBPatch.RekillPlayerLocally(__instance.localPlayerController, gameOver: true);
            }
        }

        [HarmonyPatch(nameof(StartOfRound.StartGame))]
        [HarmonyPrefix]
        public static void StartGamePrefix(StartOfRound __instance)
        {
            PhantomModePlugin.mls.LogMessage("Rekill player locally called because level is starting");
            if (PlayerControllerBPatch.IsPhantomMode)
            {
                PlayerControllerBPatch.ResetPhantomMode(__instance.localPlayerController);
            }
        }

        [HarmonyPatch(nameof(StartOfRound.UpdatePlayerVoiceEffects))]
        [HarmonyPrefix]
        public static bool UpdatePlayerVoiceEffectsPrefix(StartOfRound __instance)
        {
            if (PlayerControllerBPatch.IsPhantomMode)
            {
                for (int i = 0; i < __instance.allPlayerScripts.Length; i++)
                {
                    PlayerControllerB player = __instance.allPlayerScripts[i];
                    if (player == GameNetworkManager.Instance.localPlayerController)
                    {
                        continue;
                    }
                    if (player.voicePlayerState == null || player.currentVoiceChatIngameSettings._playerState == null || player.currentVoiceChatAudioSource == null || player.currentVoiceChatIngameSettings == null)
                    {
                        __instance.RefreshPlayerVoicePlaybackObjects();
                        if (player.voicePlayerState == null || player.currentVoiceChatAudioSource == null)
                        {
                            continue;
                        }
                    }
                    AudioSource currentVoiceChatAudioSource = StartOfRound.Instance.allPlayerScripts[i].currentVoiceChatAudioSource;
                    if (player.isPlayerDead)
                    {
                        player.currentVoiceChatIngameSettings.set2D = true;
                        currentVoiceChatAudioSource.volume = 1f;
                        currentVoiceChatAudioSource.GetComponent<AudioLowPassFilter>().enabled = false;
                        currentVoiceChatAudioSource.GetComponent<AudioHighPassFilter>().enabled = false;
                        currentVoiceChatAudioSource.panStereo = 0f;
                    }
                    else
                    {
                        player.currentVoiceChatIngameSettings.set2D = false;
                        currentVoiceChatAudioSource.volume = 0.8f;
                    }
                }

                return false;
            }

            return true;
        }
    }
}
