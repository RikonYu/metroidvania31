using System.Collections;
using System.Collections.Generic;
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
            spriteRenderer.flipX = IsLeft;
        }

        UpdateAnimatorSpeed();
    }

    void Update()
    {
        UpdateAnimatorSpeed();
    }

    public Vector2 GetVelocity()
    {
        float direction = IsLeft ? -1f : 1f;
        return new Vector2(direction * MoveSpeed, 0f);
    }

    private void UpdateAnimatorSpeed()
    {
        if (animator == null)
        {
            return;
        }

        float signedSpeed = IsLeft ? -MoveSpeed : MoveSpeed;
        animator.SetFloat("Speed", signedSpeed / 3f);
    }
}
