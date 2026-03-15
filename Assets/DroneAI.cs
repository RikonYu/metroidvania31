using UnityEngine;

public class DroneAI : EnemyAI
{
    protected override void Start()
    {
        base.Start();
        currentState = EnemyState.Patrol;
    }

    protected override void Update()
    {
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
