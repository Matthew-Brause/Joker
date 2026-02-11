using System.Collections;
using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;

public class PlayerInventory : NetworkBehaviour
{
    public List<string> hand = new List<string>();
    [HideInInspector] public GameManager gameManager;

    [SerializeField] public GameObject cardPrefab;

    private void Start()
    {
        gameManager = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameManager>();
    }


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
    public void ChangeHand(List<string> newHand)
    {
        hand = newHand;
        if (isLocalPlayer)
        {
            DisplayHand();
        }
    }

    [ClientRpc]
    public void RpcChangeHand(List<string> newHand)
    {
        if (isServer) {return;}

        hand = newHand;
        if (isLocalPlayer)
        {
            DisplayHand();
        }
    }

    private List<GameObject> cardsInHand = new List<GameObject>();

    [ClientCallback]
    private void DisplayHand()
    {
        // clear the previous cards
        if (cardsInHand.Count > 0)
        {
            foreach (GameObject card in cardsInHand)
            {
                Destroy(card);
            }
        }
        cardsInHand = new List<GameObject>();
        
        float spacing = 0.25f;
        int index = 0;
        foreach (string cardId in hand)
        {
            Card cardData = gameManager.deckDictionary[cardId];

            int playerIndex = gameManager.playerOrder.IndexOf(GetComponent<PlayerSetup>());
            Transform ui = gameManager.playerUIPositions[playerIndex];

            GameObject card = Instantiate(cardPrefab, ui.position, ui.rotation);
            card.GetComponent<SpriteRenderer>().sprite = cardData.cardArt;
            
            // the name of the card is used when the player picks the card
            card.name = cardId;
            
            // space the cards
            card.transform.position = card.transform.position - card.transform.right * spacing * index;
            index += 1;

            cardsInHand.Add(card);
        }
    }
}
