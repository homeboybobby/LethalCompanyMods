using GameNetcodeStuff;
using HarmonyLib;
using System.Reflection;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;
using System.Collections;

namespace PhantomMode.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerControllerBPatch
    {
        private static FieldInfo IsJumpingField = typeof(PlayerControllerB).GetField("isJumping", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo PlayerSlidingTimerField = typeof(PlayerControllerB).GetField("playerSlidingTimer", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo IsFallingFromJumpField = typeof(PlayerControllerB).GetField("isFallingFromJump", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo SprintMultiplierField = typeof(PlayerControllerB).GetField("sprintMultiplier", BindingFlags.Instance | BindingFlags.NonPublic);

        private static bool AllowKill = true;
        public static bool IsPhantomMode = false;
        private static Coroutine JumpCoroutine;
        private static Vector3 DeathLocation;
        private static int DeathExceptions;
        private static int MaxDeathExceptions = 3;
        private static float ExceptionCooldownTime = 2f;
        private static float LastExceptionTime;
        private static Vector3[] LastSafeLocations = (Vector3[])(object)new Vector3[10];
        private static int SafeLocationsIndex;
        private static float TimeWhenSafe = Time.time;
        private static Ray InteractRay;
        private static bool NightVisionFlag = false;
        private static LightType NightVisionType;
        private static float NightVisionIntensity;
        private static float NightVisionRange;
        private static float NightVisionShadowStrength;
        private static float NightVisionBounceIntensity;
        private static float NightVisionInnerSpotAngle;
        private static float NightVisionSpotAngle;
        private static bool SetupValuesYet;
        private static float MaxSpeed = 10f;
        private static int PlayerTPIndex;
        private static Coroutine TPCoroutine;
        private static bool IsTeleporting;
        private static bool ToggledCollision = false;
        private static Coroutine ToggleCollisionCoroutine;
        private static bool CollisionEnabled = true;
        public static float LastInteractedTime = Time.time;

        public static void ResetPhantomMode(PlayerControllerB __instance)
        {
            try
            {
                if (__instance != null)
                {
                    __instance.StopAllCoroutines();

                    if (__instance.nightVision != null)
                    {
                        __instance.nightVision.gameObject.SetActive(true);
                    }

                    if (IsJumpingField != null && PlayerSlidingTimerField != null && IsFallingFromJumpField != null && SprintMultiplierField != null)
                    {
                        PlayerSlidingTimerField.SetValue(__instance, 0f);
                        IsJumpingField.SetValue(__instance, false);
                        IsFallingFromJumpField.SetValue(__instance, false);
                        __instance.fallValue = 0f;
                        __instance.fallValueUncapped = 0f;
                        SprintMultiplierField.SetValue(__instance, 1f);
                    }
                    else
                    {
                        PhantomModePlugin.mls.LogError("Private fields could not be accessed!");
                    }

                    SetNightVisionMode(__instance, 0);
                    __instance.hasBegunSpectating = false;
                    StartOfRound.Instance.SwitchCamera(GameNetworkManager.Instance.localPlayerController.gameplayCamera);
                    HUDManager.Instance.HideHUD(false);
                    HUDManager.Instance.spectatingPlayerText.text = "";
                    HUDManager.Instance.RemoveSpectateUI();
                    ShowPlayerAliveUI(__instance, show: true);
                }

                PhantomModePlugin.mls.LogMessage("Reset phantom mode variables.");
                AllowKill = true;
                IsPhantomMode = false;
                NightVisionFlag = false;
                DeathExceptions = 0;
                LastSafeLocations = new Vector3[10];
                TimeWhenSafe = Time.time;
                JumpCoroutine = null;
                PlayerTPIndex = 0;
                TPCoroutine = null;
                IsTeleporting = false;
                ToggledCollision = false;
                CollisionEnabled = true;
            }
            catch (Exception ex)
            {
                PhantomModePlugin.mls.LogMessage(ex);
            }
        }

        private static void SetNightVisionMode(PlayerControllerB __instance, int mode)
        {
            if (mode < 0) mode = 0;
            if (mode > 1) mode = 1;

            if (mode == 0)
            {
                PhantomModePlugin.mls.LogMessage("Setting default night vision values");
                __instance.nightVision.type = NightVisionType;
                __instance.nightVision.intensity = NightVisionIntensity;
                __instance.nightVision.range = NightVisionRange;
                __instance.nightVision.shadowStrength = NightVisionShadowStrength;
                __instance.nightVision.bounceIntensity = NightVisionBounceIntensity;
                __instance.nightVision.innerSpotAngle = NightVisionInnerSpotAngle;
                __instance.nightVision.spotAngle = NightVisionSpotAngle;
            } else if (mode == 1)
            {
                __instance.nightVision.type = LightType.Point;
                __instance.nightVision.intensity = 44444f;
                __instance.nightVision.range = 99999f;
                __instance.nightVision.shadowStrength = 0f;
                __instance.nightVision.bounceIntensity = 5555f;
                __instance.nightVision.innerSpotAngle = 999f;
                __instance.nightVision.spotAngle = 9999f;
            }
        }

        private static void ShowPlayerAliveUI(PlayerControllerB __instance, bool show)
        {
            HUDManager.Instance.Clock.canvasGroup.gameObject.SetActive(show);
            HUDManager.Instance.selfRedCanvasGroup.gameObject.SetActive(show);
            __instance.sprintMeterUI.gameObject.SetActive(show);
            HUDManager.Instance.weightCounter.gameObject.SetActive(show);
            if (!show)
            {
                foreach (var controlTip in HUDManager.Instance.controlTipLines)
                {
                    controlTip.text = "";
                }
            }
        }

        private static Vector3 GetTPLocation(PlayerControllerB __instance)
        {
            Vector3 position = Vector3.zero;
            if (__instance.deadBody != null)
            {
                position = __instance.deadBody.transform.position;
                return position;
            }
            position = DeathLocation;
            return position;
        }

        private static void ChangeAudioListenerToObject(PlayerControllerB __instance, GameObject addToObject)
        {
            __instance.activeAudioListener.transform.SetParent(addToObject.transform);
            __instance.activeAudioListener.transform.localEulerAngles = Vector3.zero;
            __instance.activeAudioListener.transform.localPosition = Vector3.zero;
            StartOfRound.Instance.audioListener = __instance.activeAudioListener;
        }

        [HarmonyPatch("KillPlayer")]
        [HarmonyPrefix]
        private static bool KillPlayerPrefix(PlayerControllerB __instance)
        {
            float time = Time.time;
            if (__instance.IsOwner)
            {
                if (!AllowKill)
                {
                    if (time - LastExceptionTime > ExceptionCooldownTime)
                    {
                        DeathExceptions = 0;
                    }
                    DeathExceptions++;
                    LastExceptionTime = time;
                    if (DeathExceptions >= MaxDeathExceptions)
                    {
                        PhantomModePlugin.mls.LogMessage("Too many consecutive death exceptions. Stuck in death loop.");
                        Vector3 position = LastSafeLocations[(SafeLocationsIndex - 9 + LastSafeLocations.Length) % LastSafeLocations.Length];
                        __instance.transform.position = position;
                    }
                    PhantomModePlugin.mls.LogMessage("Player should be dead on server already. Why would it attempt to kill?");
                    return false;
                }
                AllowKill = false;
                DeathLocation = __instance.transform.position;
                DeathExceptions = 0;
                PhantomModePlugin.mls.LogMessage("Called kill player");
            }

            PhantomModePlugin.mls.LogMessage("Called kill player but not as local player.");
            return true;
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        private static void UpdatePostfix(PlayerControllerB __instance, ref Light ___nightVision)
        {
            if (!SetupValuesYet && AllowKill)
            {
                PhantomModePlugin.mls.LogMessage("Setting default night vision values");
                SetupValuesYet = true;
                NightVisionType = __instance.nightVision.type;
                NightVisionIntensity = __instance.nightVision.intensity;
                NightVisionRange = __instance.nightVision.range;
                NightVisionShadowStrength = __instance.nightVision.shadowStrength;
                NightVisionBounceIntensity = __instance.nightVision.bounceIntensity;
                NightVisionInnerSpotAngle = __instance.nightVision.innerSpotAngle;
                NightVisionSpotAngle = __instance.nightVision.spotAngle;
            }

            if (Time.time - TimeWhenSafe >= 1f)
            {
                LastSafeLocations[SafeLocationsIndex] = __instance.transform.position;
                SafeLocationsIndex = (SafeLocationsIndex + 1) % LastSafeLocations.Length;
                TimeWhenSafe = Time.time;
            }

            if (AllowKill)
            {
                return;
            }

            __instance.sprintMeter = 1f;

            if (__instance.isSprinting)
            {
                if (SprintMultiplierField != null)
                {
                    if (SprintMultiplierField.GetValue(__instance) is float value && value < MaxSpeed)
                    {
                        float multiplier = value * 1.015f;
                        SprintMultiplierField.SetValue(__instance, multiplier);
                    }
                    else
                    {
                        PhantomModePlugin.mls.LogError("Current Sprint Multiplier is apparently not a float.");
                    }
                }
                else
                {
                    PhantomModePlugin.mls.LogError("Could not access UpdateSpectateBoxesIntervalField");
                }
            }

            if (!IsPhantomMode)
            {
                if (Keyboard.current[PhantomModePlugin.GetButton(Variables.StartPhantomModeButton)].wasPressedThisFrame)
                {
                    PhantomModePlugin.mls.LogMessage("Attempting to revive");
                    ReviveDeadPlayer(__instance);
                }
            }
            else if (!StartOfRound.Instance.localPlayerController.inTerminalMenu && !StartOfRound.Instance.localPlayerController.isTypingChat)
            {
                if (!CollisionEnabled)
                {
                    HandleFreeRoamControls(__instance);
                }

                if (Keyboard.current[PhantomModePlugin.GetButton(Variables.ToggleFreeRoamButton)].wasPressedThisFrame && !ToggledCollision)
                {
                    ToggledCollision = true;
                    ToggleCollisionCoroutine = __instance.StartCoroutine(ToggleCollision(__instance));
                }

                if (Keyboard.current[PhantomModePlugin.GetButton(Variables.TeleportToDeadBodyButton)].wasPressedThisFrame)
                {
                    PhantomModePlugin.mls.LogMessage("Attempting to tp to dead body");
                    string message = "(Teleported to: Your dead body)";
                    TPCoroutine = __instance.StartCoroutine(TPToPlayer(__instance, __instance.deadBody.transform.position, message));
                }

                if (Keyboard.current[PhantomModePlugin.GetButton(Variables.TeleportToEntranceButton)].wasPressedThisFrame)
                {
                    PhantomModePlugin.mls.LogMessage("Attempting to tp to front door");
                    string message = "(Teleported to: Entrance)";
                    TPCoroutine = __instance.StartCoroutine(TPToPlayer(__instance, RoundManager.FindMainEntrancePosition(true, true), message));
                }

                if (Keyboard.current[PhantomModePlugin.GetButton(Variables.SwitchToSpectateModeButton)].wasPressedThisFrame)
                {
                    PhantomModePlugin.mls.LogMessage("Attempting to switch back to spectate mode");
                    SwitchToSpectatorMode(__instance);
                }

                if (Keyboard.current[Key.LeftArrow].wasPressedThisFrame)
                {
                    if (IsTeleporting)
                    {
                        return;
                    }

                    IsTeleporting = true;
                    PlayerControllerB[] allPlayerScripts = StartOfRound.Instance.allPlayerScripts;

                    for (int i = 0; i < allPlayerScripts.Length; i++)
                    {
                        PlayerTPIndex = (PlayerTPIndex - 1 + allPlayerScripts.Length) % allPlayerScripts.Length;
                        PhantomModePlugin.mls.LogMessage($"TP index: {PlayerTPIndex}");

                        if (!__instance.playersManager.allPlayerScripts[PlayerTPIndex].isPlayerDead && __instance.playersManager.allPlayerScripts[PlayerTPIndex].isPlayerControlled && __instance.playersManager.allPlayerScripts[PlayerTPIndex].playerClientId != StartOfRound.Instance.localPlayerController.playerClientId)
                        {
                            string text = $"(Teleported to: {__instance.playersManager.allPlayerScripts[PlayerTPIndex].playerUsername})";
                            PhantomModePlugin.mls.LogMessage($"TP index: {PlayerTPIndex} Player Name: {text}");
                            TPCoroutine = __instance.StartCoroutine(TPToPlayer(__instance, __instance.playersManager.allPlayerScripts[PlayerTPIndex].transform.position, text));
                            return;
                        }
                    }
                }

                if (Keyboard.current[Key.RightArrow].wasPressedThisFrame)
                {
                    if (IsTeleporting)
                    {
                        return;
                    }

                    IsTeleporting = true;
                    PlayerControllerB[] allPlayerScripts = StartOfRound.Instance.allPlayerScripts;

                    for (int j = 0; j < allPlayerScripts.Length; j++)
                    {
                        PlayerTPIndex = (PlayerTPIndex + 1) % allPlayerScripts.Length;
                        PhantomModePlugin.mls.LogMessage($"TP index: {PlayerTPIndex}");

                        if (!__instance.playersManager.allPlayerScripts[PlayerTPIndex].isPlayerDead && __instance.playersManager.allPlayerScripts[PlayerTPIndex].isPlayerControlled && __instance.playersManager.allPlayerScripts[PlayerTPIndex].playerClientId != StartOfRound.Instance.localPlayerController.playerClientId)
                        {
                            string text = $"(Teleported to: {__instance.playersManager.allPlayerScripts[PlayerTPIndex].playerUsername})";
                            PhantomModePlugin.mls.LogMessage($"TP index: {PlayerTPIndex} Player Name: {text}");
                            TPCoroutine = __instance.StartCoroutine(TPToPlayer(__instance, __instance.playersManager.allPlayerScripts[PlayerTPIndex].transform.position, text));
                            return;
                        }
                    }
                }
            }

            if (__instance.playersManager.livingPlayers == 0 || StartOfRound.Instance.shipIsLeaving)
            {
                HUDManager.Instance.DisplayTip("Phantom Mode", "Ship is leaving. Please wait!");

                if (IsPhantomMode)
                {
                    RekillPlayerLocally(__instance, gameOver: true);
                }

                ResetPhantomMode(__instance);
            }

            if (__instance.criticallyInjured)
            {
                __instance.criticallyInjured = false;
                __instance.bleedingHeavily = false;
                HUDManager.Instance.UpdateHealthUI(100, false);
            }

            if (Keyboard.current[PhantomModePlugin.GetButton(Variables.ToggleBrightModeButton)].wasPressedThisFrame && !__instance.inTerminalMenu)
            {
                PhantomModePlugin.mls.LogMessage("Trying to toggle night vision.");

                if (___nightVision.gameObject.activeSelf)
                {
                    SetNightVisionMode(__instance, 0);
                    __instance.isInsideFactory = false;
                    NightVisionFlag = false;
                }

                if (!___nightVision.gameObject.activeSelf)
                {
                    SetNightVisionMode(__instance, 1);
                    __instance.isInsideFactory = true;
                    NightVisionFlag = true;
                }
            }

            ___nightVision.gameObject.SetActive(NightVisionFlag);
        }


        [HarmonyPatch("Interact_performed")]
        [HarmonyPrefix]
        private static bool InteractPerformedPrefix(PlayerControllerB __instance)
        {
            if (!IsPhantomMode)
            {
                return true;
            }

            if (__instance.IsOwner && !__instance.isPlayerDead && (!__instance.IsServer || __instance.isHostPlayerObject))
            {
                if (!CanUse(__instance))
                {
                    return false;
                }
                if (ShouldHaveDelay(__instance))
                {
                    if (!(Time.time - LastInteractedTime > Variables.WaitTimeBetweenInteractions))
                    {
                        return false;
                    }

                    LastInteractedTime = Time.time;
                }
            }

            return true;
        }

        private static string GetHoveringObjectName(PlayerControllerB __instance)
        {
            string text = __instance.hoveringOverTrigger.gameObject.name;
            int index = text.IndexOf("(");
            if (index != -1)
            {
                text = text.Substring(0, index).Trim();
            }

            return text;
        }

        private static bool ShouldHaveDelay(PlayerControllerB __instance, bool showDebug = true)
        {
            if (!IsPhantomMode)
            {
                return false;
            }

            if (__instance.hoveringOverTrigger != null && __instance.hoveringOverTrigger.gameObject != null)
            {
                string hoveringObjectName = GetHoveringObjectName(__instance);

                if (showDebug)
                {
                    PhantomModePlugin.mls.LogMessage($"Tried to interact with: {hoveringObjectName}");
                }

                string[] source = new string[6] { "Cube", "EntranceTeleportA", "StartGameLever", "TerminalScript", "ButtonGlass", "Trigger" };
                
                if (source.Contains(hoveringObjectName))
                {
                    return false;
                }
            }
            else if (showDebug)
            {
                PhantomModePlugin.mls.LogMessage("Failed to find interact.");
            }

            return true;
        }

        private static bool CanUse(PlayerControllerB __instance)
        {
            if (!IsPhantomMode)
            {
                return true;
            }

            if (__instance.hoveringOverTrigger != null && __instance.hoveringOverTrigger.gameObject != null)
            {
                string hoveringObjectName = GetHoveringObjectName(__instance);
                string[] source = new string[] { "RedButton", "LadderTrigger" };
                if (source.Contains(hoveringObjectName))
                {
                    return false;
                }
            }

            return true;
        }

        [HarmonyPatch("SetHoverTipAndCurrentInteractTrigger")]
        [HarmonyPostfix]
        private static void SetHoverTipAndCurrentInteractTriggerPostfix(PlayerControllerB __instance)
        {
            if (!IsPhantomMode)
            {
                return;
            }

            if (ShouldHaveDelay(__instance, showDebug: false))
            {
                float InteractionTime = LastInteractedTime;
                float waitTimeBetweenInteractions = Variables.WaitTimeBetweenInteractions;
                float TimeRemaining = waitTimeBetweenInteractions - (Time.time - InteractionTime);
                if (Time.time - InteractionTime <= waitTimeBetweenInteractions && __instance.cursorTip.text != "")
                {
                    __instance.cursorTip.text = $"Wait: {(int)TimeRemaining}";
                }
            }

            if (!CanUse(__instance))
            {
                __instance.cursorTip.text = "Can't use as a phantom!";
            }
        }

        private static bool IsPlayerCloseToGround(PlayerControllerB __instance)
        {
            InteractRay = new Ray(__instance.transform.position, Vector3.down);
            return Physics.Raycast(InteractRay, 0.15f, StartOfRound.Instance.allPlayersCollideWithMask, QueryTriggerInteraction.Ignore);
        }

        private static void DoHitGroundEffects(PlayerControllerB __instance)
        {
            __instance.GetCurrentMaterialStandingOn();

            if (__instance.fallValue < -9f)
            {
                if (__instance.fallValue < -16f)
                {
                    __instance.movementAudio.PlayOneShot(StartOfRound.Instance.playerHitGroundHard, 1f);
                    WalkieTalkie.TransmitOneShotAudio(__instance.movementAudio, StartOfRound.Instance.playerHitGroundHard, 1f);
                }
                else if (__instance.fallValue < -2f)
                {
                    __instance.movementAudio.PlayOneShot(StartOfRound.Instance.playerHitGroundSoft, 1f);
                }

                __instance.LandFromJumpServerRpc(__instance.fallValue < -16f);
            }

            if (__instance.takingFallDamage && !__instance.jetpackControls && !__instance.disablingJetpackControls && !__instance.isSpeedCheating && AllowKill)
            {
                Debug.Log($"Fall damage: {__instance.fallValueUncapped}");
                
                if (__instance.fallValueUncapped < -48.5f)
                {
                    __instance.DamagePlayer(100, true, true, CauseOfDeath.Gravity, 0, false, default(Vector3));
                }

                else if (__instance.fallValueUncapped < -45f)
                {
                    __instance.DamagePlayer(80, true, true, CauseOfDeath.Gravity, 0, false, default(Vector3));
                }
                else if (__instance.fallValueUncapped < -40f)
                {
                    __instance.DamagePlayer(50, true, true, CauseOfDeath.Gravity, 0, false, default(Vector3));
                }
                else
                {
                    __instance.DamagePlayer(30, true, true, CauseOfDeath.Gravity, 0, false, default(Vector3));
                }
            }

            if (__instance.fallValue < -16f)
            {
                RoundManager.Instance.PlayAudibleNoise(__instance.transform.position, 7f, 0.5f, 0, false, 0);
            }
        }

        [HarmonyPatch("Jump_performed")]
        [HarmonyPrefix]
        private static void JumpPerformedPrefix(PlayerControllerB __instance)
        {
            if (IsJumpingField != null && PlayerSlidingTimerField != null && IsFallingFromJumpField != null)
            {
                if (!__instance.quickMenuManager.isMenuOpen && ((__instance.IsOwner && __instance.isPlayerControlled && (!__instance.IsServer || __instance.isHostPlayerObject)) || __instance.isTestingPlayer) && !__instance.inSpecialInteractAnimation && !__instance.isTypingChat && (__instance.isMovementHindered <= 0 || __instance.isUnderwater) && !__instance.isExhausted && (!__instance.isPlayerSliding || (float)PlayerSlidingTimerField.GetValue(__instance) > 2.5f) && !__instance.isCrouching && (!AllowKill || ((__instance.thisController.isGrounded || (!(bool)IsJumpingField.GetValue(__instance) && IsPlayerCloseToGround(__instance))) && !(bool)IsJumpingField.GetValue(__instance))))
                {
                    PlayerSlidingTimerField.SetValue(__instance, 0f);
                    IsJumpingField.SetValue(__instance, true);
                    __instance.sprintMeter = Mathf.Clamp(__instance.sprintMeter - 0.08f, 0f, 1f);
                    if (JumpCoroutine != null)
                    {
                        __instance.StopCoroutine(JumpCoroutine);
                    }
                    JumpCoroutine = __instance.StartCoroutine(Jump(__instance, IsJumpingField, IsFallingFromJumpField));
                }
            }
            else
            {
                PhantomModePlugin.mls.LogError("Could not access UpdateSpectateBoxesIntervalField");
            }
        }

        private static IEnumerator Jump(PlayerControllerB __instance, FieldInfo isJumpingField, FieldInfo isFallingFromJumpField)
        {
            __instance.jumpForce = AllowKill ? 13f : 25f;
            __instance.playerBodyAnimator.SetBool("Jumping", true);
            yield return new WaitForSeconds(0.15f);
            __instance.fallValue = __instance.jumpForce;
            __instance.fallValueUncapped = __instance.jumpForce;
            yield return new WaitForSeconds(0.1f);
            isJumpingField.SetValue(__instance, false);
            isFallingFromJumpField.SetValue(__instance, true);

            if (!AllowKill)
            {
                yield return new WaitForSeconds(0.1f);
            }
            else
            {
                yield return new WaitUntil(() => __instance.thisController.isGrounded);
            }

            __instance.playerBodyAnimator.SetBool("Jumping", false);
            isFallingFromJumpField.SetValue(__instance, false);
            DoHitGroundEffects(__instance);
            JumpCoroutine = null;
        }

        [HarmonyPatch("DamagePlayer")]
        [HarmonyPrefix]
        private static void DamagePlayerPrefix(PlayerControllerB __instance, ref int damageNumber)
        {
            if (!AllowKill)
            {
                __instance.health = 100;
                __instance.criticallyInjured = false;
                __instance.bleedingHeavily = false;
                int damage = 100 - damageNumber;
                __instance.DamagePlayerServerRpc(-damage, __instance.health);
            }
        }

        [HarmonyPatch("DamagePlayer")]
        [HarmonyPostfix]
        private static void DamagePlayerPostfix(PlayerControllerB __instance, ref int damageNumber)
        {
            if (!AllowKill)
            {
                HUDManager.Instance.UpdateHealthUI(100, false);
            }
        }

        private static void HandleFreeRoamControls(PlayerControllerB __instance)
        {
            if (Keyboard.current[Key.W].isPressed)
            {
                Quaternion rotation = __instance.transform.rotation;
                Vector3 movement = rotation * Vector3.forward;
                movement.Normalize();
                __instance.transform.position += movement * Variables.FreeRoamSpeed;
            }

            if (Keyboard.current[Key.A].isPressed)
            {
                Quaternion rotation = __instance.transform.rotation;
                Quaternion movement = Quaternion.AngleAxis(-90f, Vector3.up);
                Vector3 direction = movement * rotation * Vector3.forward;
                direction.Normalize();
                __instance.transform.position += direction * Variables.FreeRoamSpeed;
            }

            if (Keyboard.current[Key.D].isPressed)
            {
                Quaternion rotation = __instance.transform.rotation;
                Quaternion movement = Quaternion.AngleAxis(90f, Vector3.up);
                Vector3 direction = movement * rotation * Vector3.forward;
                direction.Normalize();
                __instance.transform.position += direction * Variables.FreeRoamSpeed;
            }

            if (Keyboard.current[Key.S].isPressed)
            {
                Quaternion rotation = __instance.transform.rotation;
                Vector3 movement = rotation * Vector3.back;
                movement.Normalize();
                __instance.transform.position += movement * Variables.FreeRoamSpeed;
            }

            if (Keyboard.current[Key.Space].isPressed)
            {
                Vector3 up = Vector3.up;
                __instance.transform.position += up * Variables.FreeRoamSpeed;
            }

            if (Keyboard.current[Key.LeftShift].isPressed)
            {
                Vector3 down = -Vector3.up;
                __instance.transform.position += down * Variables.FreeRoamSpeed;
            }
        }


        private static void ReviveDeadPlayer(PlayerControllerB __instance)
        {
            try
            {
                PlayerControllerB[] allPlayerScripts = StartOfRound.Instance.allPlayerScripts;
                ulong playerClientId = StartOfRound.Instance.localPlayerController.playerClientId;
                Vector3 teleportLocation = GetTPLocation(allPlayerScripts[playerClientId]);
                PhantomModePlugin.mls.LogMessage($"Reviving player {playerClientId}");
                allPlayerScripts[playerClientId].velocityLastFrame = Vector3.zero;
                allPlayerScripts[playerClientId].isSprinting = false;
                allPlayerScripts[playerClientId].ResetPlayerBloodObjects(allPlayerScripts[playerClientId].isPlayerDead);
                allPlayerScripts[playerClientId].isClimbingLadder = false;
                allPlayerScripts[playerClientId].ResetZAndXRotation();
                allPlayerScripts[playerClientId].thisController.enabled = true;
                allPlayerScripts[playerClientId].health = 100;
                allPlayerScripts[playerClientId].disableLookInput = false;

                if (allPlayerScripts[playerClientId].isPlayerDead)
                {
                    allPlayerScripts[playerClientId].isPlayerDead = false;
                    allPlayerScripts[playerClientId].isPlayerControlled = true;
                    allPlayerScripts[playerClientId].isInElevator = true;
                    allPlayerScripts[playerClientId].isInHangarShipRoom = true;
                    allPlayerScripts[playerClientId].isInsideFactory = false;
                    allPlayerScripts[playerClientId].wasInElevatorLastFrame = false;
                    StartOfRound.Instance.SetPlayerObjectExtrapolate(false);
                    allPlayerScripts[playerClientId].transform.position = teleportLocation;
                    allPlayerScripts[playerClientId].setPositionOfDeadPlayer = false;
                    allPlayerScripts[playerClientId].DisablePlayerModel(StartOfRound.Instance.allPlayerObjects[playerClientId], true, true);
                    allPlayerScripts[playerClientId].helmetLight.enabled = false;
                    allPlayerScripts[playerClientId].Crouch(false);
                    allPlayerScripts[playerClientId].criticallyInjured = false;

                    if (allPlayerScripts[playerClientId].playerBodyAnimator != null)
                    {
                        allPlayerScripts[playerClientId].playerBodyAnimator.SetBool("Limp", false);
                    }

                    allPlayerScripts[playerClientId].bleedingHeavily = false;
                    allPlayerScripts[playerClientId].activatingItem = false;
                    allPlayerScripts[playerClientId].twoHanded = false;
                    allPlayerScripts[playerClientId].inSpecialInteractAnimation = false;
                    allPlayerScripts[playerClientId].disableSyncInAnimation = false;
                    allPlayerScripts[playerClientId].inAnimationWithEnemy = null;
                    allPlayerScripts[playerClientId].holdingWalkieTalkie = false;
                    allPlayerScripts[playerClientId].speakingToWalkieTalkie = false;
                    allPlayerScripts[playerClientId].isSinking = false;
                    allPlayerScripts[playerClientId].isUnderwater = false;
                    allPlayerScripts[playerClientId].sinkingValue = 0f;
                    allPlayerScripts[playerClientId].statusEffectAudio.Stop();
                    allPlayerScripts[playerClientId].DisableJetpackControlsLocally();
                    allPlayerScripts[playerClientId].health = 100;

                    if (allPlayerScripts[playerClientId].IsOwner)
                    {
                        HUDManager.Instance.gasHelmetAnimator.SetBool("gasEmitting", false);
                        allPlayerScripts[playerClientId].hasBegunSpectating = false;
                        HUDManager.Instance.RemoveSpectateUI();
                        HUDManager.Instance.gameOverAnimator.SetTrigger("revive");
                        allPlayerScripts[playerClientId].hinderedMultiplier = 1f;
                        allPlayerScripts[playerClientId].isMovementHindered = 0;
                        allPlayerScripts[playerClientId].sourcesCausingSinking = 0;
                        allPlayerScripts[playerClientId].reverbPreset = StartOfRound.Instance.shipReverb;
                    }
                }

                SoundManager.Instance.earsRingingTimer = 0f;
                allPlayerScripts[playerClientId].voiceMuffledByEnemy = false;
                SoundManager.Instance.playerVoicePitchTargets[playerClientId] = 1f;
                SoundManager.Instance.SetPlayerPitch(1f, (int)playerClientId);
                PlayerControllerB localPlayerController = GameNetworkManager.Instance.localPlayerController;
                localPlayerController.bleedingHeavily = false;
                localPlayerController.criticallyInjured = false;
                localPlayerController.playerBodyAnimator.SetBool("Limp", false);
                localPlayerController.health = 100;
                HUDManager.Instance.UpdateHealthUI(100, false);
                localPlayerController.spectatedPlayerScript = null;
                HUDManager.Instance.audioListenerLowPass.enabled = false;
                StartOfRound.Instance.SetSpectateCameraToGameOverMode(false, localPlayerController);
                IsPhantomMode = true;
                HUDManager.Instance.spectatingPlayerText.text = "";
                HUDManager.Instance.holdButtonToEndGameEarlyText.text = "";
                HUDManager.Instance.holdButtonToEndGameEarlyMeter.gameObject.SetActive(false);
                HUDManager.Instance.holdButtonToEndGameEarlyVotesText.text = "";
                ShowPlayerAliveUI(localPlayerController, false);

                if (SprintMultiplierField != null)
                {
                    SprintMultiplierField.SetValue(localPlayerController, 1f);
                }

                HUDManagerPatch.UpdateBoxesSpectateUI(HUDManager.Instance);
            }
            catch (Exception ex)
            {
                PhantomModePlugin.mls.LogError(ex);
            }
        }

        public static void RekillPlayerLocally(PlayerControllerB __instance, bool gameOver)
        {
            PhantomModePlugin.mls.LogMessage("Trying to rekill player locally");
            __instance.DropAllHeldItemsServerRpc();
            __instance.DisableJetpackControlsLocally();
            __instance.isPlayerDead = true;
            __instance.isPlayerControlled = false;
            __instance.thisPlayerModelArms.enabled = false;
            __instance.localVisor.position = __instance.playersManager.notSpawnedPosition.position;
            __instance.DisablePlayerModel(__instance.gameObject, false, false);
            __instance.isInsideFactory = false;
            __instance.IsInspectingItem = false;
            __instance.inTerminalMenu = false;
            __instance.twoHanded = false;
            __instance.carryWeight = 1f;
            __instance.fallValue = 0f;
            __instance.fallValueUncapped = 0f;
            __instance.takingFallDamage = false;
            __instance.isSinking = false;
            __instance.isUnderwater = false;
            StartOfRound.Instance.drowningTimer = 1f;
            HUDManager.Instance.setUnderwaterFilter = false;
            __instance.sourcesCausingSinking = 0;
            __instance.sinkingValue = 0f;
            __instance.hinderedMultiplier = 1f;
            __instance.isMovementHindered = 0;
            __instance.inAnimationWithEnemy = null;
            HUDManager.Instance.SetNearDepthOfFieldEnabled(true);
            HUDManager.Instance.HUDAnimator.SetBool("biohazardDamage", false);
            StartOfRound.Instance.SwitchCamera(StartOfRound.Instance.spectateCamera);

            if (gameOver)
            {
                HUDManager.Instance.DisplayTip("Phantom Mode", "Please wait. Ship is leaving!");
            }
        }


        private static void SwitchToSpectatorMode(PlayerControllerB __instance)
        {
            ShowPlayerAliveUI(__instance, false);
            RekillPlayerLocally(__instance, false);
            __instance.hasBegunSpectating = true;
            HUDManager.Instance.gameOverAnimator.SetTrigger("gameOver");
            IsPhantomMode = false;
            ChangeAudioListenerToObject(__instance, __instance.playersManager.spectateCamera.gameObject);
        }

        private static IEnumerator TPToPlayer(PlayerControllerB __instance, Vector3 newPos, string message)
        {
            if (TPCoroutine != null)
            {
                __instance.StopCoroutine(TPCoroutine);
            }

            HUDManager.Instance.spectatingPlayerText.text = message;
            GameNetworkManager.Instance.localPlayerController.TeleportPlayer(newPos, false, 0f, false, true);

            yield return new WaitForSeconds(1.5f);

            HUDManager.Instance.spectatingPlayerText.text = "";
            IsTeleporting = false;
        }

        private static IEnumerator ToggleCollision(PlayerControllerB __instance)
        {
            if (ToggleCollisionCoroutine != null)
            {
                __instance.StopCoroutine(ToggleCollisionCoroutine);
            }

            __instance = StartOfRound.Instance.localPlayerController;

            yield return new WaitForSeconds(0.1f);

            if (CollisionEnabled)
            {
                PhantomModePlugin.mls.LogMessage("Collisions are disabled");
                HUDManager.Instance.DisplayTip("Free Roam", "Activated");
                CollisionEnabled = false;
                __instance.playerCollider.enabled = false;
            }
            else if (!CollisionEnabled)
            {
                PhantomModePlugin.mls.LogMessage("Collisions are enabled");
                HUDManager.Instance.DisplayTip("Free Roam", "Deactivated");
                CollisionEnabled = true;
                __instance.playerCollider.enabled = true;
            }

            ToggledCollision = false;
        }
    }
}