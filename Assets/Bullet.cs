using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public bool IsEnemy;
    public float Speed;
    public float Damage;
    Vector2 dir;
    // Start is called before the first frame update
    void Start()
    {
        
    }
    public void Init(bool isenemy, Vector2 dir)
    {
        this.IsEnemy = isenemy;
        dir = dir.normalized;
        this.dir = dir;
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        if(isenemy)
            this.gameObject.layer = LayerMask.NameToLayer("EnemyBullet");
        else
            this.gameObject.layer = LayerMask.NameToLayer("MyBullet");
    }

    // Update is called once per frame
    void Update()
    {
        transform.position += (Vector3) dir * Speed * Time.deltaTime;
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.GetComponent<EnemyController>()!=null)
        {
            collision.gameObject.GetComponent<EnemyController>().Hurt(this.Damage);
        }
        else if (collision.gameObject.GetComponent<MCController>() != null)
        {
            collision.gameObject.GetComponent<MCController>().Hurt(this.Damage);
        }
        Destroy(gameObject);
    }
}
