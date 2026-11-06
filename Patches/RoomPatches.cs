using HarmonyLib;
using Photon.Pun;

namespace MultiplayerExpansion.Patches;

[HarmonyPatch]
public static class RoomPatches
{
    [HarmonyPatch(typeof(GameManager), "Awake")]
    [HarmonyPostfix]
    private static void GameManager_Awake_Postfix()
    {
        Plugin.ApplyMaxPlayers();
    }

    [HarmonyPatch(typeof(NetworkConnect), "OnJoinedRoom")]
    [HarmonyPostfix]
    private static void OnJoinedRoom_Postfix()
    {
        if (PhotonNetwork.CurrentRoom == null) return;
        Plugin.ApplyMaxPlayers();
        Plugin.Logger.LogInfo($"Room MaxPlayers set to {PhotonNetwork.CurrentRoom.MaxPlayers}");
    }
}
