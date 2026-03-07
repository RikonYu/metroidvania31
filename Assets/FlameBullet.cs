using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlameBullet : Bullet
{
    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        //base.OnTriggerEnter2D(collision);
        print(collision.gameObject.GetComponent<FireDoor>());
        if (collision.gameObject.GetComponent<FireDoor>() != null)
        {
            collision.gameObject.GetComponent<FireDoor>().Blast();
            ReturnToPool();
        }
    }
}
