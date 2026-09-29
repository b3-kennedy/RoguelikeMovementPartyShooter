using UnityEngine;

public class RecoilAnimation : MonoBehaviour
{
    Quaternion defaultRot;
    PlayerGunManager playerGunManager;

    float horizontalRecoil;
    float verticalRecoil;

    float currentHorizontalRecoil;
    float currentVerticalRecoil;
    float snappiness;
    float recoilRecoveryDampening;

    Vector3 targetRot;
    Vector3 currentRot;
    Gun gun;

    void Start()
    {
        defaultRot = transform.localRotation;
    }

    public void SetPlayerGunManager(PlayerGunManager manager)
    {
        playerGunManager = manager;
    }

    public void OnPickup()
    {
        gun = playerGunManager.GetGun();

        GunData gunData = gun.gunData;

        horizontalRecoil = gunData.horizontalRecoil;
        verticalRecoil = gunData.verticalRecoil;
        recoilRecoveryDampening = gunData.recoilRecoveryDampening;
        snappiness = gunData.snappiness;
    }

    public void Recoil()
    {
        targetRot += new Vector3(-verticalRecoil, Random.Range(-horizontalRecoil, horizontalRecoil), 0f);
    }

    void Update()
    {
        if (!playerGunManager) return;
        if (!gun) return;

        targetRot = Vector3.Lerp(targetRot, Vector3.zero, recoilRecoveryDampening * Time.deltaTime);
        currentRot = Vector3.Slerp(currentRot, targetRot,  snappiness * Time.deltaTime);
        transform.localRotation = Quaternion.Euler(currentRot);
    }
}
