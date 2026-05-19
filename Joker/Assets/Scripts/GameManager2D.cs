using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Mirror;
using Steamworks;
using TMPro;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;

public class GameManager2D : NetworkBehaviour
{
    public Dictionary<string, Card> deckDictionary;
    public List<Card> cardDeck = new List<Card>();
    [HideInInspector] public List<string> cardIdDeck;
    public int cardsPerPlayer = 2;
    [HideInInspector] public Player2D localPlayer;

    public int trickNumber;
    public List<Player2D> playerOrder;
    [SyncVar] public int turnNumber;
    public int playerStarted = 0;

    public int currentTricksBid = 0;
    public int currentTricksBidTotal = 0;

    public string trumpCardId;
    public char trumpSuit;
    public GameObject trumpCard;
    public GameObject cardPrefab;
    public string initialCardId;
    public char initialCardSuit;
    public int initialCardValue;
    public List<string> trickCards;
    public int roundMultiplyer; // If a joker is the trump suit, the first bidder can choose to have the hands reshuffled at +1 round mult

    [SerializeField] private TextMeshProUGUI trickNumberText;
    [SerializeField] private TextMeshProUGUI trickBiddingText;
    [SerializeField] private GameObject trickButtons;
    [SerializeField] private GameObject endTurnButton;

    // TODO: UI and Card positions need to be ordered the same way, fix this annoyance using a new class
    [SerializeField] public List<Transform> playerUIPositions;
    public Transform localPlayerHandPosition;
    [SerializeField] public List<Transform> playedCardPositions;
    public Transform trumpCardPosition;

    // TODO: make start button only available to the host
    private void Start()
    {
        DisplayTrickButtons(false);
        DisplayEndTurnButton(false);
    }

    public void StartGame()
    {
        if (isServer)
        {
            SetupDecks();
            RpcSetupDecks();

            // set an order to the players
            playerOrder = new List<Player2D>();
            List<GameObject> players = new List<GameObject>();
            foreach (Player2D player in PlayerSetup2D.playerList)
            {
                playerOrder.Add(player);
                players.Add(player.gameObject);
            }
            SetPlayerOrder(players);
            RpcSetPlayerOrder(players);
            
            StartRound();
        }
    }

    public void StartRound()
    {
        if (isServer)
        {
            // TODO: make sure there are 4 players in the lobby
            // Shouldn't ever happen once game is done
            if (PlayerSetup2D.playerList.Count > cardDeck.Count * cardsPerPlayer)
            {
                Debug.LogError("Not enough cards for the players!");
            }

            // deal the hands
            List<string> tempCardIdDeck = new List<string>(cardIdDeck);
            foreach (Player2D player in playerOrder)
            {
                List<string> tempHand = new List<string>();
                for (int i = 0; i < cardsPerPlayer; i++)
                {
                    int cardIndex = Random.Range(0,tempCardIdDeck.Count);
                    tempHand.Add(tempCardIdDeck[cardIndex]);
                    tempCardIdDeck.RemoveAt(cardIndex);
                }

                player.GetComponent<PlayerInventory2D>().ChangeHand(tempHand);
                player.GetComponent<PlayerInventory2D>().RpcChangeHand(tempHand);
            }


            // determine the trump card
            if (tempCardIdDeck.Count > 0)
            {
                int cardIndex = Random.Range(0,tempCardIdDeck.Count);
                string tempTrumpCardId = tempCardIdDeck[cardIndex];
                SetTrumpCard(tempTrumpCardId);
                RpcSetTrumpCard(tempTrumpCardId);
            }
            

            // start the trick
            // loop 0 is the trick choosing part
            turnNumber = 0;
            int newTrickNumber = 0;
            DisplayTrickNumber(newTrickNumber);
            RpcDisplayTrickNumber(newTrickNumber);

            playerOrder[turnNumber].GetComponent<Player2D>().TurnStart();
            playerOrder[turnNumber].GetComponent<Player2D>().RpcTurnStart();
        }
    }



    // [Command]
    // private void CmdSetPlayerOrder(List<GameObject> players)
    // {
    //     SetPlayerOrder(players);
    //     RpcSetPlayerOrder(players);
    // }

    private void SetPlayerOrder(List<GameObject> players)
    {
        playerOrder = new List<Player2D>();
        for (int i = 0; i < players.Count; i++)
        {
            // find who the local player is
            if (players[i].GetComponent<Player2D>() == localPlayer)
            {
                // add players to the list after local player
                for (int j = 0; j < players.Count; j++)
                {
                    Player2D player = players[(i+j)%players.Count].GetComponent<Player2D>();
                    playerOrder.Add(player);
                }
            }
        }
        SetPlayerUI();
    }

    [ClientRpc]
    private void RpcSetPlayerOrder(List<GameObject> players)
    {
        if (isServer) {return;}

        SetPlayerOrder(players);
    }

    private void SetupDecks()
    {
        // setup the decks
        cardIdDeck = new List<string>();
        foreach (Card card in cardDeck)
        {
            string cardId = card.cardValue.ToString("D2") + "-" + card.cardSuit;
            cardIdDeck.Add(cardId);
        }
        deckDictionary = new Dictionary<string, Card>();
        for (int i = 0; i < cardIdDeck.Count; i++)
        {
            deckDictionary.Add(cardIdDeck[i], cardDeck[i]);
        }
    }



    private void DisplayTrickNumber(int newTrickNumber)
    {
        trickNumber = newTrickNumber;
        if (trickNumber == 0)
        {
            trickNumberText.text = "Bidding";
        } 
        else if (trickNumber == -1)
        {
            trickNumberText.text = "In Between Rounds";
            currentTricksBidTotal = 0;
        }
        else
        {
            trickNumberText.text = "Trick Number: " + trickNumber.ToString();
        }
    }

    private void DisplayTrumpCard()
    {
        if (trumpCard != null)
        {
            Destroy(trumpCard);
        } 
        Card cardData = deckDictionary[trumpCardId];

        Transform ui = trumpCardPosition;

        trumpCard = Instantiate(cardPrefab, ui.position, ui.rotation);
        trumpCard.GetComponent<SpriteRenderer>().sprite = cardData.cardArt;
        
        // the name of the card is used when the player interacts with a card
        trumpCard.name = trumpCardId;
    }

    [ClientRpc]
    private void RpcDisplayTrickNumber(int newTrickNumber)
    {
        if (isServer) {return;}

        DisplayTrickNumber(newTrickNumber);
    }

    private void SetPlayerUI()
    {
        int canvasIndex = 0;
        foreach (Player2D player in playerOrder)
        {
            player.playerUI.SetParent(playerUIPositions[canvasIndex]);
            player.playerUI.position = playerUIPositions[canvasIndex].position;
            player.playerUI.rotation = playerUIPositions[canvasIndex].rotation;
            
            canvasIndex += 1;
        }
    }

    // should only be called by local player
    [ClientCallback]
    public void EndPlayerTurn()
    {
        // Stop "dealer" from bidding illegal amount
        if (trickNumber == 0 && turnNumber == playerOrder.Count-1)
        {
            if (currentTricksBidTotal == cardsPerPlayer) 
            {
                Debug.Log("Invalid Trick Choice");
                return;
            }
        } 
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
            // that was the last turn
            if (trickNumber != 0)
            {
                // decide winner and reorder if it wasn't the last round
                string bestCard = trickCards[0];
                int bestCardPosition = 0;
                for (int i = 1; i < trickCards.Count; i++)
                {
                    if (IsCardBetter(bestCard, trickCards[i]))
                    {
                        bestCard = trickCards[i];
                        bestCardPosition = i;
                    }
                }
                playerStarted = (playerStarted+bestCardPosition)%playerOrder.Count;

                playerOrder[playerStarted].RpcWonTrick();

                trickCards = new List<string>();
            }
            // check if it was the last trick
            if (trickNumber == cardsPerPlayer)
            {
                DisplayTrickNumber(-1);
                RpcDisplayTrickNumber(-1);

                playerStarted = 0;
                // TODO handle new person being dealer
                // TODO: calculate points based on player bets
            }
            else
            {
                turnNumber = 0;
                int newTrickNumber = trickNumber;
                DisplayTrickNumber(newTrickNumber + 1);
                RpcDisplayTrickNumber(newTrickNumber + 1);
            }
        }


        int trueIndex = (playerStarted+turnNumber)%playerOrder.Count;
        playerOrder[trueIndex].GetComponent<Player2D>().TurnStart();
        playerOrder[trueIndex].GetComponent<Player2D>().RpcTurnStart();
    }

    public void RemoveAllPlayedCards()
    {
        foreach (Player2D player in playerOrder)
        {
            player.RemovePlayedCard();
            //player.RpcRemovePlayedCard();
        }
    }

    private bool IsCardBetter(string currentCard, string newCard)
    {
        int currentCardValue = int.Parse(currentCard.Substring(0,2));
        char currentCardSuit = currentCard[3];
        int newCardValue = int.Parse(newCard.Substring(0,2));
        char newCardSuit = newCard[3];
        if (newCardSuit == 'j') 
        {
            if (newCard[4] == 'h')
            {
                return true; 
            }
            else if (newCard[4] == 'l')
            {
                return false; 
            }
            else
            {
                Debug.Log("Error: Joker not high or low");
            }
        } 
        else if (currentCardSuit == 'j') 
        {
            if (currentCard[4] == 'h')
            {
                return false; // left these as separate because I would look at this later and forget that I handled jokers
            }
        } 


        // if current card is following suit
        if (currentCardSuit == initialCardSuit)
        {
            // if new card follows suit
            if (newCardSuit == initialCardSuit)
            {
                if (newCardValue > currentCardValue) 
                {
                    return true;
                }
                else 
                {
                    return false;
                }
            }
            // if new card is trump
            else if (newCardSuit == trumpSuit)
            {
                return true;
            }
            else 
            {
                return false;
            }
        }
        // if current card is trump
        else if (currentCardSuit == trumpSuit)
        {
            if (newCardSuit == trumpSuit)
            {
                if (newCardValue > currentCardValue) 
                {
                    return true;
                }
                else 
                {
                    return false;
                }
            }
            else 
            {
                return false;
            }
        }
        else 
        {
            return false;
        }
    }

    [ClientCallback]
    public void PlusTrickBid()
    {        
        if (currentTricksBid < cardsPerPlayer)
        {
            currentTricksBid++;
            DisplayTricksBid();
        }
    }

    [ClientCallback]
    public void MinusTrickBid()
    {
        if (currentTricksBid > 0)
        {
            currentTricksBid--;
            DisplayTricksBid();
        }
    }

    public void DisplayTrickButtons(bool enable)
    {
        trickButtons.SetActive(enable);
    }

    public void DisplayTricksBid()
    {
        trickBiddingText.text = currentTricksBid.ToString();
    }

    public void DisplayEndTurnButton(bool enable)
    {
        endTurnButton.SetActive(enable);
    }

    private void SetTrumpCard(string cardId)
    {
        trumpCardId = cardId;
        SetTrumpSuit(trumpCardId[3]);
        DisplayTrumpCard();
    }

    [ClientRpc]
    private void RpcSetTrumpCard(string cardId)
    {
        if (isServer) {return;}
        
        SetTrumpCard(cardId);
    }

    private void SetTrumpSuit(char suit)
    {
        trumpSuit = suit;
    }

    [ClientRpc]
    private void RpcSetTrumpSuit(char suit)
    {
        if (isServer) {return;}
        
        SetTrumpSuit(suit);
    }

    [ClientRpc]
    private void RpcSetupDecks()
    {
        if (isServer) {return;}

        SetupDecks();
    }
}


