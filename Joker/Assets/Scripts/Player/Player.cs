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

    private GameObject cam;
    public GameManager gameManager;
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

    private void DisplayTricks()
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
        playCardText.text = "Played card: " + cardInPlay;
    }
    
    // should only get called by localplayer
    [ClientCallback]
    public void CalculateActions()
    {
        // just for safety
        if (isLocalPlayer)
        {
            // allow the player to play a card or choose tricks...
            if (gameManager.roundNumber == 0)
            {
                int trickAmount = cam.GetComponent<CameraSetup>().trickAmount;
                CmdChooseTricks(trickAmount);

                // TODO: hide the buttons for choosing tricks
            }
            else
            {
                string cardId = cam.GetComponent<CameraSetup>().playCardInput.text;

                // need to check that cardId is in the hand
                if (inventory.hand.Contains(cardId))
                {
                    CmdChooseCard(cardId);
                    inventory.CmdRemoveCard(cardId);
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
