using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteTileFix : MonoBehaviour
{
    void Start()
    {

        var sr = GetComponent<SpriteRenderer>();
        var propBlock = new MaterialPropertyBlock();
        sr.GetPropertyBlock(propBlock);
        float isVertical = sr.size.y > sr.size.x ? 1f : 0f;
        propBlock.SetFloat(Shader.PropertyToID("_IsVertical"), isVertical);

        sr.SetPropertyBlock(propBlock);
    }
}
