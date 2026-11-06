using System;
using HarmonyLib;
using Photon.Pun;
using Steamworks;

namespace MultiplayerExpansion.Patches;

[HarmonyPatch]
public static class LobbyPatches
{
    [HarmonyPatch(typeof(GameDirector), nameof(GameDirector.SetStart))]
    [HarmonyPostfix]
    private static void GameDirector_SetStart_Postfix()
    {
        try
        {
            if (!Plugin.LateJoinEnabled.Value) return;
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
            if (!ShouldAllowJoin()) return;

            OpenLobby();
            Plugin.Logger.LogInfo("LateJoin: lobby opened after level generation.");
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError($"LateJoin SetStart error: {e}");
        }
    }

    [HarmonyPatch(typeof(RunManager), nameof(RunManager.ChangeLevel))]
    [HarmonyPrefix]
    private static void RunManager_ChangeLevel_Prefix()
    {
        try
        {
            if (!Plugin.LateJoinEnabled.Value) return;
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;

            CloseLobby();
            Plugin.Logger.LogDebug("LateJoin: lobby closed for level change.");
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError($"LateJoin ChangeLevel error: {e}");
        }
    }

    internal static bool ShouldAllowJoin()
    {
        if (SemiFunc.RunIsShop()) return Plugin.AllowInShop.Value;
        if (SemiFunc.RunIsArena()) return Plugin.AllowInArena.Value;
        if (SemiFunc.RunIsLevel()) return Plugin.AllowInLevel.Value;
        return Plugin.AllowInTruck.Value;
    }

    private static void OpenLobby()
    {
        try
        {
            if (PhotonNetwork.CurrentRoom != null)
                PhotonNetwork.CurrentRoom.IsOpen = true;
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError($"LateJoin OpenLobby Photon error: {e}");
        }

        try
        {
            if (!SteamClient.IsValid) return;
            if (SteamLobby.TryGetCurrent(out var lobby))
                lobby.SetJoinable(true);
        }
        catch { }
    }

    private static void CloseLobby()
    {
        try
        {
            if (PhotonNetwork.CurrentRoom != null)
                PhotonNetwork.CurrentRoom.IsOpen = false;
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError($"LateJoin CloseLobby Photon error: {e}");
        }

        try
        {
            if (!SteamClient.IsValid) return;
            if (SteamLobby.TryGetCurrent(out var lobby))
                lobby.SetJoinable(false);
        }
        catch { }
    }
}
