using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Utils
{
    public static IEnumerator ChainEnums(List<IEnumerator> ienumList)
    {
        foreach (var ienum in ienumList)
        {
            while (ienum.MoveNext())
            {
                yield return ienum.Current;
            }
        }
    }

    public static IEnumerator WaitForKSeconds(float k)
    {
        yield return new WaitForSeconds(k);
    }

    public static IEnumerator WaitUntilCondition(Func<bool> condition)
    {
        while (!condition())
        {
            yield return null;
        }

    }

    public static IEnumerator RunOnce(System.Action func)
    {
        func?.Invoke();
        yield return null;
    }

    public static void SnapToGround(GameObject obj, float snapDistance, LayerMask groundLayer)
    {
        if (obj == null)
        {
            return;
        }

        Collider2D[] colliders = obj.GetComponentsInChildren<Collider2D>(true);
        if (colliders == null || colliders.Length == 0)
        {
            RaycastHit2D fallbackHit = Physics2D.Raycast(obj.transform.position, Vector2.down, snapDistance, groundLayer);
            if (fallbackHit.collider != null)
            {
                obj.transform.position = new Vector3(obj.transform.position.x, fallbackHit.point.y, obj.transform.position.z);
            }
            return;
        }

        bool hasBounds = false;
        bool hasNonTrigger = false;
        Bounds bounds = new Bounds(obj.transform.position, Vector3.zero);

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D c = colliders[i];
            if (c == null || !c.enabled || c.isTrigger)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = c.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(c.bounds);
            }
            hasNonTrigger = true;
        }

        if (!hasNonTrigger)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider2D c = colliders[i];
                if (c == null || !c.enabled)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = c.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(c.bounds);
                }
            }
        }

        if (!hasBounds)
        {
            return;
        }

        float bottomOffset = obj.transform.position.y - bounds.min.y;
        float castDistance = snapDistance + Mathf.Max(0f, bounds.size.y) + 0.2f;
        Vector2 rayOrigin = new Vector2(obj.transform.position.x, bounds.max.y + 0.1f);
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, castDistance, groundLayer);
        if (hit.collider != null)
        {
            obj.transform.position = new Vector3(
                obj.transform.position.x,
                hit.point.y + bottomOffset,
                obj.transform.position.z);
        }
    }
}
