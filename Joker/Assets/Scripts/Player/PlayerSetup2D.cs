using System.Collections;
using System.Collections.Generic;
using KinematicCharacterController;
using KinematicCharacterController.Examples;
using Mirror;
using UnityEngine;

public class PlayerSetup2D : NetworkBehaviour
{
    public readonly static List<Player2D> playerList = new List<Player2D>();
    [HideInInspector] public GameManager2D gameManager;
    public Transform playerUICanvas;

    // Start is called before the first frame update
    void Start()
    {
        playerList.Add(GetComponent<Player2D>());

        if (!isLocalPlayer)
        {
            //GetComponent<PlayerInteract>().enabled = false;
            this.gameObject.layer = 7;
        }
        else
        {
            gameManager = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameManager2D>();
            gameManager.localPlayer = GetComponent<Player2D>();
            this.gameObject.layer = 6;
        }
    }

    // Update is called once per frame
    private void OnDestroy() 
    {
        playerList.Remove(GetComponent<Player2D>());
    }
}
