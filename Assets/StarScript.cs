using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StarScript : MonoBehaviour
{
    public GameManager gameManager;

    // handle everything in GameManager for convenience
    void OnMouseOver()
    {
        gameManager.OnStarMouseOver();
    }

    void OnMouseExit()
    {
        gameManager.OnStarMouseExit();
    }

    void OnCollisionEnter(Collision col)
    {
        gameManager.GameOver();
    }

}
