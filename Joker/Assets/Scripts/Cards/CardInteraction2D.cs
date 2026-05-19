using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

public class CardInteraction2D : MonoBehaviour
{
    private GameManager2D gameManager;
    [SerializeField] private GameObject highlight;
    [SerializeField] private GameObject jokerButtons;
    private string selectedCardId;

    private void Start()
    {
        gameManager = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameManager2D>();
        SetHighlightCard(false);
        ShowJokerButtons(false);
        selectedCardId = this.transform.name;

        // default jokers to high and spades
        if (selectedCardId[3] == 'j')
        {
            selectedCardId = selectedCardId + "hs";
            ShowJokerButtons(true);
            // TODO: only show when card is selected (player needs to tell the card when its no longer selected)
        }
    }
    

    private void OnMouseOver()
    {
        // TODO make hovering logic so light blue box or something around card
        if (Input.GetMouseButtonDown(0)) 
        {
            gameManager.localPlayer.SetSelectedCard(selectedCardId, this);
        }
    }

    public void SetHighlightCard(bool enable)
    {
        highlight.SetActive(enable);
    }

    public void ShowJokerButtons(bool enable)
    {
        jokerButtons.SetActive(enable);
    }

    public void SetJokerHigh(bool status)
    {
        // TODO have a way of signalling to player what they have selected
        if (status)
        {
            selectedCardId = selectedCardId.Substring(0,4) + 'h' + selectedCardId[5];
        }
        else
        {
            selectedCardId = selectedCardId.Substring(0,4) + 'l' + selectedCardId[5];
        }
        gameManager.localPlayer.SetSelectedCard(selectedCardId, this);
    }

    public void SetJokerSuit(string suit)
    {
        // TODO have a way of signalling to player what they have selected
        selectedCardId = selectedCardId.Substring(0,5) + suit;

        gameManager.localPlayer.SetSelectedCard(selectedCardId, this);
    }
}
