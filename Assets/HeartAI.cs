using System.Collections;
using UnityEngine;

public class HeartAI : EnemyAI
{
    public GameObject LaunchPrefab;
    public GameObject InsectPrefab;

    [Header("State 1")]
    public float state1LaunchInterval = 2.5f;

    [Header("State 2")]
    public float state2BurstInterval = 1.5f;

    [Header("Damage Reaction")]
    public float hurtBurstCooldown = 1.5f;

    [Header("Vessel Reveal")]
    public float revealStepInterval = 1.2f;
    public float revealDuration = 8f;

    private const float ForbiddenAngle = 30f;

    private Room ownerRoom;
    private SpriteRenderer vesselRenderer;
    private Transform vesselTransform;
    private Vector3 vesselBaseLocalPosition;
    private Vector3 vesselBaseLocalScale;

    private int heartState = 1;
    private float nextStateActionTime;
    private float nextHurtBurstTime;
    private Coroutine revealRoutine;

    protected override void Start()
    {
        base.Start();
        currentState = EnemyState.Patrol;

        ownerRoom = GetComponentInParent<Room>();
        vesselTransform = transform.Find("Vessel");
        if (vesselTransform != null)
        {
            vesselRenderer = vesselTransform.GetComponent<SpriteRenderer>();
            vesselBaseLocalPosition = vesselTransform.localPosition;
            vesselBaseLocalScale = vesselTransform.localScale;
            ApplyHiddenVessel();
        }

        if (controller != null)
        {
            controller.Damaged += HandleDamaged;
        }

        if (revealDuration > 0f)
        {
            revealRoutine = StartCoroutine(RevealVesselRoutine());
        }
        else
        {
            CompleteReveal();
        }
    }

    private void OnDisable()
    {
        if (controller != null)
        {
            controller.Damaged -= HandleDamaged;
        }

        if (revealRoutine != null)
        {
            StopCoroutine(revealRoutine);
            revealRoutine = null;
        }
    }

    protected override void Update()
    {
        UpdateCurrentPhase();

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }

        moveInput = Vector2.zero;
        if (!IsPlayerEngaged())
        {
            return;
        }

        if (Time.time < nextStateActionTime)
        {
            return;
        }

        if (heartState == 1)
        {
            FireRandomLaunch();
            nextStateActionTime = Time.time + Mathf.Max(0.01f, state1LaunchInterval);
            return;
        }

        FireRandomLaunch();
        FireRandomInsects(3);
        nextStateActionTime = Time.time + Mathf.Max(0.01f, state2BurstInterval);
    }

    private void HandleDamaged(float damage)
    {
        if (damage <= 0f || !isActiveAndEnabled || !IsPlayerEngaged())
        {
            return;
        }

        if (Time.time < nextHurtBurstTime)
        {
            return;
        }

        FireRandomLaunch();
        FireRandomInsects(3);
        nextHurtBurstTime = Time.time + Mathf.Max(0.01f, hurtBurstCooldown);
    }

    private IEnumerator RevealVesselRoutine()
    {
        if (vesselTransform == null || vesselRenderer == null)
        {
            yield break;
        }

        float elapsed = 0f;
        float nextRevealStep = 0f;
        RevealDirection currentDirection = GetRandomRevealDirection();

        while (elapsed < revealDuration)
        {
            if (elapsed >= nextRevealStep)
            {
                currentDirection = GetRandomRevealDirection();
                nextRevealStep += Mathf.Max(0.01f, revealStepInterval);
            }

            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / revealDuration);
            ApplyVesselReveal(progress, currentDirection);
            yield return null;
        }

        CompleteReveal();
    }

    private void CompleteReveal()
    {
        heartState = controller != null && controller.CurrentHP > 0f ? 2 : 1;

        if (vesselTransform != null)
        {
            vesselTransform.localScale = vesselBaseLocalScale;
            vesselTransform.localPosition = vesselBaseLocalPosition;
        }

        if (vesselRenderer != null)
        {
            vesselRenderer.enabled = true;
            Color color = vesselRenderer.color;
            color.a = 1f;
            vesselRenderer.color = color;
        }
    }

    private void ApplyHiddenVessel()
    {
        if (vesselRenderer != null)
        {
            vesselRenderer.enabled = false;
            Color color = vesselRenderer.color;
            color.a = 1f;
            vesselRenderer.color = color;
        }

        if (vesselTransform != null)
        {
            vesselTransform.localScale = Vector3.zero;
            vesselTransform.localPosition = vesselBaseLocalPosition;
        }
    }

    private void ApplyVesselReveal(float progress, RevealDirection direction)
    {
        if (vesselTransform == null || vesselRenderer == null)
        {
            return;
        }

        vesselRenderer.enabled = progress > 0f;

        Vector2 axisMask = GetAxisMask(direction);
        Vector2 anchor = GetAnchor(direction);

        float scaleX = Mathf.Lerp(1f, progress, axisMask.x);
        float scaleY = Mathf.Lerp(1f, progress, axisMask.y);

        Vector3 targetScale = new Vector3(
            vesselBaseLocalScale.x * Mathf.Max(0.001f, scaleX),
            vesselBaseLocalScale.y * Mathf.Max(0.001f, scaleY),
            vesselBaseLocalScale.z);

        Vector3 offset = new Vector3(
            anchor.x * vesselBaseLocalScale.x * (1f - scaleX) * 0.5f,
            anchor.y * vesselBaseLocalScale.y * (1f - scaleY) * 0.5f,
            0f);

        vesselTransform.localScale = targetScale;
        vesselTransform.localPosition = vesselBaseLocalPosition + offset;
    }

    private void FireRandomLaunch()
    {
        if (LaunchPrefab == null || GameController.instance == null)
        {
            return;
        }

        Vector2 fireDirection = GetRandomAllowedDirection();
        GameController.instance.FireBullet(LaunchPrefab, transform.position, fireDirection, true);
    }

    private void FireRandomInsects(int count)
    {
        if (InsectPrefab == null || playerTransform == null)
        {
            return;
        }

        Vector2 baseToPlayer = GetDirectionToPlayer();
        if (baseToPlayer == Vector2.zero)
        {
            baseToPlayer = Vector2.right;
        }

        for (int i = 0; i < count; i++)
        {
            GameObject spawned = Instantiate(InsectPrefab, transform.position, Quaternion.identity);
            InsectAI insectAI = spawned.GetComponent<InsectAI>();
            if (insectAI == null)
            {
                continue;
            }

            Vector2 randomDirection = GetRandomAllowedDirection();
            float signedAngle = Vector2.SignedAngle(baseToPlayer, randomDirection);
            insectAI.LaunchFromNest(playerTransform, signedAngle);
        }
    }

    private Vector2 GetRandomAllowedDirection()
    {
        Vector2 playerDirection = GetDirectionToPlayer();
        if (playerDirection == Vector2.zero)
        {
            playerDirection = Vector2.right;
        }

        for (int i = 0; i < 24; i++)
        {
            float angle = Random.Range(0f, 360f);
            Vector2 direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)).normalized;
            if (Vector2.Angle(direction, playerDirection) > ForbiddenAngle)
            {
                return direction;
            }
        }

        return Quaternion.Euler(0f, 0f, ForbiddenAngle + 15f) * playerDirection;
    }

    private Vector2 GetDirectionToPlayer()
    {
        if (playerTransform == null)
        {
            return Vector2.zero;
        }

        Vector2 direction = (playerTransform.position - transform.position);
        return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.zero;
    }

    private bool IsPlayerEngaged()
    {
        if (GameController.instance == null || ownerRoom == null)
        {
            return false;
        }

        return GameController.instance.ActiveRoom == ownerRoom;
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

    private RevealDirection GetRandomRevealDirection()
    {
        return (RevealDirection)Random.Range(0, 8);
    }

    private Vector2 GetAnchor(RevealDirection direction)
    {
        switch (direction)
        {
            case RevealDirection.Left: return Vector2.left;
            case RevealDirection.Right: return Vector2.right;
            case RevealDirection.Up: return Vector2.up;
            case RevealDirection.Down: return Vector2.down;
            case RevealDirection.UpLeft: return new Vector2(-1f, 1f);
            case RevealDirection.UpRight: return new Vector2(1f, 1f);
            case RevealDirection.DownLeft: return new Vector2(-1f, -1f);
            case RevealDirection.DownRight: return new Vector2(1f, -1f);
            default: return Vector2.zero;
        }
    }

    private Vector2 GetAxisMask(RevealDirection direction)
    {
        switch (direction)
        {
            case RevealDirection.Left:
            case RevealDirection.Right:
                return new Vector2(1f, 0f);
            case RevealDirection.Up:
            case RevealDirection.Down:
                return new Vector2(0f, 1f);
            default:
                return Vector2.one;
        }
    }

    private enum RevealDirection
    {
        Left,
        Right,
        Up,
        Down,
        UpLeft,
        UpRight,
        DownLeft,
        DownRight
    }
}
