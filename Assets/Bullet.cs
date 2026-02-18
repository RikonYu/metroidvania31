using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public bool IsEnemy;
    public float Speed;
    public float Damage;
    
    [HideInInspector]
    public string PoolKey;
    
    protected Vector2 dir;

    public virtual void Init(bool isenemy, Vector2 dir)
    {
        this.IsEnemy = isenemy;
        this.dir = dir.normalized;
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(this.dir.y, this.dir.x) * Mathf.Rad2Deg);
        
        if(isenemy)
            this.gameObject.layer = LayerMask.NameToLayer("EnemyBullet");
        else
            this.gameObject.layer = LayerMask.NameToLayer("MyBullet");
    }

    void Update()
    {
        transform.position += (Vector3)dir * Speed * Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        print(collision.gameObject.name);
        if(collision.gameObject.GetComponent<EnemyController>() != null)
        {
            collision.gameObject.GetComponent<EnemyController>().Hurt(this.Damage);

        }
        else if (collision.gameObject.GetComponent<MCController>() != null)
        {
            collision.gameObject.GetComponent<MCController>().Hurt(this.Damage);

        }

        ReturnToPool();
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