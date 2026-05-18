using System;
using System.Collections.Generic;
using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace MultiplayerExpansion.Patches;

[HarmonyPatch]
public static class SpawnPatches
{
    private static readonly HashSet<int> _lateJoiners = new();
    private static readonly Dictionary<string, bool> _deadPlayers = new();

    [HarmonyPatch(typeof(NetworkManager), nameof(NetworkManager.OnPlayerEnteredRoom))]
    [HarmonyPostfix]
    private static void OnPlayerEnteredRoom_Postfix(Player newPlayer)
    {
        try
        {
            if (!Plugin.LateJoinEnabled.Value) return;
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
            if (!SemiFunc.RunIsLevel() && !SemiFunc.RunIsShop() && !SemiFunc.RunIsArena()) return;

            _lateJoiners.Add(newPlayer.ActorNumber);
            Plugin.Logger.LogInfo($"LateJoin: player '{newPlayer.NickName}' (#{newPlayer.ActorNumber}) joined late.");
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError($"LateJoin OnPlayerEnteredRoom error: {e}");
        }
    }

    [HarmonyPatch(typeof(PlayerAvatar), "Awake")]
    [HarmonyPostfix]
    private static void PlayerAvatar_Awake_Postfix(PlayerAvatar __instance)
    {
        try
        {
            if (!Plugin.LateJoinEnabled.Value) return;
            if (EnemyDirector.instance == null) return;
            if (EnemyDirector.instance.enemiesSpawned.Count == 0) return;
            if (!GameDirector.instance.PlayerList.Contains(__instance)) return;

            RegisterWithEnemies(__instance);
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError($"LateJoin PlayerAvatar_Awake error: {e}");
        }
    }

    [HarmonyPatch(typeof(PlayerAvatar), "Start")]
    [HarmonyPostfix]
    private static void PlayerAvatar_Start_Postfix(PlayerAvatar __instance)
    {
        try
        {
            if (!Plugin.LateJoinEnabled.Value) return;
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
            if (!LevelGenerator.Instance.Generated) return;

            __instance.photonView.RPC("LoadingLevelAnimationCompletedRPC", RpcTarget.AllBuffered);
            Plugin.Logger.LogInfo($"LateJoin: sent LoadingLevelAnimationCompletedRPC for '{__instance.photonView.Owner?.NickName}'.");
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError($"LateJoin PlayerAvatar_Start error: {e}");
        }
    }

    [HarmonyPatch(typeof(EnemyVision), nameof(EnemyVision.VisionTrigger))]
    [HarmonyPrefix]
    private static void EnemyVision_VisionTrigger_Prefix(EnemyVision __instance, int playerID)
    {
        if (!__instance.VisionTriggered.ContainsKey(playerID))
            __instance.PlayerAdded(playerID);
    }

    [HarmonyPatch(typeof(PlayerAvatar), nameof(PlayerAvatar.Spawn))]
    [HarmonyPrefix]
    private static bool PlayerAvatar_Spawn_Prefix(PlayerAvatar __instance)
    {
        try
        {
            if (__instance.spawned)
            {
                Plugin.Logger.LogDebug($"LateJoin: blocked re-spawn of already-spawned player '{__instance.playerName}'.");
                return false;
            }
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError($"LateJoin PlayerAvatar_Spawn error: {e}");
        }
        return true;
    }

    [HarmonyPatch(typeof(SemiFunc), nameof(SemiFunc.OnSceneSwitch))]
    [HarmonyPrefix]
    private static void OnSceneSwitch_Prefix()
    {
        _lateJoiners.Clear();
        _deadPlayers.Clear();
    }

    [HarmonyPatch(typeof(PlayerAvatar), nameof(PlayerAvatar.PlayerDeath))]
    [HarmonyPostfix]
    private static void PlayerDeath_Postfix(PlayerAvatar __instance)
    {
        try
        {
            string? steamId = SemiFunc.PlayerGetSteamID(__instance);
            if (steamId != null)
                _deadPlayers[steamId] = true;
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError($"LateJoin PlayerDeath error: {e}");
        }
    }

    [HarmonyPatch(typeof(PlayerAvatar), "LoadingLevelAnimationCompletedRPC")]
    [HarmonyPostfix]
    private static void LoadingLevelAnimationCompleted_Postfix(PlayerAvatar __instance)
    {
        try
        {
            if (!Plugin.LateJoinEnabled.Value) return;
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;

            var pv = __instance.photonView;
            if (pv == null || pv.Owner == null) return;

            int actorNr = pv.Owner.ActorNumber;
            if (!_lateJoiners.Contains(actorNr)) return;

            _lateJoiners.Remove(actorNr);
            Plugin.Logger.LogInfo($"LateJoin: syncing state for '{pv.Owner.NickName}'.");

            SyncStateForLateJoiner(__instance);
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError($"LateJoin LoadingLevelAnimation error: {e}");
        }
    }

    private static void SyncStateForLateJoiner(PlayerAvatar avatar)
    {
        SyncExtractionPoints(avatar);
        SyncValuables(avatar);

        if (Plugin.KillIfPreviouslyDead.Value)
        {
            string? steamId = SemiFunc.PlayerGetSteamID(avatar);
            if (steamId != null && _deadPlayers.ContainsKey(steamId))
            {
                avatar.PlayerDeath(-1);
                Plugin.Logger.LogInfo($"LateJoin: killed '{avatar.playerName}' (previously dead).");
            }
        }
    }

    private static void RegisterWithEnemies(PlayerAvatar avatar)
    {
        if (EnemyDirector.instance == null) return;

        int viewID = avatar.photonView.ViewID;
        int count = 0;

        foreach (var ep in EnemyDirector.instance.enemiesSpawned)
        {
            if (ep?.Enemy == null) continue;
            ep.Enemy.PlayerAdded(viewID);
            count++;
        }

        Plugin.Logger.LogInfo($"LateJoin: registered player viewID={viewID} with {count} enemies.");
    }

    private static void SyncExtractionPoints(PlayerAvatar avatar)
    {
        var target = avatar.photonView.Owner;
        var eps = UnityEngine.Object.FindObjectsOfType<ExtractionPoint>();

        foreach (var ep in eps)
        {
            var pv = ep.GetComponent<PhotonView>();
            if (pv == null) continue;

            pv.RPC("StateSetRPC", target, (int)ep.currentState);

            if (ep.haulGoal > 0)
                pv.RPC("HaulGoalSetRPC", target, ep.haulGoal);
        }
    }

    private static void SyncValuables(PlayerAvatar avatar)
    {
        var target = avatar.photonView.Owner;
        var valuables = UnityEngine.Object.FindObjectsOfType<ValuableObject>();

        foreach (var val in valuables)
        {
            if (!val.dollarValueSet) continue;

            var pv = val.GetComponent<PhotonView>();
            if (pv == null) continue;

            pv.RPC("DollarValueSetRPC", target, val.dollarValueCurrent);
        }
    }
}
