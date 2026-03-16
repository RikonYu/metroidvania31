using System.Collections;
using UnityEngine;

public class ArmAI : EnemyAI
{
    [Header("References")]
    public Transform leftArm;
    public Transform rightArm;
    public Transform leftFireSpot;
    public Transform rightFireSpot;
    

    [Header("Timing")]
    public float restDuration = 1.2f;
    public float armMoveSpeed = 14f;
    public float fireInterval = 0.5f; // k seconds
    public bool engageOnlyInActiveRoom = true;

    [Header("Room Local Anchors")]
    public Vector2 leftAnchor = new Vector2(8.3f,5.5f);
    public Vector2 rightAnchor = new Vector2(23.5f, 12.5f);
    public Vector2 sweepMin = new Vector2(-7.5f, -7f);
    public Vector2 sweepMax = new Vector2(7.5f, 2f);

    public Sprite IAS;

    private Room ownerRoom;
    private Coroutine attackLoop;
    private int lastAttackIndex = -1;
    private Vector3 fixedBodyLocalPosition;

    private ArmRig leftRig;
    private ArmRig rightRig;

    private class ArmRig
    {
        public Transform root;
        public Transform fireSpot;
        public SpriteRenderer[] renderers;
        public Collider2D[] colliders;
        public Sprite[] defaultSprites;
        public bool[] defaultRendererEnabled;
        public bool[] defaultColliderEnabled;
        public Vector3 centerPos;
    }

    protected override void Start()
    {
        base.Start();
        currentState = EnemyState.Patrol;

        ownerRoom = GetComponentInParent<Room>();

        ResolveReferences();
        leftRig = BuildRig(leftArm, leftFireSpot);
        rightRig = BuildRig(rightArm, rightFireSpot);
        fixedBodyLocalPosition = transform.localPosition;

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.gravityScale = 0f;
        }

        if (leftRig == null || rightRig == null)
        {
            Debug.LogWarning("[ArmAI] Arm references are missing.", this);
        }

        if (attackLoop != null)
        {
            StopCoroutine(attackLoop);
        }
        attackLoop = StartCoroutine(AttackLoop());
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
            SetArmVisible(leftRig, false);
            SetArmVisible(rightRig, false);
            return;
        }

        UpdateCurrentPhase();

        transform.localPosition = fixedBodyLocalPosition;
        moveInput = Vector2.zero;
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }
    }

    protected override void Attack()
    {
    }

    private void OnDisable()
    {
        if (attackLoop != null)
        {
            StopCoroutine(attackLoop);
            attackLoop = null;
        }
    }

    private IEnumerator AttackLoop()
    {
        SetArmVisible(leftRig, false);
        SetArmVisible(rightRig, false);

        while (isActiveAndEnabled)
        {
            if (!CanEngage())
            {
                SetArmVisible(leftRig, false);
                SetArmVisible(rightRig, false);
                yield return null;
                continue;
            }

            int attackIndex = GetNextAttackIndex();
            yield return StartCoroutine(PlayAttack(attackIndex));

            // Between attack cycles both arms stay in the inactive (IAS) state.
            SetArmVisible(leftRig, false);
            SetArmVisible(rightRig, false);

            if (restDuration > 0f)
            {
                yield return new WaitForSeconds(restDuration);
            }
        }
    }

    private IEnumerator PlayAttack(int attackIndex)
    {
        Vector2 lAnchorRoom = leftAnchor;
        Vector2 rAnchorRoom = rightAnchor;
        float topYRoom = GetTopYLocal();
        float bottomYRoom = GetBottomYLocal();
        float leftXRoom = GetLeftXLocal();
        float rightXRoom = GetRightXLocal();

        Vector2 leftTopRoom = new Vector2(leftXRoom, topYRoom);
        Vector2 rightTopRoom = new Vector2(rightXRoom, topYRoom);
        Vector2 leftBottomRoom = new Vector2(leftXRoom, bottomYRoom);
        Vector2 rightBottomRoom = new Vector2(rightXRoom, bottomYRoom);

        switch (attackIndex)
        {
            case 0:
                // 1) Left arm: (5,10) -> top -> right -> down to (27,10), fires 45 down-right.
                SetArmVisible(leftRig, true);
                SetArmVisible(rightRig, false);
                yield return StartCoroutine(MoveArmToStartViaRails(leftRig, RoomLocalToArmLocal(leftRig, lAnchorRoom)));
                yield return StartCoroutine(MoveArm(leftRig, RoomLocalToArmLocal(leftRig, new Vector2(lAnchorRoom.x, topYRoom)), true, new Vector2(1f, -1f).normalized));
                yield return StartCoroutine(MoveArm(leftRig, RoomLocalToArmLocal(leftRig, new Vector2(rAnchorRoom.x, topYRoom)), true, new Vector2(1f, -1f).normalized));
                yield return StartCoroutine(MoveArm(leftRig, RoomLocalToArmLocal(leftRig, rAnchorRoom), true, new Vector2(1f, -1f).normalized));
                yield return StartCoroutine(ReturnArmToCenterViaRails(leftRig));
                break;

            case 1:
                // 2) Right mirrored of attack 1.
                SetArmVisible(leftRig, false);
                SetArmVisible(rightRig, true);
                yield return StartCoroutine(MoveArmToStartViaRails(rightRig, RoomLocalToArmLocal(rightRig, rAnchorRoom)));
                yield return StartCoroutine(MoveArm(rightRig, RoomLocalToArmLocal(rightRig, new Vector2(rAnchorRoom.x, topYRoom)), true, new Vector2(-1f, -1f).normalized));
                yield return StartCoroutine(MoveArm(rightRig, RoomLocalToArmLocal(rightRig, new Vector2(lAnchorRoom.x, topYRoom)), true, new Vector2(-1f, -1f).normalized));
                yield return StartCoroutine(MoveArm(rightRig, RoomLocalToArmLocal(rightRig, lAnchorRoom), true, new Vector2(-1f, -1f).normalized));
                yield return StartCoroutine(ReturnArmToCenterViaRails(rightRig));
                break;

            case 2:
                // 3) Left arm top sweep, fires straight down.
                SetArmVisible(leftRig, true);
                SetArmVisible(rightRig, false);
                yield return StartCoroutine(MoveArmToStartViaRails(leftRig, RoomLocalToArmLocal(leftRig, leftTopRoom)));
                yield return StartCoroutine(MoveArm(leftRig, RoomLocalToArmLocal(leftRig, rightTopRoom), true, Vector2.down));
                yield return StartCoroutine(ReturnArmToCenterViaRails(leftRig));
                break;

            case 3:
                // 4) Mirrored top sweep with right arm.
                SetArmVisible(leftRig, false);
                SetArmVisible(rightRig, true);
                yield return StartCoroutine(MoveArmToStartViaRails(rightRig, RoomLocalToArmLocal(rightRig, rightTopRoom)));
                yield return StartCoroutine(MoveArm(rightRig, RoomLocalToArmLocal(rightRig, leftTopRoom), true, Vector2.down));
                yield return StartCoroutine(ReturnArmToCenterViaRails(rightRig));
                break;

            case 4:
                // 5) Left: bottom->top (fire right), Right: top->bottom (fire left).
                SetArmVisible(leftRig, true);
                SetArmVisible(rightRig, true);
                yield return StartCoroutine(MoveTwoArmsToStartViaRails(
                    leftRig, RoomLocalToArmLocal(leftRig, leftBottomRoom),
                    rightRig, RoomLocalToArmLocal(rightRig, rightTopRoom),
                    false, Vector2.zero, false, Vector2.zero));
                yield return StartCoroutine(MoveTwoArms(
                    leftRig, RoomLocalToArmLocal(leftRig, leftTopRoom),
                    rightRig, RoomLocalToArmLocal(rightRig, rightBottomRoom),
                    true, Vector2.right, true, Vector2.left));
                yield return StartCoroutine(ReturnTwoArmsToCenterViaRails(leftRig, rightRig));
                break;

            default:
                // 6) Mirror of attack 5.
                SetArmVisible(leftRig, true);
                SetArmVisible(rightRig, true);
                yield return StartCoroutine(MoveTwoArmsToStartViaRails(
                    leftRig, RoomLocalToArmLocal(leftRig, leftTopRoom),
                    rightRig, RoomLocalToArmLocal(rightRig, rightBottomRoom),
                    false, Vector2.zero, false, Vector2.zero));
                yield return StartCoroutine(MoveTwoArms(
                    leftRig, RoomLocalToArmLocal(leftRig, leftBottomRoom),
                    rightRig, RoomLocalToArmLocal(rightRig, rightTopRoom),
                    true, Vector2.right, true, Vector2.left));
                yield return StartCoroutine(ReturnTwoArmsToCenterViaRails(leftRig, rightRig));
                break;
        }

        SetArmVisible(leftRig, true);
        SetArmVisible(rightRig, true);
    }

    private IEnumerator MoveArm(ArmRig rig, Vector2 target, bool canShoot, Vector2 fireDir)
    {
        if (rig == null || rig.root == null)
        {
            yield break;
        }

        float speed = Mathf.Max(0.01f, armMoveSpeed);
        float timer = 0f;
        float interval = Mathf.Max(0.01f, fireInterval);

        while (((Vector2)rig.root.localPosition - target).sqrMagnitude > 0.0001f)
        {
            Vector2 next = Vector2.MoveTowards(rig.root.localPosition, target, speed * Time.deltaTime);
            SetArmLocalPosition(rig, next);

            if (canShoot)
            {
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    FireFrom(rig, fireDir);
                    timer += interval;
                }
            }

            yield return null;
        }

        SetArmLocalPosition(rig, target);
    }

    private IEnumerator MoveTwoArms(
        ArmRig a, Vector2 targetA,
        ArmRig b, Vector2 targetB,
        bool shootA, Vector2 dirA,
        bool shootB, Vector2 dirB)
    {
        float speed = Mathf.Max(0.01f, armMoveSpeed);
        float interval = Mathf.Max(0.01f, fireInterval);
        float timerA = 0f;
        float timerB = 0f;

        while (true)
        {
            bool reachedA = a == null || a.root == null || ((Vector2)a.root.localPosition - targetA).sqrMagnitude <= 0.0001f;
            bool reachedB = b == null || b.root == null || ((Vector2)b.root.localPosition - targetB).sqrMagnitude <= 0.0001f;
            if (reachedA && reachedB)
            {
                break;
            }

            if (!reachedA)
            {
                Vector2 nextA = Vector2.MoveTowards(a.root.localPosition, targetA, speed * Time.deltaTime);
                SetArmLocalPosition(a, nextA);
                if (shootA)
                {
                    timerA -= Time.deltaTime;
                    if (timerA <= 0f)
                    {
                        FireFrom(a, dirA);
                        timerA += interval;
                    }
                }
            }

            if (!reachedB)
            {
                Vector2 nextB = Vector2.MoveTowards(b.root.localPosition, targetB, speed * Time.deltaTime);
                SetArmLocalPosition(b, nextB);
                if (shootB)
                {
                    timerB -= Time.deltaTime;
                    if (timerB <= 0f)
                    {
                        FireFrom(b, dirB);
                        timerB += interval;
                    }
                }
            }

            yield return null;
        }

        if (a != null && a.root != null)
        {
            SetArmLocalPosition(a, targetA);
        }
        if (b != null && b.root != null)
        {
            SetArmLocalPosition(b, targetB);
        }
    }

    private IEnumerator ReturnArmToCenterViaRails(ArmRig rig)
    {
        if (rig == null || rig.root == null)
        {
            yield break;
        }

        Vector2 current = rig.root.localPosition;
        Vector2 center = rig.centerPos;
        float sideX = GetSideRailX(rig);
        float topY = GetTopYLocal();

        yield return StartCoroutine(MoveArm(rig, new Vector2(sideX, current.y), false, Vector2.zero));
        yield return StartCoroutine(MoveArm(rig, new Vector2(sideX, topY), false, Vector2.zero));
        yield return StartCoroutine(MoveArm(rig, new Vector2(center.x, topY), false, Vector2.zero));
        yield return StartCoroutine(MoveArm(rig, center, false, Vector2.zero));
    }

    private IEnumerator MoveArmToStartViaRails(ArmRig rig, Vector2 target)
    {
        if (rig == null || rig.root == null)
        {
            yield break;
        }

        Vector2 current = rig.root.localPosition;
        float sideX = GetSideRailX(rig);
        float topY = GetTopYLocal();

        yield return StartCoroutine(MoveArm(rig, new Vector2(current.x, topY), false, Vector2.zero));
        yield return StartCoroutine(MoveArm(rig, new Vector2(sideX, topY), false, Vector2.zero));
        yield return StartCoroutine(MoveArm(rig, new Vector2(sideX, target.y), false, Vector2.zero));
        yield return StartCoroutine(MoveArm(rig, target, false, Vector2.zero));
    }

    private IEnumerator MoveTwoArmsToStartViaRails(
        ArmRig left, Vector2 leftTarget,
        ArmRig right, Vector2 rightTarget,
        bool shootLeft, Vector2 leftDir,
        bool shootRight, Vector2 rightDir)
    {
        float leftRailX = GetLeftXLocal();
        float rightRailX = GetRightXLocal();
        float topY = GetTopYLocal();

        Vector2 leftCurrent = left != null && left.root != null ? (Vector2)left.root.localPosition : Vector2.zero;
        Vector2 rightCurrent = right != null && right.root != null ? (Vector2)right.root.localPosition : Vector2.zero;

        yield return StartCoroutine(MoveTwoArms(left, new Vector2(leftCurrent.x, topY), right, new Vector2(rightCurrent.x, topY), false, Vector2.zero, false, Vector2.zero));
        yield return StartCoroutine(MoveTwoArms(left, new Vector2(leftRailX, topY), right, new Vector2(rightRailX, topY), false, Vector2.zero, false, Vector2.zero));
        yield return StartCoroutine(MoveTwoArms(left, new Vector2(leftRailX, leftTarget.y), right, new Vector2(rightRailX, rightTarget.y), false, Vector2.zero, false, Vector2.zero));
        yield return StartCoroutine(MoveTwoArms(left, leftTarget, right, rightTarget, shootLeft, leftDir, shootRight, rightDir));
    }

    private IEnumerator ReturnTwoArmsToCenterViaRails(ArmRig left, ArmRig right)
    {
        float leftRailX = GetLeftXLocal();
        float rightRailX = GetRightXLocal();
        float topY = GetTopYLocal();

        Vector2 leftCurrent = left != null && left.root != null ? (Vector2)left.root.localPosition : Vector2.zero;
        Vector2 rightCurrent = right != null && right.root != null ? (Vector2)right.root.localPosition : Vector2.zero;
        Vector2 leftCenter = left != null ? (Vector2)left.centerPos : Vector2.zero;
        Vector2 rightCenter = right != null ? (Vector2)right.centerPos : Vector2.zero;

        yield return StartCoroutine(MoveTwoArms(left, new Vector2(leftRailX, leftCurrent.y), right, new Vector2(rightRailX, rightCurrent.y), false, Vector2.zero, false, Vector2.zero));
        yield return StartCoroutine(MoveTwoArms(left, new Vector2(leftRailX, topY), right, new Vector2(rightRailX, topY), false, Vector2.zero, false, Vector2.zero));
        yield return StartCoroutine(MoveTwoArms(left, new Vector2(leftCenter.x, topY), right, new Vector2(rightCenter.x, topY), false, Vector2.zero, false, Vector2.zero));
        yield return StartCoroutine(MoveTwoArms(left, leftCenter, right, rightCenter, false, Vector2.zero, false, Vector2.zero));
    }

    private void FireFrom(ArmRig rig, Vector2 direction)
    {
        if (rig == null || rig.root == null || Bullet == null || GameController.instance == null)
        {
            return;
        }

        Vector3 firePos = rig.fireSpot != null ? rig.fireSpot.position : rig.root.position;
        GameController.instance.FireBullet(Bullet, firePos, direction, true);
    }

    private bool CanEngage()
    {
        if (IsCombatPaused())
        {
            return false;
        }

        if (!engageOnlyInActiveRoom || GameController.instance == null || ownerRoom == null)
        {
            return true;
        }

        return GameController.instance.ActiveRoom == ownerRoom;
    }

    private int GetNextAttackIndex()
    {
        int next = Random.Range(0, 6);
        if (next == lastAttackIndex)
        {
            next = (next + Random.Range(1, 6)) % 6;
        }

        lastAttackIndex = next;
        return next;
    }

    private void SetArmVisible(ArmRig rig, bool visible)
    {
        if (rig == null)
        {
            return;
        }

        if (rig.renderers != null)
        {
            for (int i = 0; i < rig.renderers.Length; i++)
            {
                SpriteRenderer renderer = rig.renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                bool defaultEnabled = rig.defaultRendererEnabled != null && i < rig.defaultRendererEnabled.Length
                    ? rig.defaultRendererEnabled[i]
                    : true;

                if (visible)
                {
                    renderer.enabled = defaultEnabled;
                    if (rig.defaultSprites != null && i < rig.defaultSprites.Length && rig.defaultSprites[i] != null)
                    {
                        renderer.sprite = rig.defaultSprites[i];
                    }
                }
                else
                {
                    if (defaultEnabled && IAS != null)
                    {
                        renderer.enabled = true;
                        renderer.sprite = IAS;
                    }
                    else
                    {
                        renderer.enabled = defaultEnabled;
                    }
                }
            }
        }

        if (rig.colliders != null)
        {
            for (int i = 0; i < rig.colliders.Length; i++)
            {
                Collider2D collider = rig.colliders[i];
                if (collider == null)
                {
                    continue;
                }

                bool defaultEnabled = rig.defaultColliderEnabled != null && i < rig.defaultColliderEnabled.Length
                    ? rig.defaultColliderEnabled[i]
                    : true;
                collider.enabled = visible ? defaultEnabled : false;
            }
        }
    }

    private void SetArmLocalPosition(ArmRig rig, Vector2 localPosition)
    {
        if (rig == null || rig.root == null)
        {
            return;
        }

        Vector3 prevWorld = rig.root.position;
        Vector3 currentLocal = rig.root.localPosition;
        rig.root.localPosition = new Vector3(localPosition.x, localPosition.y, currentLocal.z);

        if (rig.fireSpot != null && !IsFireSpotAttachedToArm(rig))
        {
            Vector3 worldDelta = rig.root.position - prevWorld;
            rig.fireSpot.position += worldDelta;
        }
    }

    private bool IsFireSpotAttachedToArm(ArmRig rig)
    {
        if (rig == null || rig.root == null || rig.fireSpot == null)
        {
            return false;
        }

        return rig.fireSpot == rig.root || rig.fireSpot.IsChildOf(rig.root);
    }

    private void ResolveReferences()
    {
        if (leftArm == null)
        {
            leftArm = FindChildByAlias(transform, "leftarm", "left_arm", "arm_l", "larm", "left");
        }
        if (rightArm == null)
        {
            rightArm = FindChildByAlias(transform, "rightarm", "right_arm", "arm_r", "rarm", "right");
        }

        if (leftFireSpot == null && leftArm != null)
        {
            leftFireSpot = leftArm.Find("firespot");
        }
        if (rightFireSpot == null && rightArm != null)
        {
            rightFireSpot = rightArm.Find("firespot");
        }
    }

    private ArmRig BuildRig(Transform arm, Transform fireSpot)
    {
        if (arm == null)
        {
            return null;
        }

        ArmRig rig = new ArmRig();
        rig.root = arm;
        rig.fireSpot = fireSpot;
        rig.renderers = arm.GetComponentsInChildren<SpriteRenderer>(true);
        rig.colliders = arm.GetComponentsInChildren<Collider2D>(true);
        rig.defaultSprites = new Sprite[rig.renderers.Length];
        rig.defaultRendererEnabled = new bool[rig.renderers.Length];
        for (int i = 0; i < rig.renderers.Length; i++)
        {
            if (rig.renderers[i] == null)
            {
                continue;
            }

            rig.defaultSprites[i] = rig.renderers[i].sprite;
            rig.defaultRendererEnabled[i] = rig.renderers[i].enabled;
        }

        rig.defaultColliderEnabled = new bool[rig.colliders.Length];
        for (int i = 0; i < rig.colliders.Length; i++)
        {
            if (rig.colliders[i] == null)
            {
                continue;
            }

            rig.defaultColliderEnabled[i] = rig.colliders[i].enabled;
            ArmContactRelay relay = rig.colliders[i].GetComponent<ArmContactRelay>();
            if (relay == null)
            {
                relay = rig.colliders[i].gameObject.AddComponent<ArmContactRelay>();
            }

            relay.SetOwner(controller);
        }
        rig.centerPos = arm.localPosition;
        return rig;
    }

    private Vector2 RoomLocalToArmLocal(ArmRig rig, Vector2 roomLocal)
    {
        // All configured points are treated as local coordinates
        // in each arm's parent space.
        return roomLocal;
    }

    private float GetTopYLocal()
    {
        return Mathf.Max(sweepMin.y, sweepMax.y);
    }

    private float GetBottomYLocal()
    {
        return Mathf.Min(sweepMin.y, sweepMax.y);
    }

    private float GetLeftXLocal()
    {
        return Mathf.Min(sweepMin.x, sweepMax.x);
    }

    private float GetRightXLocal()
    {
        return Mathf.Max(sweepMin.x, sweepMax.x);
    }

    private float GetSideRailX(ArmRig rig)
    {
        if (rig == null)
        {
            return GetLeftXLocal();
        }

        if (rig == rightRig)
        {
            return GetRightXLocal();
        }

        if (rig == leftRig)
        {
            return GetLeftXLocal();
        }

        float leftX = GetLeftXLocal();
        float rightX = GetRightXLocal();
        return Mathf.Abs(rig.root.localPosition.x - leftX) <= Mathf.Abs(rig.root.localPosition.x - rightX) ? leftX : rightX;
    }

    private Transform FindChildByAlias(Transform root, params string[] aliases)
    {
        if (root == null || aliases == null)
        {
            return null;
        }

        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            string lower = all[i].name.ToLowerInvariant();
            for (int j = 0; j < aliases.Length; j++)
            {
                if (lower == aliases[j])
                {
                    return all[i];
                }
            }
        }

        return null;
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
}

public class ArmContactRelay : MonoBehaviour
{
    [SerializeField] private EnemyController owner;

    public void SetOwner(EnemyController ownerController)
    {
        owner = ownerController;
    }

    private void Awake()
    {
        if (owner == null)
        {
            owner = GetComponentInParent<EnemyController>();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (owner == null)
        {
            return;
        }

        owner.ApplyContactDamage(collision.gameObject, transform.position);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (owner == null)
        {
            return;
        }

        owner.ApplyContactDamage(other.gameObject, transform.position);
    }
}
