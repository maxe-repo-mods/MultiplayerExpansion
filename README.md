# MultiplayerExpansion

Increase the maximum player count and enable late join functionality in R.E.P.O.

R.E.P.O. BepInEx mod. Host-side only.

## Features

- Raise the maximum number of players from the default 4 to up to 99
- Enable/disable late join during different game phases (shop, truck, level, arena)
- Automatically kill late joiners who died in previous phases
- Auto-configure Photon/Steam lobby size to match player limit

## Installation

Requires [BepInEx 5.x](https://thunderstore.io/c/repo/p/BepInEx/BepInExPack/).

Place `MultiplayerExpansion.dll` in `BepInEx/plugins/`.

Configuration file is generated at `BepInEx/config/maxenterme.MultiplayerExpansion.cfg` on first launch.

## Configuration

Edit `BepInEx/config/maxenterme.MultiplayerExpansion.cfg` to customize behavior.

| Section | Key | Default | Range | Description |
|---------|-----|---------|-------|-------------|
| Player Limit | MaxPlayers | 99 | 1-99 | Maximum number of players allowed in multiplayer |
| Late Join | Enabled | true | - | Enable late join functionality |
| Late Join | AllowInShop | true | - | Allow players to join during shop phase |
| Late Join | AllowInTruck | true | - | Allow players to join during truck/lobby phase |
| Late Join | AllowInLevel | true | - | Allow players to join during active level gameplay |
| Late Join | AllowInArena | true | - | Allow players to join during arena phase |
| Late Join | KillIfPreviouslyDead | true | - | Auto-kill late joiners who died in a previous phase |

## Build

```bash
dotnet build -c Release
```

The compiled DLL will be available at:
```
bin/Release/netstandard2.1/MultiplayerExpansion.dll
```


## AI Disclosure

This mod was developed with the assistance of AI (Claude by Anthropic). All code has been reviewed and tested by the developer.

## License

MIT
