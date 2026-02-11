using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class PlayerDisableMeshes : NetworkBehaviour
{
    [SerializeField] private GameObject meshRoot;
    [SerializeField] private GameObject personalCanvas;

    private void Start() 
    {
        if (isLocalPlayer) {
            meshRoot.SetActive(false);
            personalCanvas.SetActive(false);
        }
    }
}
