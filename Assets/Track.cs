using UnityEngine;

public class Track : MonoBehaviour
{
    public float MoveSpeed;
    public bool IsLeft;

    private SpriteRenderer spriteRenderer;
    private Animator animator;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = false;
        }
        animator.SetBool("isleft", IsLeft) ;

        UpdateAnimatorSpeed();

    }

    void Update()
    {
        UpdateAnimatorSpeed();
    }

    public Vector2 GetVelocity()
    {
        float direction = IsLeft ? -1f : 1f;
        return new Vector2(direction * Mathf.Abs(MoveSpeed), 0f);
    }

    private void UpdateAnimatorSpeed()
    {
        if (animator == null)
        {
            return;
        }

        float signedSpeed = IsLeft ? -Mathf.Abs(MoveSpeed) : Mathf.Abs(MoveSpeed);
        animator.SetFloat("Speed", signedSpeed / 3f);

    }
}
