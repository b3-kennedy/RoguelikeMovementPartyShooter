using UnityEngine;

public class Shotgun : Gun
{
    Rigidbody rb;
    public float recoilForce = 5f;
    public override void Fire(bool useAmmo, int ammoLoss, bool canRecoil = true)
    {
        base.Fire(useAmmo, ammoLoss, canRecoil);

        var controller = GetPlayerController();

        if (controller.IsGrounded() && !controller.IsSliding()) return;

        Camera cam = controller.GetComponent<Interact>().cam;
        if (cam == null) return;

        controller.ApplyImpulse(-cam.transform.forward * recoilForce);
    }
}
