using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class PlayerSetup : NetworkBehaviour
{
    private NetworkManager manager;
    public readonly static List<PlayerSetup> playerList = new List<PlayerSetup>();
    private GameObject cam;

    private void Start()
    {
        playerList.Add(this);

        if (!isLocalPlayer)
        {
            this.gameObject.layer = 7;
        }
        else
        {
            manager = NetworkManager.singleton;
            cam = GameObject.FindGameObjectWithTag("MainCamera");
            GetComponent<PlayerInventory>().handText = cam.GetComponent<CameraSetup>().cardText;
            this.gameObject.layer = 6;
        }
    }

    private void OnDestroy() 
    {
        playerList.Remove(this);
    }
}
