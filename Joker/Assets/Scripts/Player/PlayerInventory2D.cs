using System.Collections;
using System.Collections.Generic;
using Mirror;
using Mirror.BouncyCastle.Math.Field;
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

    [ClientCallback]
    public List<string> getValidCards()
    {
        List<string> validCards = new List<string>();

        // specifically handling if high joker was lead card
        if (gameManager.initialCardId[3] == 'j')
        {
            if (gameManager.initialCardId[4] == 'h')
            {
                bool hasSuit = false;
                string bestIdOfSuit = "";
                int bestValOfSuit = -1;
                // check if hand can follow suit
                foreach (string cardId in hand)
                {
                    if (cardId[3] == gameManager.initialCardSuit)
                    {
                        hasSuit = true;
                        int cardValue = int.Parse(cardId.Substring(0,2));
                        if (cardValue > bestValOfSuit)
                        {
                            bestIdOfSuit = cardId;
                            bestValOfSuit = cardValue;
                        }
                    }
                    else if (cardId[3] == 'j')
                    {
                        validCards.Add(cardId);
                    }
                }


                if (hasSuit)
                {
                    if (bestIdOfSuit == "")
                    {
                        Debug.Log("Error: Best card of suit is still empty somehow");
                    }
                    validCards.Add(bestIdOfSuit);
                    return validCards;
                }
                else
                {
                    return hand;
                }
            }
        }

        bool canFollow = false;
        // check if hand can follow suit
        foreach (string cardId in hand)
        {
            if (cardId[3] == gameManager.initialCardSuit)
            {
                canFollow = true;
                validCards.Add(cardId);
            }
            else if (cardId[3] == 'j')
            {
                validCards.Add(cardId);
            }
        }
        if (canFollow)
        {
            return validCards;
        }

        // must trump because you can't follow suit
        if (gameManager.trumpCardId != null)
        {
            bool canTrump = false;
            // check if hand can trump
            foreach (string cardId in hand)
            {
                if (cardId[3] == gameManager.trumpCardId[3])
                {
                    canTrump = true;
                    validCards.Add(cardId);
                }
            }
            if (canTrump)
            {
                return validCards;
            }
        }

        // can't follow and no trumps so entire hand is valid
        return hand;
    }

    [Command]    
    public void CmdRemoveCard(string cardId)
    {
        // TODO: not here but we eventually want to be able to hover over a face down trick and see what is in it
        hand.Remove(cardId.Substring(0,4));
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
        
        hand.Remove(cardId.Substring(0,4));
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
}
