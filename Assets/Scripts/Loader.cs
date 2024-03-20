using UnityEngine;
using System.Collections;

// Code extracted from 2d roguelike unity project
public class Loader : MonoBehaviour
{
    public GameObject gameManager;

    void Awake()
    {

        if (GameManager.instance == null)
            Instantiate(gameManager);

    }
}