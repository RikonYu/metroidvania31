using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IceBullet : Bullet
{
    public float FreezeTime;
    
    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        if (IsEnemy)
        {
            MCController mc = collision.gameObject.GetComponent<MCController>();
            if (mc != null)
            {
                mc.Hurt(this.Damage);
                mc.Freeze(this.FreezeTime);
                ReturnToPool();
                return;
            }
        }
        else
        {
            EnemyController enemy = collision.gameObject.GetComponent<EnemyController>();
            if (enemy != null)
            {
                enemy.Hurt(this.Damage);
                if(!enemy.IsBoss) // does not freeze bosses
                enemy.Freeze(this.FreezeTime);
                ReturnToPool();
                return;
            }
            else
            {
                Platform pf = collision.gameObject.GetComponent<Platform>();
                if (pf != null && pf.IsFreezeable)
                {
                    pf.Freeze(this.FreezeTime);
                }
            }
        }
    }
}
