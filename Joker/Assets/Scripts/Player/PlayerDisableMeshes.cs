using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class PlayerDisableMeshes : NetworkBehaviour
{
    [SerializeField] private GameObject meshRoot;
    [SerializeField] private GameObject steamCanvas;

    private void Start() 
    {
        if (isLocalPlayer) {
            meshRoot.SetActive(false);
            steamCanvas.SetActive(false);
        }
    }
}
