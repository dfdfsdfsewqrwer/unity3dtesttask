using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Killzone : MonoBehaviour
{
    public GameManager gameManager;

    void OnCollisionEnter(Collision col)
    {
        gameManager.GameOver();
    }

}
