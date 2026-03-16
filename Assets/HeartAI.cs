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
    private const int RevealSectorCount = 8;
    private const float RevealHalfAngle = 22.5f;

    private Room ownerRoom;
    private SpriteRenderer vesselRenderer;
    private Transform vesselTransform;
    private Transform revealMaskRoot;
    private readonly Transform[] sectorMaskTransforms = new Transform[RevealSectorCount];
    private readonly SpriteMask[] sectorMasks = new SpriteMask[RevealSectorCount];
    private readonly float[] sectorRevealProgress = new float[RevealSectorCount];
    private Vector2 vesselHalfSize;

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
            SetupVesselMask();
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

        if (vesselRenderer != null)
        {
            vesselRenderer.maskInteraction = SpriteMaskInteraction.None;
        }

        SetAllSectorMasksEnabled(false);
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
        if (damage <= 0f || !isActiveAndEnabled || !IsPlayerEngaged() || IsCombatPaused())
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

        float totalElapsed = 0f;
        float segmentDuration = Mathf.Max(0.01f, revealStepInterval);
        float revealPerStep = revealDuration > 0f
            ? (segmentDuration * RevealSectorCount) / revealDuration
            : 1f;

        while (totalElapsed < revealDuration)
        {
            int directionIndex = GetRandomUnfinishedDirection();
            if (directionIndex < 0)
            {
                break;
            }

            float currentProgress = sectorRevealProgress[directionIndex];
            float targetProgress = Mathf.Clamp01(currentProgress + revealPerStep);
            float currentStepDuration = Mathf.Min(segmentDuration, revealDuration - totalElapsed);
            float segmentElapsed = 0f;

            while (segmentElapsed < currentStepDuration)
            {
                segmentElapsed += Time.deltaTime;
                totalElapsed += Time.deltaTime;

                float t = currentStepDuration <= 0.0001f ? 1f : Mathf.Clamp01(segmentElapsed / currentStepDuration);
                sectorRevealProgress[directionIndex] = Mathf.Lerp(currentProgress, targetProgress, t);
                UpdateSectorMasks();
                yield return null;
            }

            sectorRevealProgress[directionIndex] = targetProgress;
            UpdateSectorMasks();
        }

        CompleteReveal();
    }

    private void CompleteReveal()
    {
        heartState = controller != null && controller.CurrentHP > 0f ? 2 : 1;

        if (vesselRenderer != null)
        {
            vesselRenderer.enabled = true;
            vesselRenderer.maskInteraction = SpriteMaskInteraction.None;
            Color color = vesselRenderer.color;
            color.a = 1f;
            vesselRenderer.color = color;
        }

        SetAllSectorMasksEnabled(false);
    }

    private void ApplyHiddenVessel()
    {
        if (vesselRenderer != null)
        {
            vesselRenderer.enabled = true;
            vesselRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            Color color = vesselRenderer.color;
            color.a = 1f;
            vesselRenderer.color = color;
        }

        for (int i = 0; i < sectorRevealProgress.Length; i++)
        {
            sectorRevealProgress[i] = 0f;
        }

        SetAllSectorMasksEnabled(true);
        UpdateSectorMasks();
    }

    private void UpdateSectorMasks()
    {
        for (int i = 0; i < RevealSectorCount; i++)
        {
            if (sectorMaskTransforms[i] == null || sectorMasks[i] == null)
            {
                continue;
            }

            float progress = Mathf.Clamp01(sectorRevealProgress[i]);
            sectorMasks[i].enabled = progress > 0.0001f;

            Vector2 direction = GetSectorDirection(i);
            float maxDistance = GetMaxDistanceAlongDirection(direction);
            float length = Mathf.Max(0.001f, maxDistance * progress);
            float width = Mathf.Max(0.001f, 2f * length * Mathf.Tan(RevealHalfAngle * Mathf.Deg2Rad));

            sectorMaskTransforms[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            // The mask sprite pivot is at its inner edge, so keeping it at the center
            // makes each reveal sector grow outward without erasing the already-shown area.
            sectorMaskTransforms[i].localPosition = Vector3.zero;
            sectorMaskTransforms[i].localScale = new Vector3(length, width, 1f);
        }
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

    private int GetRandomUnfinishedDirection()
    {
        int[] candidates = new int[RevealSectorCount];
        int count = 0;

        for (int i = 0; i < RevealSectorCount; i++)
        {
            if (sectorRevealProgress[i] < 0.999f)
            {
                candidates[count] = i;
                count++;
            }
        }

        if (count == 0)
        {
            return -1;
        }

        return candidates[Random.Range(0, count)];
    }

    private void SetupVesselMask()
    {
        if (vesselRenderer == null || vesselTransform == null)
        {
            return;
        }

        revealMaskRoot = vesselTransform.Find("RevealMasks");
        if (revealMaskRoot == null)
        {
            GameObject root = new GameObject("RevealMasks");
            revealMaskRoot = root.transform;
            revealMaskRoot.SetParent(vesselTransform, false);
        }

        Sprite whiteSprite = GetWhiteMaskSprite();
        vesselHalfSize = vesselRenderer.sprite != null ? vesselRenderer.sprite.bounds.extents : Vector2.one;

        for (int i = 0; i < RevealSectorCount; i++)
        {
            Transform maskTransform = revealMaskRoot.Find("SectorMask_" + i);
            if (maskTransform == null)
            {
                GameObject maskObject = new GameObject("SectorMask_" + i);
                maskTransform = maskObject.transform;
                maskTransform.SetParent(revealMaskRoot, false);
            }

            SpriteMask mask = maskTransform.GetComponent<SpriteMask>();
            if (mask == null)
            {
                mask = maskTransform.gameObject.AddComponent<SpriteMask>();
            }

            mask.sprite = whiteSprite;
            mask.alphaCutoff = 0f;
            mask.isCustomRangeActive = true;
            mask.frontSortingLayerID = vesselRenderer.sortingLayerID;
            mask.backSortingLayerID = vesselRenderer.sortingLayerID;
            mask.frontSortingOrder = vesselRenderer.sortingOrder + 1;
            mask.backSortingOrder = vesselRenderer.sortingOrder - 1;

            sectorMaskTransforms[i] = maskTransform;
            sectorMasks[i] = mask;
            sectorRevealProgress[i] = 0f;
        }

        SetAllSectorMasksEnabled(true);
        UpdateSectorMasks();
    }

    private Vector2 GetSectorDirection(int index)
    {
        float angle = index * 45f;
        float radians = angle * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)).normalized;
    }

    private float GetMaxDistanceAlongDirection(Vector2 direction)
    {
        return Mathf.Abs(direction.x) * vesselHalfSize.x + Mathf.Abs(direction.y) * vesselHalfSize.y;
    }

    private void SetAllSectorMasksEnabled(bool enabled)
    {
        for (int i = 0; i < sectorMasks.Length; i++)
        {
            if (sectorMasks[i] != null)
            {
                sectorMasks[i].enabled = enabled;
            }
        }
    }

    private Sprite GetWhiteMaskSprite()
    {
        if (whiteMaskSprite == null)
        {
            float pixelsPerUnit = Mathf.Max(1f, Texture2D.whiteTexture.width);
            whiteMaskSprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                new Vector2(0f, 0.5f),
                pixelsPerUnit);
        }

        return whiteMaskSprite;
    }

    private static Sprite whiteMaskSprite;
}
