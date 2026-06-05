using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using DG.Tweening;
using UnityEngine;

public class CardInteraction2D : MonoBehaviour
{
    private GameManager2D gameManager;
    [SerializeField] private GameObject highlight;
    [SerializeField] private GameObject jokerButtons;
    private string selectedCardId;
    public bool isLocalPlayersCard = false;

    // TODO: BIG THING, make all cards buttons/UI instead of physical objects
    // or not ??? Amandin is still not sure whether this is smart

    private void Start()
    {
        gameManager = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameManager2D>();
        selectedCardId = this.transform.name;
        ShowJokerButtons(false);
        highlight.SetActive(false);
    }

    private void OnMouseEnter()
    {
        // TODO make hovering logic so light blue box or something around card
        if (isLocalPlayersCard)
        {
            this.transform.DOScale(Vector3.one * 1.25f, 0.15f);
        }
    }

    private void OnMouseExit()
    {
        if (isLocalPlayersCard)
        {
            this.transform.DOScale(Vector3.one, 0.15f);
        }
    }

    private void OnMouseOver()
    {
        // we selected this card
        if (Input.GetMouseButtonDown(0) && gameManager.trickNumber != 0) 
        {
            if (selectedCardId[3] == 'j')
            {
                // reset joker defaults
                selectedCardId = this.transform.name + "hs";
            }
            TrySelectCard();
        }
    }

    private void TrySelectCard()
    {
        gameManager.localPlayer.SetSelectedCard(selectedCardId, this);
    }

    public void SelectCard()
    {
        if (selectedCardId[3] == 'j')
        {
            ShowJokerButtons(true);
        }
        highlight.SetActive(true);
    }

    public void UnSelectCard()
    {
        if (selectedCardId[3] == 'j')
        {
            ShowJokerButtons(false);
        }
        highlight.SetActive(false);
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
        TrySelectCard();
    }

    public void SetJokerSuit(string suit)
    {
        // TODO have a way of signalling to player what they have selected
        selectedCardId = selectedCardId.Substring(0,5) + suit;

        TrySelectCard();
    }
}
