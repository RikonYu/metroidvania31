using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trap : MonoBehaviour
{
    public float UpTime;
    public float DownTime;



    [Header("Trap Effects")]
    public float damage = 10f;
    public float stunDuration = 0.5f;
    public float knockbackForce = 15f;
    Animator anim;
    float t;
    bool isUp = true;
    BoxCollider2D hurtbox;

    void Start()
    {
        hurtbox = gameObject.GetComponent<BoxCollider2D>();
        hurtbox.size = gameObject.GetComponent<SpriteRenderer>().size;
        anim = gameObject.GetComponent<Animator>();
    }

    void Update()
    {
        t -= Time.deltaTime;
        if (DownTime <= 1e-4f) return;
        if (t <= 0f)
        {
            isUp = !isUp;
            hurtbox.enabled = isUp;
            gameObject.GetComponent<SpriteRenderer>().enabled = isUp;
            if (isUp)
            {
                t = UpTime;
                anim.Play("trap", 0, 0f);
                anim.SetFloat("Speed", 1f);
            }

            else
            {
                anim.Play("trap", 0, 1f);
                anim.SetFloat("Speed", -1f);
                t = DownTime;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        MCController player = collision.gameObject.GetComponent<MCController>();

        if (player != null)
        {
            bool isdead = damage >= player.CurrentHealth;
            player.ApplyDamageAndStun(damage, stunDuration);

            Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
            if (playerRb != null && !isdead)
            {
                Vector2 direction = (collision.transform.position - transform.position).normalized;

                if (direction.y <= 0.2f)
                {
                    direction.y = 0.8f;
                }

                direction = direction.normalized;
                playerRb.velocity = Vector2.zero;
                playerRb.AddForce(direction * knockbackForce, ForceMode2D.Impulse);
            }
        }
    }
}