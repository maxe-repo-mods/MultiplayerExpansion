using System.Collections;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Steamworks;
using UnityEngine;

namespace MultiplayerExpansion;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public class Plugin : BaseUnityPlugin
{
    private const string PluginGuid = "maxenterme.MultiplayerExpansion";
    private const string PluginName = "MultiplayerExpansion";
    private const string PluginVersion = "1.0.2";

    internal static Plugin Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger => Instance._logger;
    private ManualLogSource _logger => base.Logger;

    // Player limit
    internal static ConfigEntry<int> MaxPlayers = null!;

    // Late join
    internal static ConfigEntry<bool> LateJoinEnabled = null!;
    internal static ConfigEntry<bool> AllowInShop = null!;
    internal static ConfigEntry<bool> AllowInTruck = null!;
    internal static ConfigEntry<bool> AllowInLevel = null!;
    internal static ConfigEntry<bool> AllowInArena = null!;
    internal static ConfigEntry<bool> KillIfPreviouslyDead = null!;

    private void Awake()
    {
        Instance = this;

        MaxPlayers = Config.Bind(
            "Player Limit", "MaxPlayers", 99,
            new ConfigDescription("Maximum number of players allowed", new AcceptableValueRange<int>(1, 99))
        );

        LateJoinEnabled = Config.Bind(
            "Late Join", "Enabled", true,
            "Enable late join functionality."
        );
        AllowInShop = Config.Bind("Late Join", "AllowInShop", true,
            "Allow late join during shop phase.");
        AllowInTruck = Config.Bind("Late Join", "AllowInTruck", true,
            "Allow late join during truck/lobby phase.");
        AllowInLevel = Config.Bind("Late Join", "AllowInLevel", true,
            "Allow late join during gameplay levels.");
        AllowInArena = Config.Bind("Late Join", "AllowInArena", true,
            "Allow late join during arena.");
        KillIfPreviouslyDead = Config.Bind("Late Join", "KillIfPreviouslyDead", true,
            "Auto-kill late joiners who died earlier in this level.");

        new Harmony(PluginGuid).PatchAll(typeof(Plugin).Assembly);
        Logger.LogInfo($"{PluginName} v{PluginVersion} loaded! MaxPlayers={MaxPlayers.Value}");

        StartCoroutine(SetSteamLobbyMax());
    }

    private IEnumerator SetSteamLobbyMax()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f);

            if (!SteamClient.IsValid) continue;
            if (SteamManager.instance == null) continue;

            var lobby = SteamManager.instance.currentLobby;
            if (lobby.Id == 0) continue;

            lobby.MaxMembers = MaxPlayers.Value;
            Logger.LogInfo($"Steam lobby MaxMembers set to {MaxPlayers.Value}");
            break;
        }
    }
}
