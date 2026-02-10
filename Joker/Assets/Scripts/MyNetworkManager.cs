using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Steamworks;

public class MyNetworkManager : NetworkManager
{
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);

        CSteamID steamId = SteamMatchmaking.GetLobbyMemberByIndex(
            SteamLobby3.lobbyID,
            numPlayers - 1);

        var playerSteamData = conn.identity.GetComponent<PlayerSteamData>();

        playerSteamData.SetSteamId(steamId.m_SteamID);
    }
}
