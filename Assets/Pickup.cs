using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pickup : MonoBehaviour
{


    public virtual void PickupEffect()
    {

    }

    // Update is called once per frame
    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.GetComponent<MCController>() != null)
        {
            PickupEffect();
            Destroy(gameObject);
        }
    }
}
