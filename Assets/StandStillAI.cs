using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StandStillAI : EnemyAI
{
    public int direction = 6;

    protected override void Start()
    {
        var spr = transform.Find("Sprite");
        var fs = transform.Find("firespot");
        base.Start();

        if (direction == 4)
        {
            spr.GetComponent<SpriteRenderer>().flipX = true;
            fs.localPosition = new Vector2(-fs.localPosition.x, fs.localPosition.y);
        }
        else if (direction == 2)
        {
            spr.rotation = Quaternion.Euler(0f, 0f, 270f);
            fs.localPosition = new Vector2(fs.localPosition.y, fs.localPosition.x);
        }
        else if (direction == 8)
        {
            spr.rotation = Quaternion.Euler(0f, 0f, 90f);
            fs.localPosition = new Vector2(fs.localPosition.y, -fs.localPosition.x);
        }
    }

    protected override void Update()
    {
        for (int i = StageNum - 1; i >= 0; i--)
        {
            if (controller.CurrentHP / controller.MaxHP <= Portion[i])
            {
                currentPhase = i;
                break;
            }
        }

        if (playerTransform == null) return;

        if (CanAttackPlayer())
        {
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                Attack();
                lastAttackTime = Time.time;
            }
        }
    }

    private bool CanAttackPlayer()
    {
        
        float distToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        if (distToPlayer > attackRange) return false;

        Vector2 dirToPlayer = (playerTransform.position - transform.position).normalized;
        Vector2 facingDir = GetFacingDirection();

        if (Vector2.Angle(facingDir, dirToPlayer) > 60f) return false;

        RaycastHit2D hit = Physics2D.Linecast(transform.position, playerTransform.position, obstacleMask);
        if (hit.collider != null) return false;

        return true;
    }

    private Vector2 GetFacingDirection()
    {
        switch (direction)
        {
            case 8: return Vector2.up;
            case 2: return Vector2.down;
            case 4: return Vector2.left;
            case 6: return Vector2.right;
            default: return Vector2.right;
        }
    }
}