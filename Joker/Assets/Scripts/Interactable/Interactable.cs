using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum InteractableTypes
{
    StartGameButton,
    EndRoundButton,
    AddTrickButton,
    RemoveTrickButton
}

public class Interactable : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private InteractableTypes interactableType;


    // TODO: make button animation
    
    public void Activate()
    {
        if (interactableType == InteractableTypes.StartGameButton)
        {
            gameManager.StartGame();
        }
        else if (interactableType == InteractableTypes.EndRoundButton)
        {
            gameManager.EndPlayerTurn();
        }
        else if (interactableType == InteractableTypes.AddTrickButton)
        {
            gameManager.localPlayer.tricks += 1;
            gameManager.localPlayer.DisplayTricks();
        }
        else if (interactableType == InteractableTypes.RemoveTrickButton)
        {
            gameManager.localPlayer.tricks -= 1;
            gameManager.localPlayer.DisplayTricks();
        }
    }
}
