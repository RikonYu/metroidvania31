using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlidePickup : Pickup
{

    public override void PickupEffect()
    {
        GameController.instance.CanSlide = true;
    }
}
