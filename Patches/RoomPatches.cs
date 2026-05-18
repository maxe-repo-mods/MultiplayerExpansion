using HarmonyLib;
using Photon.Pun;

namespace MultiplayerExpansion.Patches;

[HarmonyPatch]
public static class RoomPatches
{
    [HarmonyPatch(typeof(NetworkConnect), "OnJoinedRoom")]
    [HarmonyPostfix]
    private static void OnJoinedRoom_Postfix()
    {
        if (PhotonNetwork.CurrentRoom == null) return;
        PhotonNetwork.CurrentRoom.MaxPlayers = Plugin.MaxPlayers.Value;
        Plugin.Logger.LogInfo($"Room MaxPlayers set to {Plugin.MaxPlayers.Value}");
    }
}
