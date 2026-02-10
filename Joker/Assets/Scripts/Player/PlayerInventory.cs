using System.Collections;
using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;

public class PlayerInventory : NetworkBehaviour
{
    // this script will store the current cards the player has
    public List<string> hand = new List<string>();
    [HideInInspector] public TextMeshProUGUI handText;

    public void ChangeHand(List<string> newHand)
    {
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
