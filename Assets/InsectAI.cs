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

    private Animator spriteAnimator;
    private SpriteRenderer spriteRenderer;
    private Collider2D bodyCollider;
    private Collider2D playerCollider;

    private float defaultGravityScale;
    private float burrowAnimDuration = 0.25f;
    private string burrowStateName = "insect";
    private bool defaultIsFlying;
    private bool isPhasing;
    private bool phaseTouchedObstacle;
    private bool hasHitPlayer;
    private int arcDirection = 1;
    private float phaseTimer;
    private Coroutine burrowRoutine;

    protected override void Start()
    {
        base.Start();

        Transform sprite = transform.Find("Sprite");
        if (sprite != null)
        {
            spriteAnimator = sprite.GetComponent<Animator>();
            spriteRenderer = sprite.GetComponent<SpriteRenderer>();
        }

        bodyCollider = GetComponent<Collider2D>();
        playerCollider = playerTransform != null ? playerTransform.GetComponent<Collider2D>() : null;
        defaultGravityScale = rb != null ? rb.gravityScale : 0f;
        defaultIsFlying = controller != null && controller.IsFlying;
        currentState = EnemyState.Patrol;

        if (spriteAnimator != null && spriteAnimator.runtimeAnimatorController != null)
        {
            AnimationClip[] clips = spriteAnimator.runtimeAnimatorController.animationClips;
            if (clips != null && clips.Length > 0 && clips[0] != null)
            {
                burrowAnimDuration = Mathf.Max(0.01f, clips[0].length);
                burrowStateName = clips[0].name;
            }
        }
    }

    protected override void Update()
    {
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

    private new void FixedUpdate()
    {
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
        rb.velocity = moveInput;
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

    protected override void Attack()
    {
    }

    protected override void OnCollisionEnter2D(Collision2D collision)
    {
        if (TryHitPlayer(collision.gameObject))
        {
            return;
        }

        if (currentState != EnemyState.Patrol && IsObstacleLayer(collision.gameObject.layer))
        {
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
        bodyCollider.isTrigger = true;

        if (burrowRoutine != null)
        {
            StopCoroutine(burrowRoutine);
        }
        burrowRoutine = StartCoroutine(BurrowInRoutine());
    }

    private void FinishPhasing()
    {
        if (!isPhasing)
        {
            return;
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

        if (burrowRoutine != null)
        {
            StopCoroutine(burrowRoutine);
        }
        burrowRoutine = StartCoroutine(BurrowOutRoutine());
    }

    private void StopPhasing(bool forceVisible)
    {
        isPhasing = false;
        phaseTouchedObstacle = false;

        if (bodyCollider != null)
        {
            bodyCollider.isTrigger = false;
        }

        if (burrowRoutine != null)
        {
            StopCoroutine(burrowRoutine);
            burrowRoutine = null;
        }

        if (spriteAnimator != null)
        {
            spriteAnimator.enabled = false;
        }

        if (forceVisible && spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }
    }

    private IEnumerator BurrowInRoutine()
    {
        PlayBurrowAnimation(1f, 0f);
        yield return new WaitForSeconds(burrowAnimDuration);

        if (isPhasing && spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        if (spriteAnimator != null)
        {
            spriteAnimator.enabled = false;
        }

        burrowRoutine = null;
    }

    private IEnumerator BurrowOutRoutine()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }

        PlayBurrowAnimation(-1f, 1f);
        yield return new WaitForSeconds(burrowAnimDuration);

        if (spriteAnimator != null)
        {
            spriteAnimator.enabled = false;
        }

        burrowRoutine = null;
    }

    private void PlayBurrowAnimation(float speed, float normalizedTime)
    {
        if (spriteAnimator == null)
        {
            return;
        }

        spriteAnimator.enabled = true;
        spriteAnimator.speed = speed;
        spriteAnimator.Play(burrowStateName, 0, normalizedTime);
        spriteAnimator.Update(0f);
    }

    private void ResolveEmergenceOverlap()
    {
        if (TryFindEmergencePoint(out Vector2 newPosition))
        {
            transform.position = newPosition;
        }
    }

    private bool TryFindEmergencePoint(out Vector2 newPosition)
    {
        Collider2D[] nearbyObstacles = Physics2D.OverlapCircleAll(transform.position, emergeSearchRadius, obstacleMask);

        for (int i = 0; i < emergeAttempts; i++)
        {
            Vector2 candidate = GetRandomEmergenceCandidate(nearbyObstacles);
            if (IsValidEmergencePoint(candidate))
            {
                newPosition = candidate;
                return true;
            }
        }

        newPosition = transform.position;
        return false;
    }

    private Vector2 GetRandomEmergenceCandidate(Collider2D[] nearbyObstacles)
    {
        if (nearbyObstacles != null && nearbyObstacles.Length > 0)
        {
            Collider2D obstacle = nearbyObstacles[Random.Range(0, nearbyObstacles.Length)];
            Vector2 reference = playerTransform != null ? playerTransform.position : transform.position;
            Vector2 closestPoint = obstacle.ClosestPoint(reference);
            Vector2 away = closestPoint - reference;
            if (away.sqrMagnitude <= 0.0001f)
            {
                away = Random.insideUnitCircle.normalized;
            }

            Vector2 lateral = Vector2.Perpendicular(away.normalized) * Random.Range(-emergeLateralRange, emergeLateralRange);
            return closestPoint + away.normalized * emergeSurfaceOffset + lateral;
        }

        return (Vector2)transform.position + Random.insideUnitCircle.normalized * Random.Range(0.75f, emergeSearchRadius);
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
        Collider2D hit = Physics2D.OverlapBox(movedBounds.center, movedBounds.size * 0.9f, 0f, obstacleMask);
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
        Collider2D hit = Physics2D.OverlapBox(bounds.center, bounds.size * 0.9f, 0f, obstacleMask);
        return hit != null;
    }

    private bool IsObstacleLayer(int layer)
    {
        return (obstacleMask.value & (1 << layer)) != 0;
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
}
