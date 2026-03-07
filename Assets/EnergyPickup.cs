using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnergyPickup : Pickup
{
    public float Amount;

    public override void PickupEffect()
    {
        GameController.instance.mc.MaxEnergy += Amount;
        GameController.instance.mc.CurrentEnergy += Amount;
    }

}
