using UnityEngine;

public class LaserGun : Gun
{
    [SerializeField] LineRenderer beam;
    [SerializeField] float beamDuration = 0.08f;
    [SerializeField] float endFollowSpeed = 100f;

    Vector3 beamEnd;
    float beamTimer;
    bool beamActive;
    bool remote;

    float baseWidth;

    void Awake()
    {
        baseWidth = beam.widthMultiplier;
    }

    public override void Fire(bool useAmmo, int ammoLoss, bool canRecoil)
    {
        base.Fire(useAmmo, gunData.ammoPerShot, false);
    }

    public override void AlternateFire(bool useAmmo, int ammoLoss, bool canRecoil)
    {
        base.AlternateFire(useAmmo, gunData.ammoPerAltShot, true);
    }

    // Called by Gun.Fire on the owner
    protected override void OnShotFired(Vector3 start, Vector3 end)
    {
        remote = false;
        StartBeam(end);

        var m = GetManager();
        if (m) m.SendBeam(end);
    }

    // Called by PlayerGunManager.BeamRpc on other clients
    public void PlayRemoteBeam(Vector3 end)
    {
        remote = true;
        StartBeam(end);
    }

    void StartBeam(Vector3 end)
    {
        beamEnd = end;
        beamTimer = 0f;
        beamActive = true;

        beam.useWorldSpace = true;
        beam.positionCount = 2;
        beam.enabled = true;
    }

    void LateUpdate()
    {
        if (!beamActive) return;

        beamTimer += Time.deltaTime;
        if (beamTimer >= beamDuration)
        {
            beamActive = false;
            beam.enabled = false;
            return;
        }

        if (!remote)
        {
            Vector3 target = GetAimPoint();
            beamEnd = Vector3.Lerp(beamEnd, target, 1f - Mathf.Exp(-endFollowSpeed * Time.deltaTime));
        }

        beam.SetPosition(0, muzzle.position);
        beam.SetPosition(1, beamEnd);
        beam.widthMultiplier = baseWidth * (1f - beamTimer / beamDuration);
    }
}