using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Mirror;
using TMPro;
using UnityEditor.Rendering;
using UnityEngine;

public class Player2D : NetworkBehaviour
{
    public bool playerTurn = false;

    public int tricksBid = 0;
    public int tricksWon = 0;
    public string cardInPlayID;
    public GameObject cardInPlay;
    public string selectedCardId;
    public CardInteraction2D selectedCardInteraction;
    
    [HideInInspector] public GameManager2D gameManager;
    private PlayerInventory2D inventory;

    public Transform playerUI;
    [SerializeField] private TextMeshProUGUI tricksBidText;
    [SerializeField] private TextMeshProUGUI tricksWonText;

    // Start is called before the first frame update
    private void Start()
    {
        gameManager = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameManager2D>();
        inventory = GetComponent<PlayerInventory2D>();
        tricksBidText.gameObject.SetActive(false);
        tricksWonText.gameObject.SetActive(false);
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
                        CmdChooseCard(selectedCardId);
                        inventory.CmdRemoveCard(selectedCardId);
                    }
                    else if (inventory.getValidCards().Contains(selectedCardId.Substring(0,4))) // Card ID excluding joker tags (high/low/suit)
                    {
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
                
                // TODO: hide the input for choosing a card
                // we think this means that we should hide the end turn button until it's your turn
            }

            gameManager.DisplayEndTurnButton(false);
            CmdTurnEnd();
        }
    }

    private void DisplayPlayCard()
    {
        Card cardData = gameManager.deckDictionary[cardInPlayID.Substring(0,4)]; // TODO this line is might lead to possible bugs make sure it isn't

        int playerIndex = gameManager.playerOrder.IndexOf(GetComponent<Player2D>());
        Transform ui = gameManager.playedCardPositions[playerIndex];

        if (gameManager.turnNumber == 0)
        {
            gameManager.RemoveAllPlayedCards();
        }

        GameObject cardPrefab = GetComponent<PlayerInventory2D>().cardPrefab;
        cardInPlay = Instantiate(cardPrefab, ui.position, ui.rotation);
        cardInPlay.GetComponent<SpriteRenderer>().sprite = cardData.cardArt;
        
        // the name of the card is used when the player interacts with a card
        cardInPlay.name = cardInPlayID;
    }

    public void SetSelectedCard(string cardId, CardInteraction2D cardInteraction)
    {
        if (inventory.hand.Contains(cardId.Substring(0,4))) // Card ID excluding joker tags (high/low/suit)
        {
            // unhighlight old card
            if (selectedCardInteraction != null)
            {
                selectedCardInteraction.SetHighlightCard(false);
            }

            // select and highlight new card
            selectedCardId = cardId;
            cardInteraction.SetHighlightCard(true);
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
        DisplayTricksBid();
        RpcChooseTricks(trickAmount);
    }

    [ClientRpc]
    private void RpcChooseTricks(int trickAmount)
    {
        if (isServer) {return;}
        
        tricksBid = trickAmount;
        gameManager.currentTricksBidTotal += trickAmount;
        DisplayTricksBid();
    }

    public void DisplayTricksBid()
    {
        tricksBidText.text = "Tricks Bid: " + tricksBid.ToString();
        tricksBidText.gameObject.SetActive(true);
    }

    [ClientRpc]
    public void RpcWonTrick()
    {
        tricksWon += 1;
        DisplayTricksWon();
    }

    public void DisplayTricksWon()
    {
        tricksWonText.text = "Tricks Won: " + tricksWon.ToString();
        tricksWonText.gameObject.SetActive(true);
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

}
