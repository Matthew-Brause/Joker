using System.Collections;
using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;

public class Player : NetworkBehaviour
{
    public bool playerTurn = false;

    public int tricks;
    [SyncVar] public int tricksWon;
    public string cardInPlay;
    public string selectedCard;

    private GameObject cam;
    [HideInInspector] public GameManager gameManager;
    private PlayerInventory inventory;

    [SerializeField] private TextMeshProUGUI playCardText;
    [SerializeField] private TextMeshProUGUI trickText;

    private void Start()
    {
        cam = GameObject.FindGameObjectWithTag("MainCamera");
        gameManager = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameManager>();
        inventory = GetComponent<PlayerInventory>();
    }

    [Command]
    private void CmdChooseTricks(int trickAmount)
    {
        tricks = trickAmount;
        DisplayTricks();
        RpcChooseTricks(trickAmount);
    }

    [ClientRpc]
    private void RpcChooseTricks(int trickAmount)
    {
        if (isServer) {return;}

        tricks = trickAmount;
        DisplayTricks();
    }

    public void DisplayTricks()
    {
        trickText.text = "Tricks: " + tricks.ToString();
    }

    [Command]
    private void CmdChooseCard(string cardId)
    {
        cardInPlay = cardId;
        DisplayPlayCard();
        RpcChooseCard(cardId);
    }

    [ClientRpc]
    private void RpcChooseCard(string cardId)
    {
        if (isServer) {return;}

        cardInPlay = cardId;
        DisplayPlayCard();
    }

    private void DisplayPlayCard()
    {
        Card cardData = gameManager.deckDictionary[cardInPlay];

        int playerIndex = gameManager.playerOrder.IndexOf(GetComponent<PlayerSetup>());
        Transform ui = gameManager.playedCardPositions[playerIndex];

        GameObject cardPrefab = GetComponent<PlayerInventory>().cardPrefab;
        GameObject card = Instantiate(cardPrefab, ui.position, ui.rotation);
        card.GetComponent<SpriteRenderer>().sprite = cardData.cardArt;
        
        // the name of the card is used when the player interacts with a card
        card.name = cardInPlay;
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
            if (gameManager.roundNumber == 0)
            {
                // tricks was changed for the localplayer by buttons
                CmdChooseTricks(tricks);

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

        if (gameManager.roundNumber == 0)
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
