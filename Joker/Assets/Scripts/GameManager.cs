using System.Collections;
using System.Collections.Generic;
using Mirror;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public List<string> fixedDeck = new List<string>();
    public List<string> playingDeck;
    public int cardsPerPlayer = 2;
    public Player localPlayer;

    [SyncVar] public int roundNumber;
    public List<PlayerSetup> playerOrder;
    [SyncVar] public int turnNumber;

    // can only be run by the server
    [ServerCallback]
    public void StartGame()
    {
        if (isServer)
        {
            // setup the decks
            playingDeck = new List<string>(fixedDeck);

            // should set an order to the players
            playerOrder = new List<PlayerSetup>();
            List<GameObject> players = new List<GameObject>();
            foreach (PlayerSetup player in PlayerSetup.playerList)
            {
                playerOrder.Add(player);
                players.Add(player.gameObject);
            }
            RpcSetPlayerOrder(players);

            // deal the hands
            foreach (PlayerSetup player in playerOrder)
            {
                List<string> tempHand = new List<string>();
                for (int i = 0; i < cardsPerPlayer; i++)
                {
                    int cardIndex = Random.Range(1,playingDeck.Count);
                    tempHand.Add(playingDeck[cardIndex]);
                    playingDeck.RemoveAt(cardIndex);
                }

                DealHand(player.gameObject, tempHand);
                RpcDealHand(player.gameObject, tempHand);
            }

            // start the game
            roundNumber = 1;
            turnNumber = 0;

            playerOrder[turnNumber].GetComponent<Player>().TurnStart();
            playerOrder[turnNumber].GetComponent<Player>().RpcTurnStart();
        }
    }

    [ClientRpc]
    private void RpcSetPlayerOrder(List<GameObject> players)
    {
        if (isServer) {return;}

        foreach(GameObject player in players)
        {
            playerOrder.Add(player.GetComponent<PlayerSetup>());
        }
    }

    

    private void DealHand(GameObject player, List<string> hand)
    {
        player.GetComponent<PlayerInventory>().ChangeHand(hand);
    }

    [ClientRpc]
    private void RpcDealHand(GameObject player, List<string> hand)
    {
        if (isServer) {return;}

        DealHand(player, hand);
    }

    // should only be called by clients
    [ClientCallback]
    public void EndPlayerTurn()
    {
        localPlayer.CmdTurnEnd();
    }

    // should only be called by server
    [ServerCallback]
    public void CalculateNextPlayer()
    {
        
        if (turnNumber < 3)
        {
            turnNumber += 1;
        }
        else
        {
            // TODO:
            // that was the last turn, decide winner and reorder if it wasn't the last round

            // check if it was the last round
            if (roundNumber == cardsPerPlayer)
            {
                // TODO:
                // calculate points based on player bets
            }
            else
            {
                turnNumber = 0;
                roundNumber += 1;
            }
        }
        

        playerOrder[turnNumber].GetComponent<Player>().TurnStart();
        playerOrder[turnNumber].GetComponent<Player>().RpcTurnStart();
    }
}
