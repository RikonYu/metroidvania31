using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlameBullet : Bullet
{
    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        base.OnTriggerEnter2D(collision);
        if (collision.gameObject.GetComponent<FireDoor>() != null)
        {
            collision.gameObject.GetComponent<FireDoor>().Blast();
            ReturnToPool();
        }
    }
}
