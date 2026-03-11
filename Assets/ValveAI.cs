using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ValveAI : StandStillAI
{
    protected override void Attack()
    {
        var firepos = transform.Find("firespot").position;
        Vector2 fw = Vector2.zero;
        if (direction == 2) fw = Vector2.down;
        else if (direction == 4) fw = Vector2.left;
        else if (direction == 6) fw = Vector2.right;
        else if (direction == 8) fw = Vector2.up;
        Debug.Log("firing");
        GameController.instance.FireBullet(Bullet, firepos, fw, true);
    }
}
