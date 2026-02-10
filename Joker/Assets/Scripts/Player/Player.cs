using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class Player : NetworkBehaviour
{
    public bool playerTurn = false;

    [SyncVar] public int tricks;
    [SyncVar] public int tricksWon;
    [SyncVar] public string cardInPlay;

    private GameObject cam;
    public GameManager gameManager;
    private PlayerInventory inventory;

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
    }

    [Command]
    private void CmdChooseCard(string cardId)
    {
        // TODO:
        // should display the card for everyone else to see in an rpc call
        cardInPlay = cardId;
    }
    
    [ClientCallback]
    public void CalculateActions()
    {
        if (isLocalPlayer)
        {
            // allow the player to play a card or choose tricks...
            if (gameManager.roundNumber == 0)
            {
                int trickAmount = cam.GetComponent<CameraSetup>().trickAmount;
                CmdChooseTricks(trickAmount);
            }
            else
            {
                string cardId = cam.GetComponent<CameraSetup>().playCardText.text;

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
            }
        }

        CmdTurnEnd();
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
        // TODO:
        // add visuals when its a players turn
        playerTurn = true;
    }

    [ClientRpc]
    public void RpcTurnStart()
    {
        if (isServer) {return;}

        TurnStart();
    }
}
