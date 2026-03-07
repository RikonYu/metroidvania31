using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : Pickup
{
    public GameObject Bullet;
    // Start is called before the first frame update

    public override void PickupEffect()
    {
        GameController.instance.PickupBulllet(Bullet);
    }

}
