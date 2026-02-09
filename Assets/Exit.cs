using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Exit : MonoBehaviour
{
    public Room TargetRoom;
    public GameObject TargetEntrance;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        GameController.instance.ActivateRoom(TargetRoom);
        GameController.instance.mc.transform.position = TargetEntrance.transform.position;
    }
}
