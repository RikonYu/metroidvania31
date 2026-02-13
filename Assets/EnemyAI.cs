using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyState
{
    Patrol,
    Alert,
    Chase,
    PrepareAttack,
    Engage,
    Search
}

public class EnemyAI : MonoBehaviour
{
    protected EnemyController controller;
    private Rigidbody2D rb;
    private Transform playerTransform;

    [Header("Phase Settings")]
    public int StageNum;
    public int currentPhase;
    public List<float> Portion = new List<float>() { 1.0f };

    [Header("AI State")]
    [SerializeField]
    private EnemyState currentState = EnemyState.Patrol;

    [Header("Detection Settings")]
    [Range(0, 360)]
    public float viewAngle = 90f;
    public float viewRadius = 10f;
    public float chaseRadius = 15f;
    public LayerMask obstacleMask;

    [Header("Transition Settings")]
    public float alertDuration = 0.8f;
    private float alertTimer;
    public float prepareAttackDuration = 0.5f;
    private float prepareAttackTimer;

    [Header("Combat Settings")]
    public float attackRange = 1.5f;
    public float attackCooldown = 2.0f;
    private float lastAttackTime;

    [Header("Patrol Settings")]
    public float waypointTolerance = 0.5f;
    public float searchWaitTime = 2.0f;
    private float searchTimer;
    private int currentWaypointIndex = 0;
    private List<Transform> waypoints;

    private Vector2 moveInput;

    void Start()
    {
        controller = GetComponent<EnemyController>();
        rb = GetComponent<Rigidbody2D>();

        if (GameController.instance != null && GameController.instance.mc != null)
        {
            playerTransform = GameController.instance.mc.transform;
        }

        StageNum = Portion.Count;

        if (controller.wm != null)
        {
            waypoints = controller.wm.GetWaypoints();
        }
    }

    void Update()
    {
        for (int i = StageNum - 1; i >= 0; i--)
        {
            if (controller.CurrentHP / controller.MaxHP <= Portion[i])
            {
                currentPhase = i;
                break;
            }
        }

        if (playerTransform == null) return;

        switch (currentState)
        {
            case EnemyState.Patrol:
                UpdatePatrol();
                break;
            case EnemyState.Alert:
                UpdateAlert();
                break;
            case EnemyState.Chase:
                UpdateChase();
                break;
            case EnemyState.PrepareAttack:
                UpdatePrepareAttack();
                break;
            case EnemyState.Engage:
                UpdateEngage();
                break;
            case EnemyState.Search:
                UpdateSearch();
                break;
        }
    }

    void FixedUpdate()
    {
        MoveCharacter(moveInput);
    }

    public void ResetToPatrol()
    {
        currentState = EnemyState.Patrol;
        alertTimer = 0;
        prepareAttackTimer = 0;
        searchTimer = 0;
        moveInput = Vector2.zero;
        if (rb != null) rb.velocity = Vector2.zero;

        if (waypoints != null && waypoints.Count > 0)
        {
            currentWaypointIndex = GetClosestWaypointIndex();
        }
    }

    void UpdatePatrol()
    {
        if (CanSeePlayer())
        {
            currentState = EnemyState.Alert;
            alertTimer = alertDuration;
            moveInput = Vector2.zero;
            return;
        }

        if (waypoints == null || waypoints.Count == 0) return;

        Transform targetPoint = waypoints[currentWaypointIndex];
        CalculateMovement(targetPoint.position);

        float distToWaypoint = controller.IsFlying
            ? Vector2.Distance(transform.position, targetPoint.position)
            : Mathf.Abs(transform.position.x - targetPoint.position.x);

        if (distToWaypoint < waypointTolerance)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
        }
    }

    void UpdateAlert()
    {
        moveInput = Vector2.zero;
        FaceTarget(playerTransform.position);
        alertTimer -= Time.deltaTime;

        if (alertTimer <= 0)
        {
            currentState = EnemyState.Chase;
        }
    }

    void UpdateChase()
    {
        float distToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        if (distToPlayer > chaseRadius)
        {
            currentState = EnemyState.Search;
            searchTimer = searchWaitTime;
            moveInput = Vector2.zero;
            return;
        }

        if (distToPlayer <= attackRange)
        {
            currentState = EnemyState.PrepareAttack;
            prepareAttackTimer = prepareAttackDuration;
            moveInput = Vector2.zero;
            return;
        }

        CalculateMovement(playerTransform.position);
    }

    void UpdatePrepareAttack()
    {
        moveInput = Vector2.zero;
        FaceTarget(playerTransform.position);
        prepareAttackTimer -= Time.deltaTime;

        if (prepareAttackTimer <= 0)
        {
            currentState = EnemyState.Engage;
        }
    }

    void UpdateEngage()
    {
        float distToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        if (distToPlayer > attackRange)
        {
            currentState = EnemyState.Chase;
            return;
        }

        FaceTarget(playerTransform.position);
        moveInput = Vector2.zero;

        if (Time.time >= lastAttackTime + attackCooldown)
        {
            Attack();
            lastAttackTime = Time.time;
        }
    }

    void UpdateSearch()
    {
        if (CanSeePlayer())
        {
            currentState = EnemyState.Alert;
            alertTimer = alertDuration;
            moveInput = Vector2.zero;
            return;
        }

        searchTimer -= Time.deltaTime;
        moveInput = Vector2.zero;

        if (searchTimer <= 0)
        {
            currentWaypointIndex = GetClosestWaypointIndex();
            currentState = EnemyState.Patrol;
        }
    }

    void CalculateMovement(Vector3 targetPos)
    {
        Vector2 direction = (targetPos - transform.position).normalized;

        if (controller.IsFlying)
        {
            moveInput = direction * controller.MoveSpeed;
        }
        else
        {
            float dirX = 0;
            if (Mathf.Abs(targetPos.x - transform.position.x) > 0.1f)
            {
                dirX = Mathf.Sign(targetPos.x - transform.position.x);
            }

            moveInput = new Vector2(dirX * controller.MoveSpeed, rb.velocity.y);
        }

        if (Mathf.Abs(moveInput.x) > 0.1f)
        {
            FaceTarget(targetPos);
        }
    }

    void MoveCharacter(Vector2 velocity)
    {
        if (controller.IsFlying)
        {
            rb.velocity = velocity;
        }
        else
        {
            rb.velocity = new Vector2(velocity.x, rb.velocity.y);
        }
    }

    void FaceTarget(Vector3 targetPos)
    {
        if (targetPos.x > transform.position.x)
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, 1);
        else if (targetPos.x < transform.position.x)
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, 1);
    }

    protected virtual void Attack()
    {
    }

    int GetClosestWaypointIndex()
    {
        if (waypoints == null || waypoints.Count == 0) return 0;

        int closestIndex = 0;
        float minDst = float.MaxValue;

        for (int i = 0; i < waypoints.Count; i++)
        {
            float dst = Vector2.Distance(transform.position, waypoints[i].position);
            if (dst < minDst)
            {
                minDst = dst;
                closestIndex = i;
            }
        }
        return closestIndex;
    }

    bool CanSeePlayer()
    {
        if (playerTransform == null) return false;

        float distToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        if (distToPlayer > viewRadius) return false;

        Vector2 dirToPlayer = (playerTransform.position - transform.position).normalized;
        Vector2 facingDir = transform.localScale.x > 0 ? Vector2.right : Vector2.left;

        if (Vector2.Angle(facingDir, dirToPlayer) > viewAngle / 2f) return false;

        RaycastHit2D hit = Physics2D.Linecast(transform.position, playerTransform.position, obstacleMask);

        if (hit.collider != null)
        {
            return false;
        }

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRadius);

        Vector3 viewAngleA = DirFromAngle(-viewAngle / 2, false);
        Vector3 viewAngleB = DirFromAngle(viewAngle / 2, false);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + viewAngleA * viewRadius);
        Gizmos.DrawLine(transform.position, transform.position + viewAngleB * viewRadius);
    }

    public Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
        {
            if (transform.localScale.x < 0)
                angleInDegrees += 180;
            else
                angleInDegrees += 0;
        }
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), Mathf.Cos(angleInDegrees * Mathf.Deg2Rad), 0);
    }
}