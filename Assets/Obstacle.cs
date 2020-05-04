using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Obstacle : MonoBehaviour
{
    public GameManager gameManager;

    void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.name == "Car")
            gameManager.Crash();
    }

}
