using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HPPickup : Pickup
{
    public float Amount;

    public override void PickupEffect()
    {
        GameController.instance.mc.MaxHealth += Amount;
        GameController.instance.mc.CurrentHealth += Amount;
        UIController.instance.SetHP(GameController.instance.mc.CurrentHealth, GameController.instance.mc.MaxHealth);
    }
}
