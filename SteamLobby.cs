using System.Reflection;
using HarmonyLib;
using Steamworks.Data;
using UnityEngine;

namespace MultiplayerExpansion;

// v0.5.x replaced SteamManager's fields with SteamLobbyManager. R.E.P.O.GameLibs.Steam
// on NuGet stops at 0.4.4, which has no such type, so it is looked up by name.
internal static class SteamLobby
{
    private static readonly System.Type? _managerType = AccessTools.TypeByName("SteamLobbyManager");
    private static readonly FieldInfo? _instanceField =
        _managerType == null ? null : AccessTools.Field(_managerType, "instance");
    private static readonly FieldInfo? _currentLobbyField =
        _managerType == null ? null : AccessTools.Field(_managerType, "currentLobby");

    internal static bool TryGetCurrent(out Lobby lobby)
    {
        lobby = default;
        if (_instanceField == null || _currentLobbyField == null) return false;

        var manager = _instanceField.GetValue(null) as Object;
        if (manager == null) return false;

        lobby = (Lobby)_currentLobbyField.GetValue(manager);
        return lobby.Id != 0;
    }
}
