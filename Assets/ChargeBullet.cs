using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChargeBullet : Bullet
{
    public float MaxChargeTime = 2f;
    public static float NextChargeRatio;

    public override void Init(bool isenemy, Vector2 dir)
    {
        this.Damage = this.Damage * NextChargeRatio;
        base.Init(isenemy, dir);
    }

    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log(collision.gameObject.GetComponent<ChargeDoor>());
        base.OnTriggerEnter2D(collision);
        if (collision.gameObject.GetComponent<ChargeDoor>() != null)
        {
            collision.gameObject.GetComponent<ChargeDoor>().Blast();
            ReturnToPool();
        }
    }
}