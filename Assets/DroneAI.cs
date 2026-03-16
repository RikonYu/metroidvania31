using UnityEngine;

public class DroneAI : EnemyAI
{
    [Header("Frozen Behavior")]
    public float frozenFallGravity = 4f;
    public float frozenMaxFallSpeed = 16f;
    public float returnRiseSpeed = 5f;
    public float returnTolerance = 0.08f;

    private bool wasFrozenLastFrame;
    private bool isReturningToRoute;
    private float returnToPathY;
    private float frozenRotation;
    private RigidbodyConstraints2D cachedConstraints;
    private float cachedGravityScale;

    protected override void Start()
    {
        base.Start();
        currentState = EnemyState.Patrol;
        if (rb != null)
        {
            cachedConstraints = rb.constraints;
            cachedGravityScale = rb.gravityScale;
        }
    }

    protected override void Update()
    {
        HandleFreezeTransitions();

        if (controller != null && controller.IsFrozen)
        {
            UpdateFrozenFalling();
            return;
        }

        if (isReturningToRoute)
        {
            UpdateReturnToRoute();
            return;
        }

        if (IsCombatPaused())
        {
            moveInput = Vector2.zero;
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }
            return;
        }

        for (int i = StageNum - 1; i >= 0; i--)
        {
            if (controller.CurrentHP / controller.MaxHP <= Portion[i])
            {
                currentPhase = i;
                break;
            }
        }

        if (playerTransform == null)
        {
            moveInput = Vector2.zero;
            return;
        }

        UpdatePatrolMovement();

        if (CanSeePlayerFromPatrol())
        {
            FaceTowards(playerTransform.position);

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                Attack();
                lastAttackTime = Time.time;
            }
        }
    }

    private void FixedUpdate()
    {
        if (controller != null && controller.IsFrozen)
        {
            return;
        }

        ApplyMovement(moveInput);
    }

    private void UpdatePatrolMovement()
    {
        if (waypoints == null || waypoints.Count == 0)
        {
            moveInput = Vector2.zero;
            return;
        }

        Vector3 targetPoint = waypoints[currentWaypointIndex];
        Vector2 toTarget = targetPoint - transform.position;

        if (controller.IsFlying)
        {
            moveInput = toTarget.normalized * controller.MoveSpeed;
        }
        else
        {
            float dirX = Mathf.Abs(toTarget.x) > 0.1f ? Mathf.Sign(toTarget.x) : 0f;
            moveInput = new Vector2(dirX * controller.MoveSpeed, rb != null ? rb.velocity.y : 0f);
        }

        if (Mathf.Abs(moveInput.x) > 0.1f)
        {
            FaceTowards(targetPoint);
        }

        float distToWaypoint = controller.IsFlying
            ? Vector2.Distance(transform.position, targetPoint)
            : Mathf.Abs(transform.position.x - targetPoint.x);

        if (distToWaypoint <= waypointTolerance)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
        }
    }

    private void HandleFreezeTransitions()
    {
        if (controller == null)
        {
            return;
        }

        bool isFrozen = controller.IsFrozen;
        if (isFrozen && !wasFrozenLastFrame)
        {
            EnterFrozenFall();
        }
        else if (!isFrozen && wasFrozenLastFrame)
        {
            ExitFrozenFall();
        }

        wasFrozenLastFrame = isFrozen;
    }

    private void EnterFrozenFall()
    {
        isReturningToRoute = false;
        moveInput = Vector2.zero;
        controller.IsFlying = false;

        if (rb == null)
        {
            return;
        }

        cachedConstraints = rb.constraints;
        cachedGravityScale = rb.gravityScale;
        frozenRotation = rb.rotation;

        rb.constraints = cachedConstraints | RigidbodyConstraints2D.FreezeRotation;
        rb.gravityScale = frozenFallGravity > 0f ? frozenFallGravity : Mathf.Max(0.1f, cachedGravityScale);
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    private void UpdateFrozenFalling()
    {
        moveInput = Vector2.zero;
        if (rb == null)
        {
            return;
        }

        rb.angularVelocity = 0f;
        rb.rotation = frozenRotation;

        Vector2 velocity = rb.velocity;
        velocity.x = 0f;
        if (velocity.y < -frozenMaxFallSpeed)
        {
            velocity.y = -frozenMaxFallSpeed;
        }

        rb.velocity = velocity;
    }

    private void ExitFrozenFall()
    {
        if (controller == null || controller.CurrentHP <= 0f)
        {
            return;
        }

        controller.IsFlying = true;

        if (rb != null)
        {
            rb.constraints = cachedConstraints;
            rb.angularVelocity = 0f;
            rb.rotation = frozenRotation;
            rb.gravityScale = 0f;
            rb.velocity = Vector2.zero;
        }

        Vector2 closestOnPath = GetNearestPointOnPatrolPath(transform.position);
        returnToPathY = closestOnPath.y;
        isReturningToRoute = true;
        moveInput = Vector2.zero;
    }

    private void UpdateReturnToRoute()
    {
        float deltaY = returnToPathY - transform.position.y;
        if (Mathf.Abs(deltaY) <= returnTolerance)
        {
            Vector3 pos = transform.position;
            pos.y = returnToPathY;
            transform.position = pos;

            moveInput = Vector2.zero;
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.gravityScale = 0f;
            }

            isReturningToRoute = false;
            currentWaypointIndex = GetClosestWaypointIndexLocal();
            return;
        }

        float riseSpeed = returnRiseSpeed > 0f ? returnRiseSpeed : controller.MoveSpeed;
        moveInput = new Vector2(0f, Mathf.Sign(deltaY) * riseSpeed);
    }

    private Vector2 GetNearestPointOnPatrolPath(Vector2 from)
    {
        if (waypoints == null || waypoints.Count == 0)
        {
            return from;
        }

        if (waypoints.Count == 1)
        {
            return waypoints[0];
        }

        Vector2 bestPoint = waypoints[0];
        float bestDistSqr = float.MaxValue;

        for (int i = 0; i < waypoints.Count; i++)
        {
            Vector2 a = waypoints[i];
            Vector2 b = waypoints[(i + 1) % waypoints.Count];
            Vector2 candidate = ClosestPointOnSegment(a, b, from);
            float distSqr = (candidate - from).sqrMagnitude;
            if (distSqr < bestDistSqr)
            {
                bestDistSqr = distSqr;
                bestPoint = candidate;
            }
        }

        return bestPoint;
    }

    private Vector2 ClosestPointOnSegment(Vector2 a, Vector2 b, Vector2 point)
    {
        Vector2 ab = b - a;
        float denom = ab.sqrMagnitude;
        if (denom <= 0.0001f)
        {
            return a;
        }

        float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / denom);
        return a + ab * t;
    }

    private int GetClosestWaypointIndexLocal()
    {
        if (waypoints == null || waypoints.Count == 0)
        {
            return 0;
        }

        int closestIndex = 0;
        float minDist = float.MaxValue;

        for (int i = 0; i < waypoints.Count; i++)
        {
            float dist = Vector2.Distance(transform.position, waypoints[i]);
            if (dist < minDist)
            {
                minDist = dist;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDieFromTrap(collision != null ? collision.gameObject : null);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDieFromTrap(other != null ? other.gameObject : null);
    }

    private void TryDieFromTrap(GameObject otherObject)
    {
        if (controller == null || !controller.IsFrozen || controller.CurrentHP <= 0f || otherObject == null)
        {
            return;
        }

        if (!IsTrapObject(otherObject))
        {
            return;
        }

        controller.Hurt(controller.CurrentHP + controller.MaxHP);
    }

    private bool IsTrapObject(GameObject otherObject)
    {
        return otherObject.GetComponent<Trap>() != null
            || otherObject.GetComponentInParent<Trap>() != null
            || otherObject.GetComponent<DeathTrap>() != null
            || otherObject.GetComponentInParent<DeathTrap>() != null;
    }

    private bool CanSeePlayerFromPatrol()
    {
        float distToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        if (distToPlayer > viewRadius)
        {
            return false;
        }

        Vector2 facingDir = GetFacingDirection();
        Vector2 dirToPlayer = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
        if (Vector2.Angle(facingDir, dirToPlayer) > viewAngle * 0.5f)
        {
            return false;
        }

        RaycastHit2D hit = Physics2D.Linecast(transform.position, playerTransform.position, obstacleMask);
        return hit.collider == null;
    }

    private Vector2 GetFacingDirection()
    {
        if (controller.IsFlying && moveInput.sqrMagnitude > 0.001f)
        {
            return moveInput.normalized;
        }

        return transform.localScale.x >= 0f ? Vector2.right : Vector2.left;
    }

    private void ApplyMovement(Vector2 velocity)
    {
        if (rb == null)
        {
            return;
        }

        if (controller.IsFlying)
        {
            rb.velocity = velocity;
        }
        else
        {
            rb.velocity = new Vector2(velocity.x, rb.velocity.y);
        }
    }

    private void FaceTowards(Vector3 targetPos)
    {
        if (targetPos.x > transform.position.x)
        {
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
        else if (targetPos.x < transform.position.x)
        {
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
    }
}
