using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CameraSetup : MonoBehaviour
{
    // this script is needed to pass references between things in 
    // the scene and the players.
    // also im doing a bunch of the UI stuff in here 
    // (should maybe move endturn button here).

    public TextMeshProUGUI deckText;
    public int trickAmount;
    public TextMeshProUGUI trickText;
    public TMP_InputField playCardText;

    private void Start()
    {
        Application.targetFrameRate = 144;

        trickAmount = 0;
    }

    public void AddTrick()
    {
        trickAmount += 1;
        trickText.text = trickAmount.ToString();
    }

    public void RemoveTrick()
    {
        trickAmount -= 1;
        trickText.text = trickAmount.ToString();
    }
}
