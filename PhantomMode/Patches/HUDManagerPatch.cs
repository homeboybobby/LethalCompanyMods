using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine.UI;
using UnityEngine;
using System.Linq;

namespace PhantomMode.Patches
{
    [HarmonyPatch(typeof(HUDManager))]
    internal class HUDManagerPatch
    {
        // Field Info
        private static FieldInfo UpdateSpectateBoxesIntervalField = typeof(HUDManager).GetField("updateSpectateBoxesInterval", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo YOffsetAmountField = typeof(HUDManager).GetField("yOffsetAmount", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo BoxesAddedField = typeof(HUDManager).GetField("boxesAdded", BindingFlags.Instance | BindingFlags.NonPublic);

        // Method Info
        private static MethodInfo UpdateSpectateBoxSpeakerIconsMethod = typeof(HUDManager).GetMethod("UpdateSpectateBoxSpeakerIcons", BindingFlags.Instance | BindingFlags.NonPublic);

        public static int LivingPlayersCount;

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        private static void UpdatePrefix(HUDManager __instance)
        {
            if (PlayerControllerBPatch.IsPhantomMode && GameNetworkManager.Instance != null && GameNetworkManager.Instance.localPlayerController != null)
            {
                if (LivingPlayersCount != RoundManager.Instance.playersManager.livingPlayers)
                {
                    PhantomModePlugin.mls.LogMessage("Adding boxes");
                    __instance.gameOverAnimator.SetTrigger("gameOver");
                    __instance.spectatingPlayerText.text = "";
                    __instance.holdButtonToEndGameEarlyText.text = "";
                    __instance.holdButtonToEndGameEarlyMeter.gameObject.SetActive(false);
                    __instance.holdButtonToEndGameEarlyVotesText.text = "";
                    UpdateBoxesSpectateUI(__instance);
                }

                float interval = (float)UpdateSpectateBoxesIntervalField.GetValue(__instance);
                
                if (interval >= 0.35f)
                {
                    UpdateSpectateBoxesIntervalField.SetValue(__instance, 0f);
                    UpdateSpectateBoxSpeakerIconsMethod.Invoke(__instance, null);
                }
                else
                {
                    interval += Time.deltaTime;
                    UpdateSpectateBoxesIntervalField.SetValue(__instance, interval);
                }

                LivingPlayersCount = RoundManager.Instance.playersManager.livingPlayers;
            }
        }

        public static void UpdateBoxesSpectateUI(HUDManager __instance)
        {
            PlayerControllerB playerScript;
            for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
            {
                playerScript = StartOfRound.Instance.allPlayerScripts[i];

                if (!playerScript.isPlayerDead)
                {
                    continue;
                }

                Dictionary<Animator, PlayerControllerB> dictionary = (Dictionary<Animator, PlayerControllerB>)UpdateSpectateBoxesIntervalField.GetValue(__instance);
                float yOffset = (float)YOffsetAmountField.GetValue(__instance);
                int BoxesAdded = (int)BoxesAddedField.GetValue(__instance);
                if (dictionary.Values.Contains(playerScript))
                {
                    GameObject gameObject = dictionary.FirstOrDefault((KeyValuePair<Animator, PlayerControllerB> x) => x.Value == playerScript).Key?.gameObject;
                    if (gameObject != null && !gameObject.activeSelf)
                    {
                        RectTransform trans = gameObject.GetComponent<RectTransform>();
                        trans.anchoredPosition = new Vector2(trans.anchoredPosition.x, yOffset);
                        BoxesAddedField.SetValue(__instance, BoxesAdded++);
                        gameObject.SetActive(true);
                        YOffsetAmountField.SetValue(__instance, yOffset - 70f);
                    }
                }
                else
                {
                    GameObject gameObject = Object.Instantiate(__instance.spectatingPlayerBoxPrefab, __instance.SpectateBoxesContainer, false);
                    gameObject.SetActive(true);
                    RectTransform trans = gameObject.GetComponent<RectTransform>();
                    trans.anchoredPosition = new Vector2(trans.anchoredPosition.x, yOffset);
                    YOffsetAmountField.SetValue(__instance, yOffset - 70f);
                    BoxesAddedField.SetValue(__instance, BoxesAdded++);
                    dictionary.Add(gameObject.GetComponent<Animator>(), playerScript);
                    UpdateSpectateBoxesIntervalField.SetValue(__instance, dictionary);
                    gameObject.GetComponentInChildren<TextMeshProUGUI>().text = playerScript.playerUsername;
                    if (!GameNetworkManager.Instance.disableSteam)
                    {
                        HUDManager.FillImageWithSteamProfile(gameObject.GetComponent<RawImage>(), playerScript.playerSteamId, true);
                    }
                }

                PhantomModePlugin.mls.LogMessage($"Boxes count:{dictionary.Count}");
            }
        }
    }

}