using System.Collections;
using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;

public class PlayerInventory : NetworkBehaviour
{
    public List<string> hand = new List<string>();
    [HideInInspector] public TextMeshProUGUI handText;


    [Command]    
    public void CmdRemoveCard(string cardId)
    {
        hand.Remove(cardId);
        if (isLocalPlayer)
        {
            DisplayHand();
        }

        RpcRemoveCard(cardId);
    }

    public void RpcRemoveCard(string cardId)
    {
        if (isServer) {return;}
        
        hand.Remove(cardId);
        if (isLocalPlayer)
        {
            DisplayHand();
        }
    }

    [ServerCallback]
    public void ChangeWholeHand(List<string> newHand)
    {
        hand = newHand;
        if (isLocalPlayer)
        {
            DisplayHand();
        }
    }

    [ClientRpc]
    public void RpcChangeWholeHand(List<string> newHand)
    {
        if (isServer) {return;}

        hand = newHand;
        if (isLocalPlayer)
        {
            DisplayHand();
        }
    }

    [ClientCallback]
    private void DisplayHand()
    {
        string handString = "";
        foreach (string card in hand)
        {
            handString += card;
        }
        handText.text = handString;
    }
}
