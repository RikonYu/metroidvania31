using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    public bool IsEnemy;
    public float Speed;
    public float Damage;
    public float CoolDown;
    public int MaxBounceCount = 3;
    public float MinSpeedThreshold = 1.0f;

    [HideInInspector]
    public string PoolKey;
    protected Vector2 dir;

    private int currentBounces;
    private int groundLayerMask;
    private Rigidbody2D rb2d;
    private Vector2 velocity;

    public virtual void Init(bool isenemy, Vector2 dir)
    {
        this.IsEnemy = isenemy;
        this.dir = dir.normalized;
        currentBounces = 0;
        
        rb2d = GetComponent<Rigidbody2D>();
        velocity = this.dir * Speed;
        
        UpdateRotation();

        if (isenemy)
            this.gameObject.layer = LayerMask.NameToLayer("EnemyBullet");
        else
            this.gameObject.layer = LayerMask.NameToLayer("MyBullet");

        groundLayerMask = 1 << LayerMask.NameToLayer("Ground");
    }

    void Update()
    {
        if (rb2d != null && rb2d.gravityScale > 0)
        {
            velocity += Physics2D.gravity * rb2d.gravityScale * Time.deltaTime;
            dir = velocity.normalized;
            Speed = velocity.magnitude;
            UpdateRotation();
        }

        float stepDistance = Speed * Time.deltaTime;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, stepDistance, groundLayerMask);

        if (hit.collider != null)
        {
            bool canBounce = false;

            if (rb2d != null && rb2d.gravityScale == 0)
            {
                if (currentBounces < MaxBounceCount)
                {
                    canBounce = true;
                    currentBounces++;
                }
            }
            else
            {
                if (Speed * 0.5f >= MinSpeedThreshold)
                {
                    canBounce = true;
                }
            }

            if (canBounce)
            {
                dir = Vector2.Reflect(dir, hit.normal);
                Speed *= 0.5f;
                velocity = dir * Speed;
                UpdateRotation();
                transform.position = hit.point + hit.normal * 0.05f;
                return;
            }
            else
            {
                ReturnToPool();
                return;
            }
        }

        transform.position += (Vector3)(dir * stepDistance);
    }

    private void UpdateRotation()
    {
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        EnemyController enemy = collision.gameObject.GetComponent<EnemyController>();
        if (enemy != null)
        {
            enemy.Hurt(this.Damage);
            ReturnToPool();
            return;
        }

        MCController mc = collision.gameObject.GetComponent<MCController>();
        if (mc != null)
        {
            mc.Hurt(this.Damage);
            ReturnToPool();
            return;
        }
    }

    public void ReturnToPool()
    {
        if (GameController.instance != null)
        {
            GameController.instance.ReturnBullet(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}