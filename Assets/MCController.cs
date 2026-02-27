using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class MCController : MonoBehaviour
{
    public float MaxHealth, CurrentHealth;

    [Header("Energy & Weapon System")]
    public float MaxEnergy = 100f;
    public float CurrentEnergy;
    public float EnergyRegenRate = 15f;
    public GameObject[] WeaponList;
    public GameObject BulletPrefab;
    private int currentWeaponIndex = 0;
    private float FireCoolDown;
    private float firecd;

    private bool isCharging;
    private float currentChargeTime;

    public bool IsInSpace;
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;

    [Header("Jump & Gravity (Modified)")]
    [SerializeField] private float jumpForce = 16f;
    [SerializeField] private float baseGravityScale = 4f;
    [SerializeField] private float baseSpaceGravityScale = 0.5f;
    [SerializeField] private float maxFallSpeed = 25f;

    [Header("Game Feel")]
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.1f;

    [Header("Double Jump")]
    [SerializeField] private float doubleJumpCooldown = 0.2f;
    private bool hasDoubleJumped;
    private float timeSinceLastJump;

    [Header("Detection")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckWidth = 0.45f;
    [SerializeField] private float groundCheckHeight = 0.1f;
    [SerializeField] private LayerMask groundLayer, safeGroundLayer;

    [Header("Respawn System")]
    [SerializeField] private Vector2 respawnOffset = new Vector2(0f, 0.5f);
    [SerializeField] private float safeSlopeThreshold = 0.5f;

    private Vector2 slopeNormal;
    private bool isOnSlope;
    private bool isJumping;
    private Rigidbody2D rb;
    private float horizontalInput;
    private bool isGrounded;
    private bool wasGrounded;
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private bool isStunned;
    private Vector3 lastSafePosition;
    private Platform currentPlatform;

    float freezeCounter;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        Collider2D coll = GetComponent<Collider2D>();
        if (coll != null)
        {
            PhysicsMaterial2D noFrictionMat = new PhysicsMaterial2D("NoFriction");
            noFrictionMat.friction = 0f;
            noFrictionMat.bounciness = 0f;
            coll.sharedMaterial = noFrictionMat;
        }

        lastSafePosition = transform.position;
        firecd = 0f;
        CurrentHealth = MaxHealth;
        CurrentEnergy = MaxEnergy;
        UIController.instance.SetHP(CurrentHealth, MaxHealth);

        if (WeaponList != null && WeaponList.Length > 0)
        {
            SwapBullet(WeaponList[0]);
        }
        else if (BulletPrefab != null)
        {
            SwapBullet(BulletPrefab);
        }
    }

    public void SwapBullet(GameObject NewBulletPrefab)
    {
        BulletPrefab = NewBulletPrefab;
        if (BulletPrefab != null)
        {
            FireCoolDown = BulletPrefab.GetComponent<Bullet>().CoolDown;
        }
    }

    private void HandleWeaponSwitch()
    {
        if (WeaponList == null || WeaponList.Length <= 1) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            if (scroll > 0f)
                currentWeaponIndex = (currentWeaponIndex + 1) % WeaponList.Length;
            else
                currentWeaponIndex = (currentWeaponIndex - 1 + WeaponList.Length) % WeaponList.Length;

            SwapBullet(WeaponList[currentWeaponIndex]);
        }
    }

    private void HandleWeaponsAndEnergy()
    {
        if (BulletPrefab == null) return;

        Bullet currentBulletScript = BulletPrefab.GetComponent<Bullet>();
        if (currentBulletScript == null) return;

        float energyCost = currentBulletScript.EnergyCost;
        bool isChargeWeapon = BulletPrefab.GetComponent<ChargeBullet>() != null;
        bool isFlameWeapon = BulletPrefab.GetComponent<FlameBullet>() != null;

        bool isFiringInput = Input.GetMouseButton(0);

        if (isChargeWeapon)
        {
            ChargeBullet cbScript = BulletPrefab.GetComponent<ChargeBullet>();

            if (Input.GetMouseButtonDown(0) && CurrentEnergy >= energyCost && firecd <= 0f)
            {
                isCharging = true;
                currentChargeTime = 0f;
            }

            if (isCharging && isFiringInput)
            {
                currentChargeTime += Time.deltaTime;
                if (currentChargeTime > cbScript.MaxChargeTime)
                {
                    currentChargeTime = cbScript.MaxChargeTime;
                }
            }

            if (Input.GetMouseButtonUp(0) && isCharging)
            {
                isCharging = false;
                FireChargeWeapon(currentChargeTime, cbScript.MaxChargeTime, energyCost);
            }
        }
        else
        {
            if (isCharging) isCharging = false;

            if (isFiringInput && firecd <= 0f && CurrentEnergy >= energyCost)
            {
                FireNormalWeapon(energyCost);
            }
        }

        bool canRecover = false;

        if (isFlameWeapon)
        {
            if (!isFiringInput)
            {
                canRecover = true;
            }
        }
        else
        {
            if (CurrentEnergy < energyCost)
            {
                canRecover = true;
            }
        }

        if (canRecover)
        {
            CurrentEnergy += EnergyRegenRate * Time.deltaTime;
            if (CurrentEnergy > MaxEnergy)
            {
                CurrentEnergy = MaxEnergy;
            }
        }
    }

    private void FireNormalWeapon(float cost)
    {
        CurrentEnergy -= cost;
        firecd = FireCoolDown;

        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        worldPosition.z = 0;
        GameController.instance.FireBullet(BulletPrefab, transform.position, worldPosition - transform.position, false);
    }

    private void FireChargeWeapon(float chargeTime, float maxChargeTime, float cost)
    {
        CurrentEnergy -= cost;
        firecd = FireCoolDown;

        float ratio = Mathf.Clamp01(chargeTime / maxChargeTime);
        ChargeBullet.NextChargeRatio = ratio;

        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        worldPosition.z = 0;
        GameController.instance.FireBullet(BulletPrefab, transform.position, worldPosition - transform.position, false);
    }

    void Update()
    {
        if (isStunned) return;
        if (freezeCounter > 0f) freezeCounter -= Time.deltaTime;
        firecd -= Time.deltaTime;

        timeSinceLastJump += Time.deltaTime;

        if (freezeCounter <= 0f)
        {
            horizontalInput = Input.GetAxisRaw("Horizontal");

            if (Input.GetKeyDown(KeyCode.E))
                GameController.instance.InteractingObject?.Interact();

            if (Input.GetKeyDown(KeyCode.M))
                UIController.instance.ToggleMinimap();

            if (Input.GetKeyDown(KeyCode.W))
            {
                jumpBufferCounter = jumpBufferTime;

                if (coyoteTimeCounter <= 0f && GameController.instance.CanDoubleJump && !hasDoubleJumped && timeSinceLastJump >= doubleJumpCooldown)
                {
                    PerformDoubleJump();
                    jumpBufferCounter = 0f;
                }
            }
            else
            {
                jumpBufferCounter -= Time.deltaTime;
            }

            if (Input.GetKeyUp(KeyCode.W))
            {
                if (rb.velocity.y > 0)
                {
                    rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * 0.5f);
                }
            }

            HandleWeaponSwitch();
            HandleWeaponsAndEnergy();
        }

        if (isGrounded)
            coyoteTimeCounter = coyoteTime;
        else
            coyoteTimeCounter -= Time.deltaTime;

        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
            PerformJump();
    }

    void FixedUpdate()
    {
        CheckGround();
        UpdateSafePosition();

        if (isStunned)
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            ModifyPhysics();
            return;
        }

        Move();
        ModifyPhysics();
    }

    private void Move()
    {
        float targetVelX = horizontalInput * moveSpeed;
        float newVelX = targetVelX;
        float newVelY = rb.velocity.y;

        Vector2 platformVel = Vector2.zero;
        if (isGrounded && !isJumping && currentPlatform != null)
        {
            platformVel = currentPlatform.GetVelocity();
            newVelX += platformVel.x;
        }

        if (isGrounded && !isJumping)
        {
            if (isOnSlope)
            {
                newVelY = targetVelX * (-slopeNormal.x / slopeNormal.y);
            }
            else
            {
                if (currentPlatform != null)
                {
                    newVelY = platformVel.y;
                }
                else if (newVelY > 0)
                {
                    newVelY = 0f;
                }
            }
        }
        else if (wasGrounded && !isGrounded && !isJumping)
        {
            if (newVelY > 0)
            {
                newVelY = 0f;
            }
        }

        rb.velocity = new Vector2(newVelX, newVelY);
    }

    private void PerformJump()
    {
        isJumping = true;
        timeSinceLastJump = 0f;
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        jumpBufferCounter = 0f;
        coyoteTimeCounter = 0f;
    }

    private void PerformDoubleJump()
    {
        isJumping = true;
        hasDoubleJumped = true;
        timeSinceLastJump = 0f;
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
    }

    private void ModifyPhysics()
    {
        if (isGrounded && !isJumping)
        {
            rb.gravityScale = 0f;
        }
        else
        {
            rb.gravityScale = IsInSpace ? baseSpaceGravityScale : baseGravityScale;

            if (rb.velocity.y < -maxFallSpeed)
            {
                rb.velocity = new Vector2(rb.velocity.x, -maxFallSpeed);
            }
        }
    }

    public void Freeze(float t)
    {
        freezeCounter = t;
        rb.velocity = Vector2.zero;
    }

    private void CheckGround()
    {
        wasGrounded = isGrounded;

        float safeWidth = Mathf.Max(0.01f, groundCheckWidth - 0.05f);
        Vector2 size = new Vector2(safeWidth, groundCheckHeight);

        Collider2D hitCollider = Physics2D.OverlapBox(groundCheck.position, size, 0f, groundLayer);

        if (hitCollider != null)
        {
            isGrounded = true;
            hasDoubleJumped = false;
            currentPlatform = hitCollider.GetComponent<Platform>();
            if (rb.velocity.y <= 0.1f)
            {
                isJumping = false;
            }
        }
        else
        {
            isGrounded = false;
            currentPlatform = null;
        }

        CheckSlope();
    }

    private void CheckSlope()
    {
        Vector2 rayStart = (Vector2)groundCheck.position + Vector2.up * 0.1f;
        float rayLength = 0.4f;

        RaycastHit2D hit = Physics2D.Raycast(rayStart, Vector2.down, rayLength, groundLayer);

        if (hit)
        {
            slopeNormal = hit.normal;
            float slopeAngle = Vector2.Angle(slopeNormal, Vector2.up);
            isOnSlope = (slopeAngle > 0.1f && slopeAngle < 85f);
        }
        else
        {
            slopeNormal = Vector2.up;
            isOnSlope = false;
        }
    }

    private void UpdateSafePosition()
    {
        if (isGrounded && rb.velocity.y <= 0.1f)
        {
            RaycastHit2D hit = Physics2D.Raycast(groundCheck.position, Vector2.down, 0.5f, safeGroundLayer);

            if (hit.collider != null)
            {
                if (hit.normal.y > safeSlopeThreshold)
                {
                    lastSafePosition = hit.point + respawnOffset;
                }
            }
        }
    }

    public void Respawn(bool isDropped)
    {
        firecd = 0f;
        rb.velocity = Vector2.zero;
        isStunned = false;

        if (isDropped)
            transform.position = lastSafePosition;
        else
        {
            CurrentHealth = MaxHealth;
            transform.position = GameController.instance.LastCamp.transform.position;
            UIController.instance.SetHP(CurrentHealth, MaxHealth);
        }
    }

    public void Hurt(float dmg)
    {
        CurrentHealth -= dmg;
        if (CurrentHealth <= 0f) CurrentHealth = 0f;
        UIController.instance.SetHP(CurrentHealth, MaxHealth);
        if (CurrentHealth <= 0f)
            GameController.instance.Die(false);
    }

    private void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            float safeWidth = Mathf.Max(0.01f, groundCheckWidth - 0.05f);
            Vector3 boxSize = new Vector3(safeWidth, groundCheckHeight, 1f);
            Gizmos.DrawWireCube(groundCheck.position + Vector3.down * 0.025f, boxSize);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(groundCheck.position + Vector3.up * 0.1f, groundCheck.position + Vector3.down * 0.3f);
        }
    }
}