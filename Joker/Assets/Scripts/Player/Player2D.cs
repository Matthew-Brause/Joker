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
    [SyncVar] public int tricksWon;
    public string cardInPlayID;
    public GameObject cardInPlay;
    public string selectedCard;
    
    [HideInInspector] public GameManager2D gameManager;
    private PlayerInventory2D inventory;

    public Transform playerUI;
    [SerializeField] private TextMeshProUGUI trickText;

    // Start is called before the first frame update
        private void Start()
    {
        gameManager = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameManager2D>();
        inventory = GetComponent<PlayerInventory2D>();
    }



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
                CmdChooseTricks(tricksBid);

                // TODO: hide the buttons for choosing tricks
            }
            else
            {
                // need to check that cardId is in the hand
                if (inventory.hand.Contains(selectedCard))
                {
                    CmdChooseCard(selectedCard);
                    inventory.CmdRemoveCard(selectedCard);
                }
                else
                {
                    Debug.Log("Invalid Card Choice!");
                    return;
                }
                
                // TODO: hide the input for choosing a card
            }

            CmdTurnEnd();
        }
    }

    private void DisplayPlayCard()
    {
        Card cardData = gameManager.deckDictionary[cardInPlayID];

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
        DisplayTricks();
        RpcChooseTricks(trickAmount);
    }

    [ClientRpc]
    private void RpcChooseTricks(int trickAmount)
    {
        if (isServer) {return;}

        tricksBid = trickAmount;
        DisplayTricks();
    }

    public void DisplayTricks()
    {
        trickText.text = "Tricks Bid: " + tricksBid.ToString();
    }

    [Command]
    private void CmdChooseCard(string cardId)
    {
        cardInPlayID = cardId;
        DisplayPlayCard();
        RpcChooseCard(cardId);
    }

    [ClientRpc]
    private void RpcChooseCard(string cardId)
    {
        if (isServer) {return;}

        cardInPlayID = cardId;
        DisplayPlayCard();
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

        if (gameManager.trickNumber == 0)
        {
            // TODO: show the buttons for tricks
        }
        else
        {
            // TODO: show the field for choosing a card
        }
    }

    [ClientRpc]
    public void RpcTurnStart()
    {
        if (isServer) {return;}

        TurnStart();
    }

}
