using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class PlayerSetup : NetworkBehaviour
{
    private NetworkManager networkManager;
    public readonly static List<PlayerSetup> playerList = new List<PlayerSetup>();
    private GameObject cam;

    public GameManager gameManager;

    private void Start()
    {
        playerList.Add(this);

        if (!isLocalPlayer)
        {
            this.gameObject.layer = 7;
        }
        else
        {
            networkManager = NetworkManager.singleton;
            cam = GameObject.FindGameObjectWithTag("MainCamera");
            gameManager = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameManager>();

            gameManager.localPlayer = GetComponent<Player>();
            GetComponent<PlayerInventory>().handText = cam.GetComponent<CameraSetup>().cardText;
            this.gameObject.layer = 6;
        }
    }

    private void OnDestroy() 
    {
        playerList.Remove(this);
    }
}
