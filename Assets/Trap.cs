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
    private Animator anim;
    private SpriteRenderer spriteRenderer;
    private float t;
    private bool isUp = true;
    private BoxCollider2D hurtbox;

    void Start()
    {
        hurtbox = gameObject.GetComponent<BoxCollider2D>();
        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        if (hurtbox != null && spriteRenderer != null)
        {
            hurtbox.size = spriteRenderer.size;
        }
        anim = gameObject.GetComponent<Animator>();
        ApplyFixedStateIfNeeded();
    }

    void Update()
    {
        if (ApplyFixedStateIfNeeded())
        {
            return;
        }

        t -= Time.deltaTime;
        if (t <= 0f)
        {
            isUp = !isUp;
            ApplyVisualState();
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

    public void SetCycleTimes(float upTime, float downTime)
    {
        UpTime = Mathf.Max(0f, upTime);
        DownTime = Mathf.Max(0f, downTime);
        ApplyFixedStateIfNeeded();
    }

    public void SetFixedState(bool up)
    {
        isUp = up;
        ApplyVisualState();
    }

    private bool ApplyFixedStateIfNeeded()
    {
        if (DownTime <= 1e-4f && UpTime > 1e-4f)
        {
            isUp = true;
            t = UpTime;
            ApplyVisualState();
            return true;
        }

        if (UpTime <= 1e-4f && DownTime > 1e-4f)
        {
            isUp = false;
            t = DownTime;
            ApplyVisualState();
            return true;
        }

        return false;
    }

    private void ApplyVisualState()
    {
        if (hurtbox != null)
        {
            hurtbox.enabled = isUp;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = isUp;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        MCController player = collision.gameObject.GetComponent<MCController>();

        if (player != null)
        {
            float healthBeforeHit = player.CurrentHealth;
            bool isDeadByThisHit = damage >= healthBeforeHit;
            bool isInBossRoom = GameController.instance != null
                && GameController.instance.ActiveRoom != null
                && GameController.instance.ActiveRoom.IsBossRoom;

            if (isDeadByThisHit && !isInBossRoom && GameController.instance != null)
            {
                // Non-boss trap death: keep pre-hit HP and respawn at last safe position.
                player.CurrentHealth = healthBeforeHit;
                UIController.instance.SetHP(player.CurrentHealth, player.MaxHealth);
                GameController.instance.Die(true);
                return;
            }

            player.ApplyDamageAndStun(damage, stunDuration);

            Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
            if (playerRb != null && !isDeadByThisHit)
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
