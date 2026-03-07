using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoubleJump : Pickup
{
    public override void PickupEffect()
    {
        GameController.instance.CanDoubleJump = true;
        base.PickupEffect();
    }
}
