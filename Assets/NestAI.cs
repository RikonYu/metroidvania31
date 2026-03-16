using System.Collections;
using UnityEngine;

public class NestAI : EnemyAI
{
    [Header("Nest Direction")]
    public int direction = 6;

    [Header("Spawn Settings")]
    public GameObject insectPrefab;
    public float spawnCooldown = 3f;
    public int spawnCount = 3;
    public float intraBurstDelay = 0.08f;
    public float maxSignedMoveAngle = 35f;

    private SpriteRenderer spriteRenderer;
    private Transform fireSpot;
    private bool isBursting;
    private float nextBurstTime;
    private bool waitForFreshSight = true;
    private Coroutine burstRoutine;

    protected override void Start()
    {
        base.Start();

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.velocity = Vector2.zero;
        }

        Transform sprite = transform.Find("Sprite");
        if (sprite != null)
        {
            spriteRenderer = sprite.GetComponent<SpriteRenderer>();
        }

        fireSpot = transform.Find("firespot");
        ApplyDirectionVisuals();
        nextBurstTime = Time.time + GetSpawnCooldown();
        waitForFreshSight = true;
    }

    protected override void Update()
    {
        if (IsCombatPaused())
        {
            moveInput = Vector2.zero;
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }
            return;
        }

        UpdateCurrentPhase();

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }

        moveInput = Vector2.zero;

        if (playerTransform == null || insectPrefab == null || isBursting)
        {
            return;
        }

        if (!CanSeePlayerInFacingView())
        {
            waitForFreshSight = true;
            return;
        }

        if (waitForFreshSight)
        {
            waitForFreshSight = false;
            nextBurstTime = Time.time + GetSpawnCooldown();
            return;
        }

        if (Time.time >= nextBurstTime)
        {
            burstRoutine = StartCoroutine(SpawnBurstRoutine());
        }
    }

    protected override void Attack()
    {
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

    private void ApplyDirectionVisuals()
    {
        if (direction != 4 && direction != 6)
        {
            direction = 6;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = direction == 4;
        }

        if (fireSpot != null)
        {
            Vector3 localPos = fireSpot.localPosition;
            float absX = Mathf.Abs(localPos.x);
            fireSpot.localPosition = new Vector3(direction == 4 ? -absX : absX, localPos.y, localPos.z);
        }
    }

    private bool CanSeePlayerInFacingView()
    {
        float distToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        if (distToPlayer > viewRadius)
        {
            return false;
        }

        Vector2 dirToPlayer = (playerTransform.position - transform.position).normalized;
        if (Vector2.Angle(GetFacingDirection(), dirToPlayer) > viewAngle / 2f)
        {
            return false;
        }

        RaycastHit2D hit = Physics2D.Linecast(transform.position, playerTransform.position, obstacleMask);
        return hit.collider == null;
    }

    private Vector2 GetFacingDirection()
    {
        return direction == 4 ? Vector2.left : Vector2.right;
    }

    private IEnumerator SpawnBurstRoutine()
    {
        isBursting = true;

        int count = Mathf.Max(1, spawnCount);
        float angleExtent = Mathf.Max(0f, maxSignedMoveAngle);

        for (int i = 0; i < count; i++)
        {
            SpawnInsect(GetSignedAngleForIndex(i, count, angleExtent));

            if (i < count - 1 && intraBurstDelay > 0f)
            {
                yield return new WaitForSeconds(intraBurstDelay);
            }
        }

        isBursting = false;
        nextBurstTime = Time.time + GetSpawnCooldown();
        burstRoutine = null;
    }

    private float GetSpawnCooldown()
    {
        float cooldown = spawnCooldown;
        if (cooldown <= 0f)
        {
            cooldown = attackCooldown;
        }

        return Mathf.Max(0.01f, cooldown);
    }

    private void OnDisable()
    {
        if (burstRoutine != null)
        {
            StopCoroutine(burstRoutine);
            burstRoutine = null;
        }

        isBursting = false;
        waitForFreshSight = true;
    }

    private float GetSignedAngleForIndex(int index, int count, float angleExtent)
    {
        if (count <= 1 || angleExtent <= 0.01f)
        {
            return 0f;
        }

        float t = count == 1 ? 0.5f : index / (float)(count - 1);
        return Mathf.Lerp(-angleExtent, angleExtent, t);
    }

    private void SpawnInsect(float signedAngle)
    {
        Vector3 spawnPosition = fireSpot != null ? fireSpot.position : transform.position;
        Transform parent = transform.parent;
        GameObject spawned = Object.Instantiate(insectPrefab, spawnPosition, Quaternion.identity, parent);
        EnemyController spawnedController = spawned.GetComponent<EnemyController>();
        if (spawnedController != null)
        {
            spawnedController.MarkAsRuntimeSpawned();
        }

        InsectAI insectAI = spawned.GetComponent<InsectAI>();
        if (insectAI != null)
        {
            insectAI.LaunchFromNest(playerTransform, signedAngle);
        }
    }
}
