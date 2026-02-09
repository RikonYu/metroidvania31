using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camp : MonoBehaviour
{
    void Start()
    {
        GameController.instance.Camps.Add(this);
    }

    void Update()
    {
        
    }
}
