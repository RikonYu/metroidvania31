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
        RaycastHit2D hit = Physics2D.Raycast(obj.transform.position, Vector2.down, snapDistance, groundLayer);
        if (hit.collider != null)
        {
            obj.transform.position = new Vector3(hit.point.x, hit.point.y + obj.GetComponent<BoxCollider2D>().size.y / 2f, obj.transform.position.z);
        }
    }
}
