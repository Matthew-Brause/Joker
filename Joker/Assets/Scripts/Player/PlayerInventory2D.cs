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

    private void Start()
    {
        gameManager = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameManager2D>();
    }


    [ServerCallback]
    public void ChangeHand(List<string> newHand)
    {
        hand = newHand;
        // if (isLocalPlayer)
        // {
        //     DisplayHand();
        // }
    }

    [ClientRpc]
    public void RpcChangeHand(List<string> newHand)
    {
        if (isServer) {return;}

        hand = newHand;
        // if (isLocalPlayer)
        // {
        //     DisplayHand();
        // }
    }
}
