using System.Collections;
using System.Collections.Generic;
using Mirror;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public Dictionary<string, Card> deckDictionary;
    public List<Card> cardDeck = new List<Card>();
    [HideInInspector] public List<string> cardIdDeck;
    public int cardsPerPlayer = 2;
    public Player localPlayer;

    public int roundNumber;
    public List<PlayerSetup> playerOrder;
    [SyncVar] public int turnNumber;

    [SerializeField] private TextMeshProUGUI roundText;

    // TODO: UI and Card positions need to be ordered the same way, fix this annoyance using a new class
    [SerializeField] public List<Transform> playerUIPositions;
    [SerializeField] public List<Transform> playedCardPositions;

    // TODO: make start button only available to the host

    // should be called by the host
    public void StartGame()
    {
        if (isServer)
        {
            // TODO: make sure there are 4 players in the lobby
            if (PlayerSetup.playerList.Count > cardDeck.Count * cardsPerPlayer)
            {
                Debug.LogError("Not enough cards for the players!");
            }

            // setup the decks
            foreach (Card card in cardDeck)
            {
                string cardId = card.cardValue.ToString() + card.cardSuit;
                cardIdDeck.Add(cardId);
            }
            deckDictionary = new Dictionary<string, Card>();
            for (int i = 0; i < cardIdDeck.Count; i++)
            {
                deckDictionary.Add(cardIdDeck[i], cardDeck[i]);
            }
            RpcSetupDecks();

            // set an order to the players
            playerOrder = new List<PlayerSetup>();
            List<GameObject> players = new List<GameObject>();
            foreach (PlayerSetup player in PlayerSetup.playerList)
            {
                playerOrder.Add(player);
                players.Add(player.gameObject);
            }
            // also move player UIs to correct places
            SetPlayerUI();
            RpcSetPlayerOrder(players);

            // deal the hands
            foreach (PlayerSetup player in playerOrder)
            {
                List<string> tempHand = new List<string>();
                for (int i = 0; i < cardsPerPlayer; i++)
                {
                    int cardIndex = Random.Range(0,cardIdDeck.Count-1);
                    tempHand.Add(cardIdDeck[cardIndex]);
                    cardIdDeck.RemoveAt(cardIndex);
                }

                player.GetComponent<PlayerInventory>().ChangeHand(tempHand);
                player.GetComponent<PlayerInventory>().RpcChangeHand(tempHand);
            }

            // start the game
            // round 0 is the trick choosing part
            turnNumber = 0;
            int newRoundNumber = 0;
            DisplayRoundNumber(newRoundNumber);
            RpcDisplayRoundNumber(newRoundNumber);

            playerOrder[turnNumber].GetComponent<Player>().TurnStart();
            playerOrder[turnNumber].GetComponent<Player>().RpcTurnStart();
        }
    }

    [ClientRpc]
    private void RpcSetupDecks()
    {
        if (isServer) {return;}

        foreach (Card card in cardDeck)
        {
            string cardId = card.cardValue.ToString() + card.cardSuit;
            cardIdDeck.Add(cardId);
        }
        deckDictionary = new Dictionary<string, Card>();
        for (int i = 0; i < cardIdDeck.Count; i++)
        {
            deckDictionary.Add(cardIdDeck[i], cardDeck[i]);
        }
    }

    private void DisplayRoundNumber(int newRoundNumber)
    {
        roundNumber = newRoundNumber;
        roundText.text = "Round: " + roundNumber.ToString();
    }

    [ClientRpc]
    private void RpcDisplayRoundNumber(int newRoundNumber)
    {
        if (isServer) {return;}

        DisplayRoundNumber(newRoundNumber);
    }

    [ClientRpc]
    private void RpcSetPlayerOrder(List<GameObject> players)
    {
        if (isServer) {return;}

        foreach(GameObject player in players)
        {
            playerOrder.Add(player.GetComponent<PlayerSetup>());
        }

        SetPlayerUI();
    }

    private void SetPlayerUI()
    {
        int canvasIndex = 0;
        foreach (PlayerSetup player in playerOrder)
        {
            player.playerUICanvas.SetParent(playerUIPositions[canvasIndex]);
            player.playerUICanvas.position = playerUIPositions[canvasIndex].position;
            player.playerUICanvas.rotation = playerUIPositions[canvasIndex].rotation;

            // rotate the UI away from the player if it isn't the local player
            if (player != localPlayer.GetComponent<PlayerSetup>())
            {
                player.playerUICanvas.Rotate(0f, 180f, 0f, Space.Self);
            }
            
            canvasIndex += 1;
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
                int newRoundNumber = roundNumber;
                DisplayRoundNumber(newRoundNumber + 1);
                RpcDisplayRoundNumber(newRoundNumber + 1);
            }
        }

        playerOrder[turnNumber].GetComponent<Player>().TurnStart();
        playerOrder[turnNumber].GetComponent<Player>().RpcTurnStart();
    }
}
