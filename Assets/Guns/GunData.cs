using UnityEngine;

[CreateAssetMenu(fileName = "Gun", menuName = "Weapons/Gun")]
public class GunData : ScriptableObject
{

    public enum GunType { PISTOL, RIFLE, SHOTGUN, SNIPER, SMG, LASER};
    public GunType gunType;
    public int ammoPerShot = 1;
    public int ammoPerAltShot = 1;
    public string gunName;
    public enum FireType {SINGLE, AUTO, BURST};
    public FireType fireType;
    public int magSize;
    public float fireRate;
    public float altFireRate;
    public float damage;
    public float adsSpeed;
    public float reloadTime;
    public Vector3 adsPos;

    [Header("Normal Recoil")]
    public float verticalRecoil;
    public float horizontalRecoil;

    public float recoilRecoveryDampening;
    public float snappiness;
    
    [Header("Alt Recoil")]
    public float altVerticalRecoil;
    public float altHorizontalRecoil;

    public float altRecoilRecoveryDampening;
    public float altSnappiness;


}
