using UnityEngine;

public class OneBeam : MonoBehaviour
{
    public bool IsEnemy;
    public float DamagePerSecond;
    public int Direction = 6;
    public float MaxLength = 20f;
    public LayerMask BlockingMask;
    public float RayStartOffset = 0.05f;

    private const float MinBeamLength = 0.05f;

    private BoxCollider2D beamCollider;
    private SpriteRenderer beamRenderer;
    private Animator beamAnimator;
    private Collider2D[] selfColliders;
    private readonly Collider2D[] overlapResults = new Collider2D[16];
    private Vector2 dir;
    private float lateralOffsetWorld;
    private int groundLayer = -1;
    private int obstacleLayer = -1;
    private int mcLayer = -1;
    private int enemyLayer = -1;

    private void Awake()
    {
        beamCollider = GetComponent<BoxCollider2D>();
        beamRenderer = GetComponent<SpriteRenderer>();
        beamAnimator = GetComponent<Animator>();
        selfColliders = GetComponentsInChildren<Collider2D>(true);
        groundLayer = LayerMask.NameToLayer("Ground");
        obstacleLayer = LayerMask.NameToLayer("Obstacle");
        mcLayer = LayerMask.NameToLayer("MC");
        enemyLayer = LayerMask.NameToLayer("Enemy");

        if (beamCollider != null)
        {
            beamCollider.isTrigger = true;
        }
    }

    private void OnEnable()
    {
        dir = GetDirectionVector(Direction);
        if (dir == Vector2.zero)
        {
            Direction = 6;
            dir = Vector2.right;
        }

        transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        UpdateBeamVisual(MinBeamLength);
        RandomizeAnimationStartFrame();
    }

    private void Update()
    {
        ResolveBeam(out float beamLength);
        UpdateBeamVisual(beamLength);
        ApplyOverlapDamage(DamagePerSecond * Time.deltaTime);
    }

    private void OnDisable()
    {
        UpdateBeamVisual(MinBeamLength);
    }

    public void Init(bool isEnemy, int direction, float damagePerSecond, LayerMask blockingMask, float maxLength, float lateralOffset)
    {
        IsEnemy = isEnemy;
        Direction = direction;
        DamagePerSecond = damagePerSecond;
        BlockingMask = blockingMask;
        MaxLength = maxLength;
        lateralOffsetWorld = lateralOffset;
        dir = GetDirectionVector(Direction);
        if (dir == Vector2.zero)
        {
            Direction = 6;
            dir = Vector2.right;
        }

        transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
    }

    private void ResolveBeam(out float beamLength)
    {
        beamLength = MaxLength;

        Vector2 origin = GetBeamOriginWorld();
        float castOffset = Mathf.Clamp(RayStartOffset, 0f, Mathf.Max(0f, MaxLength - MinBeamLength));
        Vector2 castOrigin = origin + dir * castOffset;
        float castDistance = Mathf.Max(MinBeamLength, MaxLength - castOffset);
        int detectionMask = GetDetectionMask();

        RaycastHit2D[] hits = Physics2D.RaycastAll(castOrigin, dir, castDistance, detectionMask);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hitCollider = hits[i].collider;
            if (hitCollider == null || IsSelfCollider(hitCollider))
            {
                continue;
            }

            Component target = GetDamageTarget(hitCollider);
            if (target != null)
            {
                beamLength = hits[i].distance + castOffset;
                break;
            }

            if (IsBlockingLayer(hitCollider.gameObject.layer))
            {
                beamLength = hits[i].distance + castOffset;
                break;
            }
        }

        beamLength = Mathf.Clamp(beamLength, MinBeamLength, MaxLength);
    }

    private Component GetDamageTarget(Collider2D hitCollider)
    {
        if (IsEnemy)
        {
            return hitCollider.GetComponentInParent<MCController>();
        }

        return hitCollider.GetComponentInParent<EnemyController>();
    }

    private void ApplyDamage(Component damageTarget, float damageAmount)
    {
        if (damageTarget == null || damageAmount <= 0f)
        {
            return;
        }

        if (damageTarget is MCController player)
        {
            player.Hurt(damageAmount);
            return;
        }

        if (damageTarget is EnemyController enemy)
        {
            enemy.Hurt(damageAmount);
        }
    }

    private void ApplyOverlapDamage(float damageAmount)
    {
        if (beamCollider == null || damageAmount <= 0f)
        {
            return;
        }

        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.layerMask = GetTargetMask();
        filter.useTriggers = true;

        int hitCount = beamCollider.OverlapCollider(filter, overlapResults);
        Component[] damagedTargets = new Component[hitCount];
        int damagedCount = 0;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D overlap = overlapResults[i];
            if (overlap == null || IsSelfCollider(overlap))
            {
                continue;
            }

            Component target = GetDamageTarget(overlap);
            if (target == null || ContainsTarget(damagedTargets, damagedCount, target))
            {
                continue;
            }

            damagedTargets[damagedCount] = target;
            damagedCount++;
            ApplyDamage(target, damageAmount);
        }
    }

    private void UpdateBeamVisual(float worldLength)
    {
        float axisScale = GetScaleAlongAxis(dir);
        float localLength = worldLength / Mathf.Max(0.0001f, axisScale);

        Vector2 centerOffsetWorld = Vector2.Perpendicular(dir) * lateralOffsetWorld + dir * (worldLength * 0.5f);
        transform.localPosition = WorldOffsetToLocal(centerOffsetWorld);

        if (beamRenderer != null)
        {
            Vector2 size = beamRenderer.size;
            size.x = localLength;
            beamRenderer.size = size;
        }

        if (beamCollider != null)
        {
            Vector2 size = beamCollider.size;
            size.x = localLength;
            beamCollider.size = size;
            beamCollider.offset = Vector2.zero;
        }
    }

    private bool IsBlockingLayer(int layer)
    {
        return layer == groundLayer || layer == obstacleLayer;
    }

    public float GetLocalThickness()
    {
        if (beamCollider != null)
        {
            return Mathf.Abs(beamCollider.size.y);
        }

        if (beamRenderer != null)
        {
            return Mathf.Abs(beamRenderer.size.y);
        }

        return 0.125f;
    }

    private int GetDetectionMask()
    {
        int mask = 0;

        if (groundLayer >= 0)
        {
            mask |= 1 << groundLayer;
        }

        if (obstacleLayer >= 0)
        {
            mask |= 1 << obstacleLayer;
        }

        int targetLayer = IsEnemy ? mcLayer : enemyLayer;
        if (targetLayer >= 0)
        {
            mask |= 1 << targetLayer;
        }

        return mask;
    }

    private LayerMask GetTargetMask()
    {
        int targetLayer = IsEnemy ? mcLayer : enemyLayer;
        if (targetLayer < 0)
        {
            return 0;
        }

        return 1 << targetLayer;
    }

    private bool IsSelfCollider(Collider2D other)
    {
        if (selfColliders == null)
        {
            return false;
        }

        for (int i = 0; i < selfColliders.Length; i++)
        {
            if (selfColliders[i] == other)
            {
                return true;
            }
        }

        return false;
    }

    private bool ContainsTarget(Component[] targets, int count, Component target)
    {
        for (int i = 0; i < count; i++)
        {
            if (targets[i] == target)
            {
                return true;
            }
        }

        return false;
    }

    private Vector2 GetDirectionVector(int direction)
    {
        switch (direction)
        {
            case 2: return Vector2.down;
            case 4: return Vector2.left;
            case 6: return Vector2.right;
            case 8: return Vector2.up;
            default: return Vector2.zero;
        }
    }

    private float GetScaleAlongAxis(Vector2 axis)
    {
        Vector3 scale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
        axis = new Vector2(Mathf.Abs(axis.x), Mathf.Abs(axis.y));
        return axis.x > axis.y ? Mathf.Abs(scale.x) : Mathf.Abs(scale.y);
    }

    private Vector3 WorldOffsetToLocal(Vector2 worldOffset)
    {
        Vector3 scale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
        float localX = worldOffset.x / Mathf.Max(0.0001f, Mathf.Abs(scale.x));
        float localY = worldOffset.y / Mathf.Max(0.0001f, Mathf.Abs(scale.y));
        return new Vector3(localX, localY, transform.localPosition.z);
    }

    private Vector2 GetBeamOriginWorld()
    {
        Vector2 baseOrigin = transform.parent != null ? (Vector2)transform.parent.position : (Vector2)transform.position;
        return baseOrigin + Vector2.Perpendicular(dir) * lateralOffsetWorld;
    }

    private void RandomizeAnimationStartFrame()
    {
        if (beamAnimator == null || beamAnimator.runtimeAnimatorController == null)
        {
            return;
        }

        int randomFrameIndex = Random.Range(0, 3);
        float normalizedTime = randomFrameIndex / 3f;
        beamAnimator.Play(0, 0, normalizedTime);
        beamAnimator.Update(0f);
    }
}
