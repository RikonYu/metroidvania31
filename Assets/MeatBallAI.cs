using UnityEngine;

public class MetaBallAI : EnemyAI
{
    [Header("Spawn")]
    public GameObject InsectPrefab;

    [Header("Fly")]
    public float flySpeed = 8f;
    public LayerMask stopMask;

    [Header("Split")]
    public int splitCount = 3;
    public float splitInitialSpeed = 6f;
    public float splitInitialDuration = 0.5f;
    public float splitAttackAngleSpread = 28f;

    private bool isFlying;
    private bool hasSplit;
    private bool removedFromLists;
    private Vector2 flyDirection = Vector2.right;
    private float flyEndTime;

    protected override void Start()
    {
        base.Start();
        currentState = EnemyState.Patrol;
        moveInput = Vector2.zero;

        if (controller != null)
        {
            controller.Damaged += HandleDamaged;
            controller.IsFlying = true;
        }

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.angularVelocity = 0f;
            rb.velocity = Vector2.zero;
        }
    }

    protected override void Update()
    {
        if (IsCombatPaused())
        {
            StopBody();
            return;
        }

        if (!isFlying || hasSplit)
        {
            return;
        }

        if (Time.time >= flyEndTime)
        {
            SplitIntoInsects(false);
        }
    }

    private void FixedUpdate()
    {
        if (rb == null || !isFlying || hasSplit)
        {
            return;
        }

        rb.velocity = flyDirection * ResolveFlySpeed();
        rb.angularVelocity = 0f;
    }

    public void fly(Vector2 dir, float duration)
    {
        Fly(dir, duration);
    }

    public void Fly(Vector2 dir, float duration)
    {
        if (hasSplit)
        {
            return;
        }

        flyDirection = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
        flyEndTime = Time.time + Mathf.Max(0f, duration);
        isFlying = true;

        if (controller != null)
        {
            controller.IsFlying = true;
        }

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.angularVelocity = 0f;
            rb.velocity = flyDirection * ResolveFlySpeed();
        }

        if (duration <= 0f)
        {
            SplitIntoInsects(false);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isFlying || hasSplit)
        {
            return;
        }

        if (collision != null && TryHitPlayerAndSplit(collision.gameObject))
        {
            return;
        }

        if (collision != null && IsStopCollider(collision.collider))
        {
            SplitIntoInsects(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isFlying || hasSplit)
        {
            return;
        }

        if (TryHitPlayerAndSplit(other != null ? other.gameObject : null))
        {
            return;
        }

        if (IsStopCollider(other))
        {
            SplitIntoInsects(false);
        }
    }

    private void HandleDamaged(float damage)
    {
        if (!isFlying || hasSplit || controller == null)
        {
            return;
        }

        if (controller.CurrentHP <= 0f)
        {
            SplitIntoInsects(true);
        }
    }

    private void SplitIntoInsects(bool diedDuringFlight)
    {
        if (hasSplit)
        {
            return;
        }

        hasSplit = true;
        isFlying = false;
        StopBody();

        int count = diedDuringFlight ? 1 : Mathf.Max(1, splitCount);
        SpawnInsectWave(count);
        SelfDestruct();
    }

    private void SpawnInsectWave(int count)
    {
        if (InsectPrefab == null || count <= 0)
        {
            return;
        }

        Vector2 baseDirection = flyDirection.sqrMagnitude > 0.0001f ? flyDirection : Vector2.right;
        float randomBaseAngle = Random.Range(0f, 360f);
        Transform parent = transform.parent;

        for (int i = 0; i < count; i++)
        {
            float signedAttackAngle = GetSignedAttackAngle(i, count);
            Vector2 initialDirection = baseDirection;

            if (count > 1)
            {
                float angle = randomBaseAngle + i * (360f / count);
                float radians = angle * Mathf.Deg2Rad;
                initialDirection = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)).normalized;
            }

            GameObject spawned = Instantiate(InsectPrefab, transform.position, Quaternion.identity, parent);
            EnemyController spawnedController = spawned.GetComponent<EnemyController>();
            if (spawnedController != null)
            {
                spawnedController.MarkAsRuntimeSpawned();
            }

            MetaBallInsectLaunch launch = spawned.GetComponent<MetaBallInsectLaunch>();
            if (launch == null)
            {
                launch = spawned.AddComponent<MetaBallInsectLaunch>();
            }

            launch.Initialize(
                initialDirection,
                Mathf.Max(0f, splitInitialSpeed),
                Mathf.Max(0f, splitInitialDuration),
                playerTransform,
                signedAttackAngle);
        }
    }

    private float GetSignedAttackAngle(int index, int count)
    {
        if (count <= 1)
        {
            return 0f;
        }

        float t = index / (float)(count - 1);
        float spread = Mathf.Abs(splitAttackAngleSpread);
        return Mathf.Lerp(-spread, spread, t);
    }

    private bool IsStopCollider(Collider2D col)
    {
        if (col == null)
        {
            return false;
        }

        if (col.GetComponent<MCController>() != null || col.GetComponentInParent<MCController>() != null)
        {
            return false;
        }

        LayerMask effectiveMask = stopMask.value != 0 ? stopMask : LayerMask.GetMask("ground", "obstacle");
        return (effectiveMask.value & (1 << col.gameObject.layer)) != 0;
    }

    private bool TryHitPlayerAndSplit(GameObject otherObject)
    {
        if (otherObject == null || controller == null)
        {
            return false;
        }

        MCController player = otherObject.GetComponentInParent<MCController>();
        if (player == null)
        {
            return false;
        }

        controller.ApplyContactDamage(player.gameObject, transform.position);
        SplitIntoInsects(false);
        return true;
    }

    private float ResolveFlySpeed()
    {
        if (flySpeed > 0f)
        {
            return flySpeed;
        }

        if (controller != null && controller.MoveSpeed > 0f)
        {
            return controller.MoveSpeed;
        }

        return 8f;
    }

    private void StopBody()
    {
        moveInput = Vector2.zero;

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    private void SelfDestruct()
    {
        RemoveFromEnemyLists();
        Destroy(gameObject);
    }

    private void RemoveFromEnemyLists()
    {
        if (removedFromLists)
        {
            return;
        }

        removedFromLists = true;

        if (GameController.instance != null && controller != null)
        {
            GameController.instance.AllEnemies.Remove(controller);
        }

        Room room = transform.parent != null ? transform.parent.GetComponent<Room>() : null;
        if (room != null && controller != null)
        {
            room.Enemies.Remove(controller);
        }
    }

    private void OnDestroy()
    {
        if (controller != null)
        {
            controller.Damaged -= HandleDamaged;
        }

        RemoveFromEnemyLists();
    }
}

public class MeatBallAI : MetaBallAI
{
}

public class MetaBallInsectLaunch : MonoBehaviour
{
    private Vector2 initialDirection;
    private float initialSpeed;
    private float initialDuration;
    private Transform target;
    private float signedAngle;

    private bool initialized;
    private bool started;
    private float timer;

    private Rigidbody2D rb;
    private InsectAI insectAI;
    private EnemyController controller;

    public void Initialize(Vector2 direction, float speed, float duration, Transform chaseTarget, float angle)
    {
        initialDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        initialSpeed = Mathf.Max(0f, speed);
        initialDuration = Mathf.Max(0f, duration);
        target = chaseTarget;
        signedAngle = angle;
        initialized = true;
        TryStartInitialMove();
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        insectAI = GetComponent<InsectAI>();
        controller = GetComponent<EnemyController>();
    }

    private void Start()
    {
        TryStartInitialMove();
    }

    private void Update()
    {
        if (!started)
        {
            TryStartInitialMove();
            return;
        }

        timer += Time.deltaTime;
        if (timer < initialDuration)
        {
            return;
        }

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.gravityScale = 0f;
        }

        Transform chaseTarget = target;
        if (chaseTarget == null && GameController.instance != null && GameController.instance.mc != null)
        {
            chaseTarget = GameController.instance.mc.transform;
        }

        if (insectAI != null)
        {
            insectAI.enabled = true;
            insectAI.LaunchFromNest(chaseTarget, signedAngle);
        }

        Destroy(this);
    }

    private void TryStartInitialMove()
    {
        if (!initialized || started)
        {
            return;
        }

        started = true;
        timer = 0f;

        if (insectAI != null)
        {
            insectAI.enabled = false;
        }

        if (controller != null)
        {
            controller.IsFlying = true;
        }

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.angularVelocity = 0f;
            rb.velocity = initialDirection * initialSpeed;
        }
    }
}
