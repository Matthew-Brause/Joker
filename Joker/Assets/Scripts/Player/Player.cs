using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class Player : NetworkBehaviour
{
    public bool playerTurn = false;

    // TODO:
    // needs a bet and how many bets he has

    public void TurnStart()
    {
        // TODO:
        // add visuals when its a players turn
        playerTurn = true;

        if (isLocalPlayer)
        {
            //TODO:
            // allow the player to play a card or make a bet...
        }
    }

    public void TurnEnd()
    {
        playerTurn = false;
    }

    [Command]
    public void CmdTurnEnd()
    {
        GetComponent<PlayerSetup>().gameManager.CalculateNextPlayer();

        TurnEnd();
        RpcTurnEnd();
    }

    [ClientRpc]
    public void RpcTurnEnd()
    {
        if (isServer) {return;}

        TurnEnd();
    }

    [ClientRpc]
    public void RpcTurnStart()
    {
        if (isServer) {return;}

        TurnStart();
    }
}
