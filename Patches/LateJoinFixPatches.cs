using HarmonyLib;
using UnityEngine;

namespace MultiplayerExpansion.Patches;

[HarmonyPatch]
public static class LateJoinFixPatches
{
    private static float _loadingStuckTimer;

    /// <summary>
    /// Prevent NullRef when LevelGenerator.Instance.Level is null during late join.
    /// Late joiners may not have Level set when LevelAnimationStart is called,
    /// causing a NullRef at LevelGenerator.Instance.Level access.
    /// </summary>
    [HarmonyPatch(typeof(LoadingUI), nameof(LoadingUI.LevelAnimationStart))]
    [HarmonyPrefix]
    private static bool LevelAnimationStart_Prefix(LoadingUI __instance)
    {
        if (LevelGenerator.Instance == null || LevelGenerator.Instance.Level == null)
        {
            __instance.levelAnimationStarted = true;
            __instance.levelAnimationCompleted = true;
            Plugin.Logger.LogWarning("LateJoinFix: skipped loading animation (Level is null).");
            return false;
        }
        return true;
    }

    /// <summary>
    /// Detect stuck loading screen caused by dropped LoadingLevelAnimationCompletedRPC.
    /// When a player late-joins, other players' RPC may arrive before their PhotonView
    /// exists on the new client, causing the RPC to be silently dropped and
    /// levelAnimationCompleted to stay false forever.
    /// </summary>
    [HarmonyPatch(typeof(LoadingUI), "LateUpdate")]
    [HarmonyPostfix]
    private static void LateUpdate_Postfix(LoadingUI __instance)
    {
        if (__instance.levelAnimationCompleted)
        {
            _loadingStuckTimer = 0f;
            return;
        }

        if (GameDirector.instance == null || GameDirector.instance.currentState != GameDirector.gameState.Start)
        {
            _loadingStuckTimer = 0f;
            return;
        }

        if (LevelGenerator.Instance == null || !LevelGenerator.Instance.Generated)
        {
            _loadingStuckTimer = 0f;
            return;
        }

        // Immediate fix: local player's animation completed but some remote players'
        // RPC was dropped, blocking LoadingUI.levelAnimationCompleted
        var localAvatar = PlayerController.instance?.playerAvatarScript;
        if (localAvatar != null && localAvatar.levelAnimationCompleted)
        {
            ForceAllCompleted(__instance);
            return;
        }

        // Fallback: timer-based stuck detection for cases where even the local
        // player's animation couldn't complete (e.g. LevelAnimationStart NullRef)
        _loadingStuckTimer += Time.deltaTime;
        if (_loadingStuckTimer > 10f)
        {
            ForceAllCompleted(__instance);
        }
    }

    private static void ForceAllCompleted(LoadingUI instance)
    {
        foreach (var player in GameDirector.instance.PlayerList)
        {
            if (!player.levelAnimationCompleted)
            {
                player.levelAnimationCompleted = true;
                Plugin.Logger.LogWarning($"LateJoinFix: forced levelAnimationCompleted for '{player.playerName}'.");
            }
        }
        instance.levelAnimationCompleted = true;
        _loadingStuckTimer = 0f;
    }
}
