using UnityEngine;

public class TestGun : Gun
{
    public override void Fire(bool useAmmo, int ammoLoss, bool canRecoil)
    {
        base.Fire(useAmmo, gunData.ammoPerShot);
    }
}
