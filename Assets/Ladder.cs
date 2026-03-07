using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Ladder : MonoBehaviour
{
    private void Start()
    {
    }
    public float GetCenterX()
    {
        return GetComponent<Collider2D>().bounds.center.x;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("enter");
        MCController mc = collision.GetComponent<MCController>();
        if (mc != null)
        {
            mc.SetCanClimb(true, this);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        Debug.Log("exit");
        MCController mc = collision.GetComponent<MCController>();
        if (mc != null)
        {
            mc.HandleLadderExit(this);
        }
    }
}