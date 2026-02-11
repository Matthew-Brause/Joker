using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class PlayerInteract : NetworkBehaviour
{
    [SerializeField] private float maxRaycastDistance = 100f;
    [SerializeField] private LayerMask interactLayerMask;
    private Transform cam;
    private Player player;


    private void Start()
    {
        cam = Camera.main.transform;
        player = GetComponent<Player>();
    }

    [ClientCallback]
    private void Update()
    {
        // this script should be disabled for other players but its just in case
        if (isLocalPlayer) {

            if (Input.GetMouseButtonDown(0)) 
            {
                TryInteract();
            }
        }
    }
    
    [ClientCallback]
    private void TryInteract()
    {
        RaycastHit hit;
        if (Physics.Raycast(cam.position, cam.forward, out hit, maxRaycastDistance, interactLayerMask))
        {
            if (hit.transform.gameObject.layer == 8)
            {
                // ray hit a button/interactable
                hit.collider.GetComponent<Interactable>().Activate();
            }
            else if (hit.transform.gameObject.layer == 9)
            {
                // ray hit a card
                string cardId = hit.transform.name;
                player.selectedCard = cardId;
            }
        }
    }
}
