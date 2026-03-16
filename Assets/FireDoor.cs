using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireDoor : MonoBehaviour
{
    public void Blast()
    {
        if (Shaker.instance != null)
        {
            Shaker.instance.ShakeSpecialDoorOpen();
        }
        Destroy(gameObject);
    }
}
