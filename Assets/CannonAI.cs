using UnityEngine;

public class CannonAI : EnemyAI
{
    public int direction = 6;
    public float uptime = 2f;
    public float downtime = 1f;

    private SpriteRenderer spriteRenderer;
    private Transform spriteTransform;
    private Transform fireSpot;
    private LaserBullet activeLaser;
    private float cycleTimer;
    private bool waitingForDowntime;
    private float forcedFireTimer;

    protected override void Start()
    {
        base.Start();

        spriteTransform = transform.Find("Sprite");
        if (spriteTransform != null)
        {
            spriteRenderer = spriteTransform.GetComponent<SpriteRenderer>();
        }

        fireSpot = transform.Find("firespot");
        ApplyDirectionVisuals();
    }

    protected override void Update()
    {
        if (IsCombatPaused())
        {
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }
            StopLaser(false);
            return;
        }

        UpdateCurrentPhase();

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }

        if (activeLaser != null && !activeLaser.gameObject.activeInHierarchy)
        {
            activeLaser = null;
        }

        if (forcedFireTimer > 0f)
        {
            forcedFireTimer -= Time.deltaTime;

            if (activeLaser == null)
            {
                FireLaser();
            }

            if (activeLaser != null)
            {
                activeLaser.SetDuration(0f);
            }

            if (forcedFireTimer <= 0f)
            {
                forcedFireTimer = 0f;

                if (downtime > 0f)
                {
                    StopLaser(false);
                    waitingForDowntime = true;
                    cycleTimer = Mathf.Max(0.01f, downtime);
                }
            }

            return;
        }

        if (downtime <= 0f)
        {
            if (activeLaser == null)
            {
                FireLaser();
            }
            return;
        }

        if (activeLaser == null)
        {
            if (waitingForDowntime)
            {
                cycleTimer -= Time.deltaTime;
                if (cycleTimer <= 0f)
                {
                    waitingForDowntime = false;
                }
            }

            if (!waitingForDowntime)
            {
                FireLaser();
                cycleTimer = Mathf.Max(0.01f, uptime);
            }

            return;
        }

        cycleTimer -= Time.deltaTime;
        if (cycleTimer <= 0f)
        {
            StopLaser(false);
            waitingForDowntime = true;
            cycleTimer = Mathf.Max(0.01f, downtime);
        }
    }

    protected override void Attack()
    {
    }

    private void OnDisable()
    {
        StopLaser(true);
    }

    private void ApplyDirectionVisuals()
    {
        if (direction != 2 && direction != 4 && direction != 6 && direction != 8)
        {
            direction = 6;
        }

        if (spriteTransform == null || fireSpot == null)
        {
            return;
        }

        fireSpot.localRotation = Quaternion.identity;
        spriteTransform.localRotation = Quaternion.identity;

        Vector3 fireLocalPos = fireSpot.localPosition;
        float absX = Mathf.Abs(fireLocalPos.x);
        float absY = Mathf.Abs(fireLocalPos.y);

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = false;
        }

        switch (direction)
        {
            case 4:
                if (spriteRenderer != null) spriteRenderer.flipX = true;
                fireSpot.localPosition = new Vector3(-absX, fireLocalPos.y, fireLocalPos.z);
                break;
            case 6:
                fireSpot.localPosition = new Vector3(absX, fireLocalPos.y, fireLocalPos.z);
                break;
            case 2:
                spriteTransform.localRotation = Quaternion.Euler(0f, 0f, 270f);
                fireSpot.localPosition = new Vector3(fireLocalPos.y, absX > 0f ? -absX : -absY, fireLocalPos.z);
                break;
            case 8:
                spriteTransform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                fireSpot.localPosition = new Vector3(fireLocalPos.y, absX > 0f ? absX : absY, fireLocalPos.z);
                break;
        }
    }

    private Vector2 GetFacingDirection()
    {
        switch (direction)
        {
            case 2: return Vector2.down;
            case 4: return Vector2.left;
            case 6: return Vector2.right;
            case 8: return Vector2.up;
            default: return Vector2.right;
        }
    }

    private void FireLaser()
    {
        if (Bullet == null || GameController.instance == null)
        {
            return;
        }

        Vector3 firePosition = fireSpot != null ? fireSpot.position : transform.position;
        Bullet spawned = GameController.instance.FireBullet(Bullet, firePosition, GetFacingDirection(), true);
        activeLaser = spawned as LaserBullet;
        if (activeLaser != null)
        {
            activeLaser.SetDuration(downtime > 0f ? uptime : 0f);
            activeLaser.SetOwner(controller);
        }
    }

    public void ForceFireFor(float duration)
    {
        if (duration <= 0f || !isActiveAndEnabled)
        {
            return;
        }

        forcedFireTimer = Mathf.Max(forcedFireTimer, duration);
        waitingForDowntime = false;
        cycleTimer = 0f;

        if (activeLaser == null)
        {
            FireLaser();
        }

        if (activeLaser != null)
        {
            activeLaser.SetDuration(0f);
            activeLaser.SetOwner(controller);
        }
    }

    private void StopLaser(bool resetCycle)
    {
        if (activeLaser != null)
        {
            activeLaser.DestroyLaser();
            activeLaser = null;
        }

        if (resetCycle)
        {
            waitingForDowntime = false;
            cycleTimer = 0f;
            forcedFireTimer = 0f;
        }
    }

    private void UpdateCurrentPhase()
    {
        for (int i = StageNum - 1; i >= 0; i--)
        {
            if (controller.CurrentHP / controller.MaxHP <= Portion[i])
            {
                currentPhase = i;
                break;
            }
        }
    }
}
