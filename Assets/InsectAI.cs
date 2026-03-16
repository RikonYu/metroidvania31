using System.Collections;
using UnityEngine;

public class InsectAI : EnemyAI
{
    [Header("Insect Movement")]
    [SerializeField] private float moveAngle = 35f;
    [SerializeField] private float directApproachDistance = 1.5f;
    [SerializeField] private float moveSpeedMultiplier = 1f;

    [Header("Burrow Settings")]
    [SerializeField] private float minimumPhaseDuration = 0.1f;
    [SerializeField] private float emergeSearchRadius = 2.5f;
    [SerializeField] private float emergeSurfaceOffset = 0.4f;
    [SerializeField] private float emergeLateralRange = 0.75f;
    [SerializeField] private int emergeAttempts = 12;
    [SerializeField] private Sprite PhaseSprite;
    private SpriteRenderer spriteRenderer;
    private Sprite defaultSprite;
    private Collider2D bodyCollider;
    private Collider2D playerCollider;

    private float defaultGravityScale;
    private bool defaultIsFlying;
    private bool isPhasing;
    private bool phaseTouchedObstacle;
    private bool hasHitPlayer;
    private int arcDirection = 1;
    private float phaseTimer;
    private Collider2D phasedObstacle;
    private bool spawnDirectlyInChase;

    protected override void Start()
    {
        base.Start();

        Transform sprite = transform.Find("Sprite");
        if (sprite != null)
        {
            spriteRenderer = sprite.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                defaultSprite = spriteRenderer.sprite;
            }
        }

        bodyCollider = GetComponent<Collider2D>();
        playerCollider = playerTransform != null ? playerTransform.GetComponent<Collider2D>() : null;
        defaultGravityScale = rb != null ? rb.gravityScale : 0f;
        defaultIsFlying = controller != null && controller.IsFlying;
        currentState = spawnDirectlyInChase ? EnemyState.Chase : EnemyState.Patrol;
        if (spawnDirectlyInChase && rb != null)
        {
            controller.IsFlying = true;
            rb.gravityScale = 0f;
            rb.velocity = Vector2.zero;
        }
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

        UpdatePhase();

        if (playerTransform == null)
        {
            return;
        }

        UpdateCurrentPhase();

        if (currentState == EnemyState.Patrol)
        {
            UpdatePatrolState();
            return;
        }

        UpdateMoveState();
    }

    private void FixedUpdate()
    {
        if (IsCombatPaused())
        {
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }
            return;
        }

        if (rb == null || controller == null)
        {
            return;
        }

        if (currentState == EnemyState.Patrol)
        {
            controller.IsFlying = defaultIsFlying;
            rb.gravityScale = defaultGravityScale;

            if (controller.IsFlying)
            {
                rb.velocity = moveInput;
            }
            else
            {
                rb.velocity = new Vector2(moveInput.x, rb.velocity.y);
            }

            return;
        }

        controller.IsFlying = true;
        rb.gravityScale = 0f;
        Move(moveAngle);
        rb.velocity = Vector2.zero;
        rb.MovePosition(rb.position + moveInput * Time.fixedDeltaTime);
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

    private void UpdatePatrolState()
    {
        if (isPhasing)
        {
            StopPhasing(true);
        }

        if (CanSeePlayer())
        {
            currentState = EnemyState.Chase;
            arcDirection = Random.Range(0, 2) == 0 ? -1 : 1;
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }
            return;
        }

        if (waypoints == null || waypoints.Count == 0)
        {
            moveInput = Vector2.zero;
            return;
        }

        Vector3 targetPoint = waypoints[currentWaypointIndex];
        CalculatePatrolMovement(targetPoint);

        float distToWaypoint = controller.IsFlying
            ? Vector2.Distance(transform.position, targetPoint)
            : Mathf.Abs(transform.position.x - targetPoint.x);

        if (distToWaypoint < waypointTolerance)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
        }
    }

    private void UpdateMoveState()
    {
        Move(moveAngle);

        if (bodyCollider != null && !bodyCollider.isTrigger && IsOverlappingObstacle())
        {
            StartPhasing();
        }
    }

    public void Move(float angle)
    {
        if (playerTransform == null || controller == null)
        {
            moveInput = Vector2.zero;
            return;
        }

        Vector2 toPlayer = (playerTransform.position - transform.position);
        if (toPlayer.sqrMagnitude <= 0.0001f)
        {
            moveInput = Vector2.zero;
            return;
        }

        float adjustedAngle = angle;
        float distanceToPlayer = toPlayer.magnitude;
        if (distanceToPlayer < directApproachDistance)
        {
            adjustedAngle = Mathf.Lerp(0f, angle, distanceToPlayer / directApproachDistance);
        }

        Vector2 forward = toPlayer.normalized;
        Vector2 tangent = new Vector2(-forward.y, forward.x) * arcDirection;
        float tangentWeight = Mathf.Tan(adjustedAngle * Mathf.Deg2Rad);
        Vector2 curvedDirection = (forward + tangent * tangentWeight).normalized;

        moveInput = curvedDirection * controller.MoveSpeed * moveSpeedMultiplier;
        FaceByVelocity(curvedDirection.x);
    }

    public void LaunchFromNest(Transform target, float signedAngle)
    {
        if (target != null)
        {
            playerTransform = target;
        }

        spawnDirectlyInChase = true;
        currentState = EnemyState.Chase;
        moveAngle = Mathf.Abs(signedAngle);
        arcDirection = signedAngle < 0f ? -1 : 1;
        if (Mathf.Abs(signedAngle) <= 0.01f)
        {
            arcDirection = 1;
        }

        if (controller != null)
        {
            controller.IsFlying = true;
        }

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.velocity = Vector2.zero;
        }
    }

    protected override void Attack()
    {
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (TryHitPlayer(collision.gameObject))
        {
            return;
        }

        if (currentState != EnemyState.Patrol && IsObstacleLayer(collision.gameObject.layer))
        {
            phasedObstacle = collision.collider;
            StartPhasing();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (TryHitPlayer(other.gameObject))
        {
            return;
        }

        if (currentState != EnemyState.Patrol && IsObstacleLayer(other.gameObject.layer))
        {
            phasedObstacle = other;
            phaseTouchedObstacle = true;
        }
    }

    private void OnDestroy()
    {
        RemoveFromEnemyLists();
    }

    private void CalculatePatrolMovement(Vector3 targetPos)
    {
        Vector2 direction = (targetPos - transform.position).normalized;

        if (controller.IsFlying)
        {
            moveInput = direction * controller.MoveSpeed;
            FaceByVelocity(moveInput.x);
            return;
        }

        float dirX = 0f;
        if (Mathf.Abs(targetPos.x - transform.position.x) > 0.1f)
        {
            dirX = Mathf.Sign(targetPos.x - transform.position.x);
        }

        moveInput = new Vector2(dirX * controller.MoveSpeed, rb.velocity.y);
        FaceByVelocity(dirX);
    }

    private bool CanSeePlayer()
    {
        if (playerTransform == null)
        {
            return false;
        }

        float distToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        if (distToPlayer > viewRadius)
        {
            return false;
        }

        Vector2 dirToPlayer = (playerTransform.position - transform.position).normalized;
        Vector2 facingDir = GetFacingDirection();

        if (Vector2.Angle(facingDir, dirToPlayer) > viewAngle / 2f)
        {
            return false;
        }

        if (spawnDirectlyInChase)
        {
            return true;
        }

        RaycastHit2D hit = Physics2D.Linecast(transform.position, playerTransform.position, obstacleMask);
        return hit.collider == null;
    }

    private Vector2 GetFacingDirection()
    {
        if (spriteRenderer != null && spriteRenderer.flipX)
        {
            return Vector2.left;
        }

        return Vector2.right;
    }

    private void FaceByVelocity(float velocityX)
    {
        if (Mathf.Abs(velocityX) <= 0.01f || spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.flipX = velocityX < 0f;
    }

    private void UpdatePhase()
    {
        if (!isPhasing)
        {
            return;
        }

        phaseTimer -= Time.deltaTime;

        if (IsOverlappingObstacle())
        {
            phaseTouchedObstacle = true;
            if (phasedObstacle == null)
            {
                phasedObstacle = FindCurrentObstacle();
            }
            return;
        }

        if (phaseTouchedObstacle && phaseTimer <= 0f)
        {
            FinishPhasing();
        }
    }

    private void StartPhasing()
    {
        if (isPhasing || bodyCollider == null)
        {
            return;
        }

        isPhasing = true;
        phaseTouchedObstacle = true;
        phaseTimer = minimumPhaseDuration;
        if (phasedObstacle == null)
        {
            phasedObstacle = FindCurrentObstacle();
        }
        bodyCollider.isTrigger = true;
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }

        SetPhaseSprite(true);
    }

    private void FinishPhasing()
    {
        if (!isPhasing)
        {
            return;
        }

        if (TryFindEmergencePoint(out Vector2 preferredPosition))
        {
            transform.position = preferredPosition;
        }

        if (playerCollider != null && bodyCollider != null && bodyCollider.bounds.Intersects(playerCollider.bounds))
        {
            ResolveEmergenceOverlap();
        }

        isPhasing = false;
        phaseTouchedObstacle = false;
        if (bodyCollider != null)
        {
            bodyCollider.isTrigger = false;
        }
        phasedObstacle = null;
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }

        SetPhaseSprite(false);
    }

    private void StopPhasing(bool forceVisible)
    {
        isPhasing = false;
        phaseTouchedObstacle = false;

        if (bodyCollider != null)
        {
            bodyCollider.isTrigger = false;
        }
        phasedObstacle = null;
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }

        if (forceVisible)
        {
            SetPhaseSprite(false);
        }
    }

    private void ResolveEmergenceOverlap()
    {
        if (TryFindEmergencePoint(out Vector2 newPosition))
        {
            transform.position = newPosition;
        }
    }

    private bool TryFindPreferredEmergencePoint(out Vector2 newPosition)
    {
        Collider2D obstacle = phasedObstacle != null ? phasedObstacle : FindCurrentObstacle();
        if (obstacle == null)
        {
            newPosition = transform.position;
            return false;
        }

        Vector2 playerPosition = playerTransform != null ? playerTransform.position : transform.position;
        Vector2 surfacePoint = obstacle.ClosestPoint(playerPosition);
        Vector2 outward = playerPosition - surfacePoint;

        if (outward.sqrMagnitude <= 0.0001f)
        {
            outward = playerPosition - (Vector2)obstacle.bounds.center;
        }
        if (outward.sqrMagnitude <= 0.0001f)
        {
            outward = (Vector2)transform.position - (Vector2)obstacle.bounds.center;
        }
        if (outward.sqrMagnitude <= 0.0001f)
        {
            outward = Vector2.right;
        }

        outward.Normalize();
        Vector2 lateral = Vector2.Perpendicular(outward).normalized;

        for (int i = 0; i < emergeAttempts; i++)
        {
            float sideOffset;
            if (i == 0)
            {
                sideOffset = 0f;
            }
            else
            {
                int pairIndex = (i + 1) / 2;
                float normalizedStep = emergeAttempts <= 1 ? 0f : pairIndex / (float)Mathf.Max(1, emergeAttempts / 2);
                sideOffset = normalizedStep * emergeLateralRange;
                if (i % 2 == 0)
                {
                    sideOffset = -sideOffset;
                }
            }

            Vector2 candidate = surfacePoint + outward * emergeSurfaceOffset + lateral * sideOffset;
            if (IsValidEmergencePoint(candidate))
            {
                newPosition = candidate;
                return true;
            }
        }

        newPosition = transform.position;
        return false;
    }

    private bool TryFindEmergencePoint(out Vector2 newPosition)
    {
        Vector2 searchCenter = transform.position;
        Collider2D[] nearbyObstacles = Physics2D.OverlapCircleAll(searchCenter, emergeSearchRadius, GetObstacleCheckMask());

        for (int i = 0; i < emergeAttempts; i++)
        {
            Vector2 candidate = GetRandomEmergenceCandidate(nearbyObstacles, searchCenter);
            if (IsValidEmergencePoint(candidate))
            {
                newPosition = candidate;
                return true;
            }
        }

        if (TryFindPreferredEmergencePoint(out newPosition))
        {
            return true;
        }

        newPosition = transform.position;
        return false;
    }

    private Vector2 GetRandomEmergenceCandidate(Collider2D[] nearbyObstacles, Vector2 searchCenter)
    {
        if (nearbyObstacles != null && nearbyObstacles.Length > 0)
        {
            Collider2D obstacle = nearbyObstacles[Random.Range(0, nearbyObstacles.Length)];
            Vector2 obstacleCenter = obstacle.bounds.center;
            Vector2 randomDir = Random.insideUnitCircle;
            if (randomDir.sqrMagnitude <= 0.0001f)
            {
                randomDir = Vector2.right;
            }
            randomDir.Normalize();

            float approxRadius = Mathf.Max(obstacle.bounds.extents.x, obstacle.bounds.extents.y) + emergeSurfaceOffset + emergeLateralRange;
            Vector2 probe = obstacleCenter + randomDir * Mathf.Max(0.1f, approxRadius);
            Vector2 surfacePoint = obstacle.ClosestPoint(probe);

            Vector2 outward = surfacePoint - obstacleCenter;
            if (outward.sqrMagnitude <= 0.0001f)
            {
                outward = randomDir;
            }
            outward.Normalize();

            Vector2 lateral = Vector2.Perpendicular(outward) * Random.Range(-emergeLateralRange, emergeLateralRange);
            return surfacePoint + outward * emergeSurfaceOffset + lateral;
        }

        Vector2 fallbackDir = Random.insideUnitCircle;
        if (fallbackDir.sqrMagnitude <= 0.0001f)
        {
            fallbackDir = Vector2.right;
        }
        fallbackDir.Normalize();
        return searchCenter + fallbackDir * Random.Range(0.75f, emergeSearchRadius);
    }

    private bool IsValidEmergencePoint(Vector2 point)
    {
        if (playerCollider != null && WouldOverlapPlayer(point))
        {
            return false;
        }

        return !WouldOverlapObstacle(point);
    }

    private bool WouldOverlapPlayer(Vector2 point)
    {
        if (playerCollider == null || bodyCollider == null)
        {
            return false;
        }

        Bounds movedBounds = GetBoundsAtPoint(point);
        return movedBounds.Intersects(playerCollider.bounds);
    }

    private bool WouldOverlapObstacle(Vector2 point)
    {
        Bounds movedBounds = GetBoundsAtPoint(point);
        Collider2D hit = Physics2D.OverlapBox(movedBounds.center, movedBounds.size * 0.9f, 0f, GetObstacleCheckMask());
        return hit != null;
    }

    private Bounds GetBoundsAtPoint(Vector2 point)
    {
        if (bodyCollider == null)
        {
            return new Bounds(point, Vector3.one);
        }

        Vector3 offset = point - (Vector2)transform.position;
        Bounds movedBounds = bodyCollider.bounds;
        movedBounds.center += offset;
        return movedBounds;
    }

    private bool IsOverlappingObstacle()
    {
        if (bodyCollider == null)
        {
            return false;
        }

        Bounds bounds = bodyCollider.bounds;
        Collider2D hit = Physics2D.OverlapBox(bounds.center, bounds.size * 0.9f, 0f, GetObstacleCheckMask());
        return hit != null;
    }

    private Collider2D FindCurrentObstacle()
    {
        if (bodyCollider == null)
        {
            return null;
        }

        Bounds bounds = bodyCollider.bounds;
        return Physics2D.OverlapBox(bounds.center, bounds.size * 0.9f, 0f, GetObstacleCheckMask());
    }

    private bool IsObstacleLayer(int layer)
    {
        LayerMask mask = GetObstacleCheckMask();
        return (mask.value & (1 << layer)) != 0;
    }

    private LayerMask GetObstacleCheckMask()
    {
        int fallbackMask = LayerMask.GetMask("ground", "obstacle", "Ground", "Obstacle");
        if (obstacleMask.value == 0)
        {
            return fallbackMask;
        }

        return obstacleMask | fallbackMask;
    }

    private bool TryHitPlayer(GameObject target)
    {
        if (hasHitPlayer || target == null)
        {
            return false;
        }

        MCController player = target.GetComponent<MCController>();
        if (player == null)
        {
            return false;
        }

        hasHitPlayer = true;
        bool isDead = controller.CollideDamage >= player.CurrentHealth;
        player.ApplyDamageAndStun(controller.CollideDamage, controller.stunDuration);

        Rigidbody2D playerRb = target.GetComponent<Rigidbody2D>();
        if (playerRb != null && !isDead)
        {
            Vector2 direction = (target.transform.position - transform.position).normalized;
            if (direction.y <= 0.2f)
            {
                direction.y = 0.8f;
            }

            direction = direction.normalized;
            playerRb.velocity = Vector2.zero;
            playerRb.AddForce(direction * controller.knockbackForce, ForceMode2D.Impulse);
        }

        SelfDestruct();
        return true;
    }

    private void SelfDestruct()
    {
        RemoveFromEnemyLists();
        Destroy(gameObject);
    }

    private void RemoveFromEnemyLists()
    {
        if (GameController.instance != null)
        {
            GameController.instance.AllEnemies.Remove(controller);
        }

        Room room = transform.parent != null ? transform.parent.GetComponent<Room>() : null;
        if (room != null)
        {
            room.Enemies.Remove(controller);
        }
    }

    private void SetPhaseSprite(bool phased)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.enabled = true;

        if (phased && PhaseSprite != null)
        {
            spriteRenderer.sprite = PhaseSprite;
            return;
        }

        if (defaultSprite != null)
        {
            spriteRenderer.sprite = defaultSprite;
        }
    }
}
