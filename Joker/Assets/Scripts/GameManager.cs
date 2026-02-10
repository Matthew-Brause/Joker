using System.Collections;
using System.Collections.Generic;
using Mirror;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public List<string> fixedDeck = new List<string>();
    public List<string> playingDeck;
    private List<PlayerSetup> players;

    public int cardsPerPlayer = 2;

    private void Start()
    {
        GenerateDeck();
    }

    private void GenerateDeck()
    {
        // could generate the list of strings for the deck here
    }

    [ServerCallback]
    public void StartGame()
    {
        if (isServer)
        {
            playingDeck = new List<string>(fixedDeck);
            players = PlayerSetup.playerList;

            // deal the hands
            foreach (PlayerSetup player in players)
            {
                List<string> tempHand = new List<string>();
                for (int i = 0; i < cardsPerPlayer; i++)
                {
                    int cardIndex = Random.Range(1,playingDeck.Count);
                    tempHand.Add(playingDeck[cardIndex]);
                    playingDeck.RemoveAt(cardIndex);
                }

                DealHand(player.gameObject, tempHand);
                RpcDealHand(player.gameObject, tempHand);
            }
        }
    }

    private void DealHand(GameObject player, List<string> hand)
    {
        player.GetComponent<PlayerInventory>().ChangeHand(hand);
    }

    [ClientRpc]
    private void RpcDealHand(GameObject player, List<string> hand)
    {
        // if we are the host then we already dealt the hand
        if (isServer) {return;}

        DealHand(player, hand);
    }
}
