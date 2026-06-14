using System.Collections;
using System.Collections.Generic;
using System.Threading;
using DG.Tweening;
using Mirror;
using TMPro;
using UnityEditor.Rendering;
using UnityEngine;

public class Player2D : NetworkBehaviour
{
    public bool playerTurn = false;

    public int tricksBid = 0;
    public int tricksWon = 0;
    public List<int> pointsWon;
    public string cardInPlayID;
    public GameObject cardInPlay;
    public string selectedCardId;
    public CardInteraction2D selectedCardInteraction;
    
    [HideInInspector] public GameManager2D gameManager;
    private PlayerInventory2D inventory;

    public Transform playerUI;
    [SerializeField] private TextMeshProUGUI tricksBidText;
    [SerializeField] private TextMeshProUGUI tricksWonText;


    private void Start()
    {
        gameManager = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameManager2D>();
        inventory = GetComponent<PlayerInventory2D>();
        DisplayTricksBid(false);
        DisplayTricksWon(false);
    }
    
    public void SetupPlayer()
    {
        pointsWon = new List<int>();
        for (int i = 0; i < gameManager.cardsPerPlayerPerRound.Count; i++)
        {
            pointsWon.Add(0);
        }
    }

    [ClientRpc]
    public void RpcSetupPlayer()
    {
        if (isServer) {return;}

        SetupPlayer();
    }
    


    // TODO: reset tricks bid/won on new round start


    // should only get called by localplayer
    [ClientCallback]
    public void CalculateActions()
    {
        // just for safety
        if (isLocalPlayer)
        {

            // the player is trying to end his turn, make sure it actually is his turn
            if (!playerTurn) {return;}

            // allow the player to play a card or choose tricks...
            if (gameManager.trickNumber == 0)
            {
                // tricks was changed for the localplayer by buttons
                if (gameManager.turnNumber == gameManager.playerOrder.Count - 1)
                {
                    // don't allow last player to bid invalid amount of tricks
                    if (gameManager.currentTricksBidTotal + gameManager.currentTricksBid != gameManager.cardsPerPlayer)
                    {
                        CmdChooseTricks(gameManager.currentTricksBid);
                    }
                    else
                    {
                        Debug.Log("Can't bid that amount!");
                        return;
                    }
                }
                else
                {
                    CmdChooseTricks(gameManager.currentTricksBid);
                }

                // hide the trick bidding buttons at end of turn
                gameManager.DisplayTrickButtons(false);
            }
            else
            {
                if (selectedCardId == null || selectedCardId == "") {return;}
                // need to check that cardId is in the hand
                if (inventory.hand.Contains(selectedCardId.Substring(0,4))) // Card ID excluding joker tags (high/low/suit)
                {
                    if (gameManager.turnNumber == 0)
                    {
                        // why do we remove/pick cards on turn 0?
                        CmdChooseCard(selectedCardId);
                        inventory.CmdRemoveCard(selectedCardId);
                    }
                    else if (inventory.getValidCards().Contains(selectedCardId.Substring(0,4))) // Card ID excluding joker tags (high/low/suit)
                    {
                        // important that we pick the card before deleting it because we use the transform of the deleted card
                        CmdChooseCard(selectedCardId);
                        inventory.CmdRemoveCard(selectedCardId);
                    }
                    else
                    {
                        Debug.Log("Invalid Card Choice!");
                        return;
                    }
                }
                else
                {
                    Debug.Log("Impossible Card Choice!");
                    return;
                }
            }

            gameManager.DisplayEndTurnButton(false);
            CmdTurnEnd();
        }
    }

    private void DisplayPlayCard()
    {
        Card cardData = gameManager.deckDictionary[cardInPlayID.Substring(0,4)]; // TODO this line is might lead to possible bugs make sure it isn't

        int playerIndex = gameManager.playerUIOrder.IndexOf(GetComponent<Player2D>());
        Transform ui = gameManager.playedCardPositions[playerIndex];

        if (gameManager.turnNumber == 0)
        {
            gameManager.RemoveAllPlayedCards();
        }

        PlayerInventory2D playerInventory = GetComponent<PlayerInventory2D>();

        // animate playing the card if its from our hand
        GameObject cardPrefab = playerInventory.cardPrefab;
        if (isLocalPlayer)
        {
            // the order of cardsInHand is the same as hand...
            int cardIndex = playerInventory.hand.IndexOf(cardInPlayID.Substring(0,4));
            GameObject oldCard = playerInventory.cardsInHand[cardIndex];
            Vector3 position = oldCard.transform.position;
            Quaternion rotation = oldCard.transform.rotation;

            cardInPlay = Instantiate(cardPrefab, position, rotation);

            cardInPlay.transform.DOMove(ui.position, 0.25f);
            cardInPlay.transform.DOLocalRotateQuaternion(ui.rotation, 0.25f);
        }
        else
        {
            cardInPlay = Instantiate(cardPrefab, ui.position, ui.rotation);
        }
        cardInPlay.GetComponent<SpriteRenderer>().sprite = cardData.cardArt;
        


        // TODO: if the card is a joker, show properties like high/low and suit
        
        // the name of the card is used when the player interacts with a card
        cardInPlay.name = cardInPlayID;
    }

    public void CalculatePoints(bool wonLastRound, int roundNumber, int currentHistPoints, int roundMultiplyer)
    {
        if (wonLastRound)
        {
            tricksWon += 1;
        }

        int points = 0;
        if (tricksBid == tricksWon) // you got your bid
        {
            // if player bids for all tricks they get 100 points per trick
            if (tricksBid == gameManager.cardsPerPlayerPerRound[roundNumber])
            {
                points = 100*tricksWon;
            }
            // if player doesn't bid for all tricks they get 50 points + 50 points per trick
            else
            {
                points = 50 + 50*tricksWon; // yes this could just be 50*(tricksWon+1)
            }

            // if player didn't pass, they can multiply by roundMultiplyer
            if (points != 50)
            {
                points = points*roundMultiplyer;
            }
        }
        else if (tricksWon > 0)
        {
            points = 10*tricksWon*roundMultiplyer;
        }
        else
        {
            points = currentHistPoints*roundMultiplyer;
        }
        pointsWon[roundNumber] = points;
        UpdateScoreboard(roundNumber);

        tricksBid = 0;
        tricksWon = 0;
        DisplayTricksBid(false);
        DisplayTricksWon(false);
    }

    private void UpdateScoreboard(int roundNumber)
    {
        List<TextMeshProUGUI> scoreboardPointsText = gameManager.scoreboardPointsText;

        // find the players text
        int playerIndex = 0;
        foreach (Player2D player in gameManager.playerOrder)
        {
            if (player.netId == this.netId)
            {
                break;
            }
            playerIndex += 1;
        }

        scoreboardPointsText[playerIndex].text = pointsWon[roundNumber].ToString();
    }

    [ClientRpc]
    public void RpcCalculatePoints(bool wonLastRound, int roundNumber, int currentHistPoints, int roundMultiplyer)
    {
        if (isServer) {return;}

        CalculatePoints(wonLastRound, roundNumber, currentHistPoints, roundMultiplyer);
    }

    public void SetSelectedCard(string cardId, CardInteraction2D cardInteraction)
    {
        // checks if the card we select is in our hand ? 
        if (inventory.hand.Contains(cardId.Substring(0,4))) // Card ID excluding joker tags (high/low/suit)
        {
            // hide selection UI of old card
            if (selectedCardInteraction != null)
            {
                selectedCardInteraction.UnSelectCard();
            }

            // select and highlight new card
            selectedCardId = cardId;
            cardInteraction.SelectCard();
            selectedCardInteraction = cardInteraction;
        }
    }

    public void RemovePlayedCard()
    {
        if (cardInPlay != null)
        {
            Destroy(cardInPlay);
        }
    }


    [ClientRpc]
    public void RpcRemovePlayedCard()
    {
        if (isServer) {return;}

        RemovePlayedCard();
    }

    [Command]
    private void CmdChooseTricks(int trickAmount)
    {
        tricksBid = trickAmount;
        gameManager.currentTricksBidTotal += trickAmount;
        DisplayTricksBid(true);
        RpcChooseTricks(trickAmount);
    }

    [ClientRpc]
    private void RpcChooseTricks(int trickAmount)
    {
        if (isServer) {return;}
        
        tricksBid = trickAmount;
        gameManager.currentTricksBidTotal += trickAmount;
        DisplayTricksBid(true);
    }

    public void DisplayTricksBid(bool enable)
    {
        if (enable) 
        {
            tricksBidText.text = "Tricks Bid: " + tricksBid.ToString();
        }
        tricksBidText.gameObject.SetActive(enable);
    }

    [ClientRpc]
    public void RpcWonTrick()
    {
        tricksWon += 1;
        DisplayTricksWon(true);
    }

    public void DisplayTricksWon(bool enable)
    {
        if (enable) 
        {
            tricksWonText.text = "Tricks Won: " + tricksWon.ToString();
        }
        tricksWonText.gameObject.SetActive(enable);
    }

    [Command]
    private void CmdChooseCard(string cardId)
    {
        ChooseCard(cardId);
        gameManager.trickCards.Add(cardId);
        DisplayPlayCard();
        RpcChooseCard(cardId);
    }

    [ClientRpc]
    private void RpcChooseCard(string cardId)
    {
        if (isServer) {return;}
        
        ChooseCard(cardId);

        DisplayPlayCard();
    }

    private void ChooseCard(string cardId)
    {
        cardInPlayID = cardId;
        if (gameManager.turnNumber == 0)
        {
            gameManager.initialCardId = cardId;
            if (cardId[3] == 'j')
            {
                gameManager.initialCardSuit = cardId[5];
            }
            else
            {
                gameManager.initialCardSuit = cardId[3];
            }
            gameManager.initialCardValue = int.Parse(cardId.Substring(0,2)); // TODO currently unused
        }
    }

    private void TurnEnd()
    {
        playerTurn = false;
    }

    [Command]
    public void CmdTurnEnd()
    {
        TurnEnd();
        RpcTurnEnd();

        gameManager.CalculateNextPlayer();
    }

    [ClientRpc]
    public void RpcTurnEnd()
    {
        if (isServer) {return;}

        TurnEnd();
    }

    public void TurnStart()
    {
        // TODO: add visuals when its a players turn
        playerTurn = true;

        if (isLocalPlayer)
        {
            gameManager.DisplayEndTurnButton(true);

            if (gameManager.trickNumber == 0)
            {
                gameManager.DisplayTrickButtons(true);
            }
        }
    }

    [ClientRpc]
    public void RpcTurnStart()
    {
        if (isServer) {return;}

        TurnStart();
    }

    public void GiveJokerTrumpOptions()
    {
        if (isLocalPlayer)
        {
            gameManager.DisplayJokerTrumpOptions(true);
        }
    }

    [ClientRpc]
    public void RpcGiveJokerTrumpOptions()
    {
        if (isServer) {return;}

        GiveJokerTrumpOptions();
    }
    
    public void GiveThreeTrumpOptions()
    {
        if (isLocalPlayer)
        {
            gameManager.DisplayThreeTrumpOptions(true);
        }
    }

    [ClientRpc]
    public void RpcGiveThreeTrumpOptions()
    {
        if (isServer) {return;}

        GiveThreeTrumpOptions();
    }

}
