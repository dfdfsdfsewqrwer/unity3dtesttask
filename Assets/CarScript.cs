using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarScript : MonoBehaviour
{
    public GameManager gameManager;

    // handle everything in GameManager for convenience
    void OnMouseOver()
    {
        gameManager.OnCarMouseOver();
    }

    void OnMouseExit()
    {
        gameManager.OnCarMouseExit();
    }

    void OnMouseDown()
    {
        gameManager.OnCarMouseDown();
    }

    void OnMouseUp()
    {
        gameManager.OnCarMouseUp();
    }

}
