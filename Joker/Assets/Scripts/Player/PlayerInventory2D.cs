using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Mirror;
using Mirror.BouncyCastle.Math.Field;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Splines;

public class PlayerInventory2D : NetworkBehaviour
{

    public List<string> hand = new List<string>();
    [HideInInspector] public GameManager2D gameManager;

    [SerializeField] public GameObject cardPrefab;
    [SerializeField] private int maxHandSize;

    public List<GameObject> cardsInHand = new List<GameObject>();

    private void Start()
    {
        gameManager = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameManager2D>();
    }

    [ClientCallback]
    private void DisplayHand(bool newDeal)
    {
        // TODO: order the cards so that it looks nice
        
        // clear the previous cards
        if (cardsInHand.Count > 0)
        {
            foreach (GameObject card in cardsInHand)
            {
                Destroy(card);
            }
        }
        cardsInHand = new List<GameObject>();
        
        // spawn the new cards
        // TODO: its important that hand and cardsInHand have the same ordering, fix this?
        int cardIndex = 0;
        foreach (string cardId in hand)
        {
            SpawnCard(cardId, cardIndex, newDeal);
            if (newDeal)
            {
                UpdateCardPosition(cardIndex);
            }

            cardIndex += 1;
        }
    }

    private void SpawnCard(string cardId, int cardIndex, bool newDeal)
    {
        Card cardData = gameManager.deckDictionary[cardId];

        GameObject card = null;
        if (newDeal)
        {
            card = Instantiate(cardPrefab, gameManager.cardSpawnPoint.position, gameManager.cardSpawnPoint.rotation);

            // shrink card to zero so we can animate it growing
            card.transform.localScale = Vector3.zero;
        }
        else
        {
            // calculate where to place the card if it was already in our hand
            float cardSpacing = 1f / maxHandSize;
            float firstCardPosition = 0.5f - (hand.Count - 1) * cardSpacing / 2;
            Spline spline = gameManager.cardSplineContainer.Spline;

            float p = firstCardPosition + cardIndex * cardSpacing;
            Vector3 splinePosition = spline.EvaluatePosition(p);
            Vector3 forward = spline.EvaluateTangent(p);
            Vector3 up = spline.EvaluateUpVector(p);
            Quaternion rotation = Quaternion.LookRotation(up, Vector3.Cross(up, forward).normalized);

            //card has already been dealt so there won't be any animation
            card = Instantiate(cardPrefab, splinePosition, rotation);
        }


        // this is required for hand specific animations
        if (isLocalPlayer)
        {
            card.GetComponent<CardInteraction2D>().isLocalPlayersCard = true;
        }
        card.GetComponent<SpriteRenderer>().sprite = cardData.cardArt;
        
        // the name of the card is used when the player picks the card
        card.name = cardId;

        cardsInHand.Add(card);
    }

    private void UpdateCardPosition(int cardIndex)
    {
        float cardSpacing = 1f / maxHandSize;
        float firstCardPosition = 0.5f - (hand.Count - 1) * cardSpacing / 2;
        Spline spline = gameManager.cardSplineContainer.Spline;

        float p = firstCardPosition + cardIndex * cardSpacing;
        Vector3 splinePosition = spline.EvaluatePosition(p);
        Vector3 forward = spline.EvaluateTangent(p);
        Vector3 up = spline.EvaluateUpVector(p);
        Quaternion rotation = Quaternion.LookRotation(up, Vector3.Cross(up, forward).normalized);

        // card animation
        cardsInHand[cardIndex].transform.DOScale(Vector3.one, 0.25f);
        cardsInHand[cardIndex].transform.DOMove(splinePosition, 0.25f);
        cardsInHand[cardIndex].transform.DOLocalRotateQuaternion(rotation, 0.25f);
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
            DisplayHand(false);
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
            DisplayHand(false);
        }
    }

    [ServerCallback]
    public void ChangeHand(List<string> newHand)
    {
        hand = newHand;
        if (isLocalPlayer)
        {
            DisplayHand(true);
        }
    }

    [ClientRpc]
    public void RpcChangeHand(List<string> newHand)
    {
        if (isServer) {return;}

        hand = newHand;
        if (isLocalPlayer)
        {
            DisplayHand(true);
        }
    }

    [ServerCallback]
    public void AddToHand(List<string> cards)
    {
        foreach (string card in cards)
        {
            hand.Add(card);
        }
        if (isLocalPlayer)
        {
            DisplayHand(true);
        }
    }

    [ClientRpc]
    public void RpcAddToHand(List<string> cards)
    {
        if (isServer) {return;}

        foreach (string card in cards)
        {
            hand.Add(card);
        }
        if (isLocalPlayer)
        {
            DisplayHand(true);
        }
    }
}
