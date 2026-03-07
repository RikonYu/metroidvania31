using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoxColliderFix : MonoBehaviour
{
    private void Start()
    {
        gameObject.GetComponent<BoxCollider2D>().size = gameObject.GetComponent<SpriteRenderer>().size;
    }
}
