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
    private List<string> ninesCardIdDeck;
    [HideInInspector] public int roundNumber;
    [HideInInspector] public int cardsPerPlayer;
    [HideInInspector] public List<int> cardsPerPlayerPerRound;
    [HideInInspector] public Player2D localPlayer;

    [HideInInspector] public int trickNumber;
    [HideInInspector] public List<Player2D> playerOrder;
    [HideInInspector] [SyncVar] public int turnNumber;
    [HideInInspector] [SyncVar] public int roundStartingPlayer;
    [HideInInspector] public int lastPlayerWonIndex;

    [HideInInspector] public int currentTricksBid = 0;
    [HideInInspector] public int currentTricksBidTotal = 0;

    [HideInInspector] public string trumpCardId;
    [HideInInspector] public char trumpSuit;
    [HideInInspector] public GameObject trumpCard;
    public GameObject cardPrefab;
    [HideInInspector] public string initialCardId;
    [HideInInspector] public char initialCardSuit;
    [HideInInspector] public int initialCardValue;
    [HideInInspector] public List<string> trickCards;
    [HideInInspector] public int roundMultiplyer; // If a joker is the trump suit, the first bidder can choose to have the hands reshuffled at +1 round mult
    [HideInInspector] public int currentHistPoints;


    [SerializeField] private TextMeshProUGUI trickNumberText;
    [SerializeField] private TextMeshProUGUI trickBiddingText;
    [SerializeField] private TextMeshProUGUI trumpText;

    [SerializeField] private GameObject trickButtons;
    [SerializeField] private GameObject endTurnButton;
    [SerializeField] private GameObject startGameButton;
    [SerializeField] private GameObject trumpJokerButtons;
    [SerializeField] private GameObject trumpNinesButtons;

    [SerializeField] private Sprite spadeSuitArt;
    [SerializeField] private Sprite heartSuitArt;
    [SerializeField] private Sprite diamondSuitArt;
    [SerializeField] private Sprite clubSuitArt;
    [SerializeField] private Sprite noneSuitArt;

    // TODO: UI and Card positions need to be ordered the same way, fix this annoyance using a new class
    [SerializeField] public List<Transform> playerUIPositions;
    public Transform localPlayerHandPosition;
    [SerializeField] public List<Transform> playedCardPositions;
    public Transform trumpCardPosition;

    private void Start()
    {
        DisplayTrickButtons(false);
        DisplayEndTurnButton(false);
        DisplayJokerTrumpOptions(false);
        DisplayThreeTrumpOptions(false);
        trumpText.gameObject.SetActive(false);
        cardsPerPlayerPerRound = new List<int>{9,9,1,2,3,4,5,6,7,8,9,9,9,9,8,7,6,5,4,3,2,1,9,9,9,9};

        if (isServer)
        {
            DisplayStartGameButton(true);
        }
        else
        {
            DisplayStartGameButton(false);
        }
        SetupDecks();
    }

    private void DisplayStartGameButton(bool enable)
    {
        startGameButton.SetActive(enable);
    }

    public void StartGame()
    {
        roundNumber = -1;
        if (isServer)
        {
            // set an order to the players
            playerOrder = new List<Player2D>();
            List<GameObject> players = new List<GameObject>();

            // add the 1st player (host) always at the start
            Player2D player = PlayerSetup2D.playerList[0];
            playerOrder.Add(player);
            players.Add(player.gameObject);
            player.SetupPlayer();
            player.RpcSetupPlayer();

            // create the list of indexes corresponding to total players: e.g. {1,2,3}
            List<int> randomizer = new List<int>();
            for (int i = 1; i < PlayerSetup2D.playerList.Count; i++)
            {
                randomizer.Add(i);
            }

            Shuffle(randomizer);
            foreach (int index in randomizer)
            {
                player = PlayerSetup2D.playerList[index];
                playerOrder.Add(player);
                players.Add(player.gameObject);
                player.SetupPlayer();
                player.RpcSetupPlayer();
            }
            roundStartingPlayer = Random.Range(1, playerOrder.Count);
            lastPlayerWonIndex = 0;

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
            
            // that was the last round
            roundNumber += 1;
            if (roundNumber == cardsPerPlayerPerRound.Count)
            {
                // TODO: stop all play and just show scoreboard
                return;
            }

            cardsPerPlayer = cardsPerPlayerPerRound[roundNumber];
            RpcChangeRound(roundNumber);
            if (PlayerSetup2D.playerList.Count > cardDeck.Count * cardsPerPlayer)
            {
                Debug.LogError("Not enough cards for the players!");
            }

            // start the trick
            // loop 0 is the trick choosing part
            roundMultiplyer = 1; // TODO: have round multiplyer change with joker being trump stuff
            currentHistPoints = -200; // TODO: have hist points change according to settings and rounds

            roundStartingPlayer = (roundStartingPlayer+1)%playerOrder.Count;
            turnNumber = 0; 
            ChangeTrickNumber(0);
            RpcChangeTrickNumber(0);

            if (cardsPerPlayerPerRound[roundNumber] != 9)
            {
                DealCards();
            }
            else
            {
                DealThreeCards();
            }
            
        }
    }

    public void DisplayJokerTrumpOptions(bool enable)
    {
        trumpJokerButtons.SetActive(enable);
    }

    public void DisplayThreeTrumpOptions(bool enable)
    {
        trumpNinesButtons.SetActive(enable);
    }

    private void CheckJokerTrump(string attemptedTrumpCardId)
    {
        if (attemptedTrumpCardId[3] == 'j')
        {
            // give option to next player to do either no trump or double points
            playerOrder[roundStartingPlayer].GetComponent<Player2D>().GiveJokerTrumpOptions();
            playerOrder[roundStartingPlayer].GetComponent<Player2D>().RpcGiveJokerTrumpOptions();
        }
        else
        {
            playerOrder[roundStartingPlayer].GetComponent<Player2D>().TurnStart();
            playerOrder[roundStartingPlayer].GetComponent<Player2D>().RpcTurnStart();
        }
    }

    public void IncreaseRoundMultiplier()
    {
        roundMultiplyer += 1;

        // hide the buttons
        DisplayJokerTrumpOptions(false);

        DealCards();
        
    }

    public void ContinueRoundWithJokerTrump()
    {
        // hide the buttons
        DisplayJokerTrumpOptions(false);

        playerOrder[roundStartingPlayer].GetComponent<Player2D>().TurnStart();
        playerOrder[roundStartingPlayer].GetComponent<Player2D>().RpcTurnStart();
    }

    private void DealCards()
    {
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
        if (tempCardIdDeck.Count > 0 && cardsPerPlayerPerRound[roundNumber] != 9)
        {
            int cardIndex = Random.Range(0,tempCardIdDeck.Count);
            string tempTrumpCardId = tempCardIdDeck[cardIndex];
            SetTrumpCard(tempTrumpCardId);
            RpcSetTrumpCard(tempTrumpCardId);

            CheckJokerTrump(tempTrumpCardId);
        }
    }
    
    public void PickThreeTrumpSuit(string suit)
    {
        // this function should be called by a button on a localplayer's gamemanager

        // hide the buttons
        DisplayThreeTrumpOptions(false);
        CmdPickThreeTrumpSuit(suit);
    }

    [Command(requiresAuthority = false)]
    private void CmdPickThreeTrumpSuit(string trumpCardId)
    {
        // trump card id will be bogus like 00-h, 00-d, 00-c, 00-s, 00-n
        SetTrumpCard(trumpCardId);
        RpcSetTrumpCard(trumpCardId);

        // finish dealing player ones cards
        Player2D playerOne = playerOrder[roundStartingPlayer];
        List<string> tempPlayerOneHand = new List<string>();
        for (int i = 0; i < 6; i++)
        {
            int cardIndex = Random.Range(0,ninesCardIdDeck.Count);
            tempPlayerOneHand.Add(ninesCardIdDeck[cardIndex]);
            ninesCardIdDeck.RemoveAt(cardIndex);
        }
        playerOne.GetComponent<PlayerInventory2D>().AddToHand(tempPlayerOneHand);
        playerOne.GetComponent<PlayerInventory2D>().RpcAddToHand(tempPlayerOneHand);

        // deal the rest of the cards to other players
        foreach (Player2D player in playerOrder)
        {
            if (player != playerOne)
            {
                List<string> tempHand = new List<string>();
                for (int i = 0; i < cardsPerPlayer; i++)
                {
                    int cardIndex = Random.Range(0,ninesCardIdDeck.Count);
                    tempHand.Add(ninesCardIdDeck[cardIndex]);
                    ninesCardIdDeck.RemoveAt(cardIndex);
                }

                player.GetComponent<PlayerInventory2D>().ChangeHand(tempHand);
                player.GetComponent<PlayerInventory2D>().RpcChangeHand(tempHand);                
            }
        }

        playerOne.GetComponent<Player2D>().TurnStart();
        playerOne.GetComponent<Player2D>().RpcTurnStart();
    }

    private void DealThreeCards()
    {
        // deal starting player just three cards
        Player2D playerOne = playerOrder[roundStartingPlayer];
        ninesCardIdDeck = new List<string>(cardIdDeck);
        List<string> tempPlayerOneHand = new List<string>();
        for (int i = 0; i < 3; i++)
        {
            int cardIndex = Random.Range(0,ninesCardIdDeck.Count);
            tempPlayerOneHand.Add(ninesCardIdDeck[cardIndex]);
            ninesCardIdDeck.RemoveAt(cardIndex);
        }
        playerOne.GetComponent<PlayerInventory2D>().ChangeHand(tempPlayerOneHand);
        playerOne.GetComponent<PlayerInventory2D>().RpcChangeHand(tempPlayerOneHand);

        // show buttons and allow to pick trump
        playerOne.GetComponent<Player2D>().GiveThreeTrumpOptions();
        playerOne.GetComponent<Player2D>().RpcGiveThreeTrumpOptions();
    }

    [ClientRpc]
    private void RpcChangeRound(int newRoundNumber)
    {
        if (isServer) {return;}
        roundNumber = newRoundNumber;
        cardsPerPlayer = cardsPerPlayerPerRound[roundNumber];
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

    private void DisplayTrumpCard()
    {
        if (trumpCard != null)
        {
            Destroy(trumpCard);
        } 

        Transform ui = trumpCardPosition;
        trumpCard = Instantiate(cardPrefab, ui.position, ui.rotation);

        // check if the trump card is only a suit and not a real card
        if (trumpCardId.Substring(0,2) == "00" && trumpCardId[3] != 'j')
        {
            if (trumpCardId[3] == 's')
            {
                trumpCard.GetComponent<SpriteRenderer>().sprite = spadeSuitArt;
            }
            else if (trumpCardId[3] == 'h')
            {
                trumpCard.GetComponent<SpriteRenderer>().sprite = heartSuitArt;
            }
            else if (trumpCardId[3] == 'd')
            {
                trumpCard.GetComponent<SpriteRenderer>().sprite = diamondSuitArt;
            }
            else if (trumpCardId[3] == 'c')
            {
                trumpCard.GetComponent<SpriteRenderer>().sprite = clubSuitArt;
            }
            else if (trumpCardId[3] == 'n')
            {
                trumpCard.GetComponent<SpriteRenderer>().sprite = noneSuitArt;
            }
            else
            {
                Debug.Log("Error: Called DisplayTrumpSuit with invalid suit");
            }
        }
        else
        {
            Card cardData = deckDictionary[trumpCardId];
            trumpCard.GetComponent<SpriteRenderer>().sprite = cardData.cardArt;
        }

        // the name of the card is used when the player interacts with a card
        trumpCard.name = trumpCardId;

        trumpText.gameObject.SetActive(true);
    }

    private void ChangeTrickNumber(int newTrickNumber)
    {
        trickNumber = newTrickNumber;
        DisplayTrickNumber();
        
    }

    private void DisplayTrickNumber()
    {
        if (trickNumber == 0)
        {
            trickNumberText.text = "Trick Bidding";
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

    [ClientRpc]
    private void RpcChangeTrickNumber(int newTrickNumber)
    {
        if (isServer) {return;}

        ChangeTrickNumber(newTrickNumber);
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
        localPlayer.CalculateActions();
    }

    // should only be called by server
    [ServerCallback]
    public void CalculateNextPlayer()
    {
        // not the last turn in a trick/bid
        if (turnNumber < playerOrder.Count - 1)
        {
            turnNumber += 1;
        }
        else 
        {
            // not the bidding part and not the last trick
            if (trickNumber != 0  && trickNumber != cardsPerPlayer)
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
                lastPlayerWonIndex = (lastPlayerWonIndex+bestCardPosition)%playerOrder.Count;

                playerOrder[lastPlayerWonIndex].RpcWonTrick();

                trickCards = new List<string>();
            }

            // check if it was the last trick
            if (trickNumber == cardsPerPlayer)
            {
                // TODO: copypasted code needs to be made a function
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
                lastPlayerWonIndex = (lastPlayerWonIndex+bestCardPosition)%playerOrder.Count;

                foreach (Player2D player in playerOrder)
                {
                    // add tricks won to calculate points cause we avoid it on last trick
                    if (player != playerOrder[lastPlayerWonIndex])
                    {
                        player.CalculatePoints(false, roundNumber, currentHistPoints, roundMultiplyer);
                        player.RpcCalculatePoints(false, roundNumber, currentHistPoints, roundMultiplyer);
                    }
                    else
                    {
                        player.CalculatePoints(true, roundNumber, currentHistPoints, roundMultiplyer);
                        player.RpcCalculatePoints(true, roundNumber, currentHistPoints, roundMultiplyer);   
                    }
                }
                lastPlayerWonIndex = 0;

                ChangeTrickNumber(-1);
                RpcChangeTrickNumber(-1);

                StartRound();
                // TODO handle new person being dealer
            }
            else
            {
                turnNumber = 0;
                int newTrickNumber = trickNumber + 1;
                ChangeTrickNumber(newTrickNumber);
                RpcChangeTrickNumber(newTrickNumber);
            }
        }

        int trueIndex = (lastPlayerWonIndex+turnNumber)%playerOrder.Count;
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
            DisplayBiddingTricks();
        }
    }

    [ClientCallback]
    public void MinusTrickBid()
    {
        if (currentTricksBid > 0)
        {
            currentTricksBid--;
            DisplayBiddingTricks();
        }
    }

    public void DisplayTrickButtons(bool enable)
    {
        trickButtons.SetActive(enable);
    }

    public void DisplayBiddingTricks()
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

    void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}


