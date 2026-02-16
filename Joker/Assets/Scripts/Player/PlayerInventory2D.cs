using System.Collections;
using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;

public class PlayerInventory2D : NetworkBehaviour
{

    public List<string> hand = new List<string>();
    [HideInInspector] public GameManager2D gameManager;

    [SerializeField] public GameObject cardPrefab;

    [SerializeField] private float cardSpacing = 1.0f;

    private List<GameObject> cardsInHand = new List<GameObject>();

    private void Start()
    {
        gameManager = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameManager2D>();
    }

    [Command]    
    public void CmdRemoveCard(string cardId)
    {
        // TODO not here but we eventually want to be able to hover over a face down trick and see what is in it
        hand.Remove(cardId);
        if (isLocalPlayer)
        {
            DisplayHand();
        }

        RpcRemoveCard(cardId);
    }

    [ClientRpc]
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
        
        
        int index = 0;
        foreach (string cardId in hand)
        {
            Card cardData = gameManager.deckDictionary[cardId];

            // int playerIndex = gameManager.playerOrder.IndexOf(GetComponent<Player2D>());
            // Transform ui = gameManager.playerUIPositions[playerIndex];
            Transform ui = gameManager.localPlayerHandPosition;

            GameObject card = Instantiate(cardPrefab, ui.position, ui.rotation);
            card.GetComponent<SpriteRenderer>().sprite = cardData.cardArt;
            
            // the name of the card is used when the player picks the card
            card.name = cardId;
            
            // space the cards
            card.transform.position = card.transform.position + card.transform.right * cardSpacing * index;
            index += 1;

            cardsInHand.Add(card);
        }
    }
}
