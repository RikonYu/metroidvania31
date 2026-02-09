using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyState
{
    Patrol,
    Chase,
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

    [Tooltip("当前行为阶段（由HP决定）")]
    public int currentPhase;

    [Tooltip("各阶段HP百分比阈值 (1.0 -> 0.0)")]
    public List<float> Portion = new List<float>() { 1.0f };

    [Header("AI State")]
    [SerializeField]
    [Tooltip("当前AI行为状态")]
    private EnemyState currentState = EnemyState.Patrol;

    [Header("Detection Settings")]
    [Range(0, 360)]
    [Tooltip("索敌扇形角度")]
    public float viewAngle = 90f;

    [Tooltip("索敌视线半径")]
    public float viewRadius = 10f;

    [Tooltip("最大追击距离 (超过此距离放弃追击)")]
    public float chaseRadius = 15f;

    [Tooltip("视线遮挡层级 (通常选择Ground和Obstacle)")]
    public LayerMask obstacleMask;

    [Header("Combat Settings")]
    [Tooltip("攻击触发距离")]
    public float attackRange = 1.5f;

    [Tooltip("攻击冷却时间 (秒)")]
    public float attackCooldown = 2.0f;
    private float lastAttackTime;

    [Header("Patrol Settings")]
    [Tooltip("到达路点的判定距离阈值")]
    public float waypointTolerance = 0.5f;

    [Tooltip("丢失目标后的原地等待/搜索时间")]
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
        print($"currentstate: {currentState}");
        switch (currentState)
        {
            case EnemyState.Patrol:
                UpdatePatrol();
                break;
            case EnemyState.Chase:
                UpdateChase();
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

    void UpdatePatrol()
    {
        if (CanSeePlayer())
        {
            currentState = EnemyState.Chase;
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
            currentState = EnemyState.Engage;
            moveInput = Vector2.zero;
            return;
        }

        CalculateMovement(playerTransform.position);
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
            currentState = EnemyState.Chase;
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