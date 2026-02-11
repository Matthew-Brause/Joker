using System.Collections;
using System.Collections.Generic;
using KinematicCharacterController;
using KinematicCharacterController.Examples;
using Mirror;
using UnityEngine;

public class PlayerSetup : NetworkBehaviour
{
    private NetworkManager networkManager;
    public readonly static List<PlayerSetup> playerList = new List<PlayerSetup>();
    private GameObject cam;

    [HideInInspector] public GameManager gameManager;

    public bool canMove = false;

    private void Start()
    {
        playerList.Add(this);

        if (!isLocalPlayer)
        {
            GetComponent<KinematicCharacterMotor>().enabled = false;
            GetComponent<ExampleCharacterController>().enabled = false;
            this.gameObject.layer = 7;
        }
        else
        {
            GetComponent<KinematicCharacterMotor>().enabled = canMove;
            GetComponent<ExampleCharacterController>().enabled = true;
            networkManager = NetworkManager.singleton;
            cam = GameObject.FindGameObjectWithTag("MainCamera");
            gameManager = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameManager>();

            gameManager.localPlayer = GetComponent<Player>();
            GetComponent<PlayerInventory>().handText = cam.GetComponent<CameraSetup>().deckText;
            this.gameObject.layer = 6;
        }
    }

    private void OnDestroy() 
    {
        playerList.Remove(this);
    }
}
