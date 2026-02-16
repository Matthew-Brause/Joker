using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardInteraction2D : MonoBehaviour
{
    private GameManager2D gameManager;

    private void Start()
    {
        gameManager = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameManager2D>();
    }
    

    private void OnMouseOver()
    {
        // TODO make hovering logic so light blue box or something around card
        if (Input.GetMouseButtonDown(0)) 
        {
            string cardId = this.transform.name;
            
            gameManager.localPlayer.selectedCard = cardId;
        }
    }
}
