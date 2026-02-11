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

    [SerializeField] private TextMeshProUGUI roundText;

    // can only be run by the server
    [ServerCallback]
    public void StartGame()
    {
        if (isServer)
        {
            // TODO:
            // make sure there are 4 players in the game before starting

            // TODO:
            // rotate all the playersUI so that they are in order

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

                player.GetComponent<PlayerInventory>().ChangeWholeHand(tempHand);
                player.GetComponent<PlayerInventory>().RpcChangeWholeHand(tempHand);
            }

            // start the game
            // round 0 is the trick choosing part
            roundNumber = 0;
            turnNumber = 0;
            DisplayRoundNumber();
            RpcDisplayRoundNumber();

            playerOrder[turnNumber].GetComponent<Player>().TurnStart();
            playerOrder[turnNumber].GetComponent<Player>().RpcTurnStart();
        }
    }

    private void DisplayRoundNumber()
    {
        roundText.text = "Round: " + roundNumber.ToString();
    }

    [ClientRpc]
    private void RpcDisplayRoundNumber()
    {
        DisplayRoundNumber();
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

    // should only be called by local player
    [ClientCallback]
    public void EndPlayerTurn()
    {
        localPlayer.CalculateActions();
    }

    // should only be called by server
    [ServerCallback]
    public void CalculateNextPlayer()
    {
        if (turnNumber < playerOrder.Count - 1)
        {
            turnNumber += 1;
        }
        else
        {
            // TODO: that was the last turn, 
            // decide winner and reorder if it wasn't the last round

            // check if it was the last round
            if (roundNumber == cardsPerPlayer)
            {
                // TODO: calculate points based on player bets
            }
            else
            {
                turnNumber = 0;
                roundNumber += 1;
                DisplayRoundNumber();
                RpcDisplayRoundNumber();
            }
        }

        playerOrder[turnNumber].GetComponent<Player>().TurnStart();
        playerOrder[turnNumber].GetComponent<Player>().RpcTurnStart();
    }
}
