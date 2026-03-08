using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Track : MonoBehaviour
{
    public float MoveSpeed;
    public bool IsLeft;

    private void Start()
    {
        GetComponent<SpriteRenderer>().flipX = IsLeft;
    }
    void Update()
    {

    }

    public Vector2 GetVelocity()
    {
        float direction = IsLeft ? -1f : 1f;
        return new Vector2(direction * MoveSpeed, 0f);
    }
}