using UnityEngine;

[CreateAssetMenu(fileName = "Gun", menuName = "Weapons/Gun")]
public class GunData : ScriptableObject
{

    public enum GunType { PISTOL, RIFLE, SHOTGUN, SNIPER, SMG };
    public GunType gunType;

    public string gunName;
    public enum FireType {SINGLE, AUTO, BURST};
    public FireType fireType;
    public int magSize;
    public float fireRate;
    public float damage;
    public float adsSpeed;
    public float reloadTime;
    public Vector3 adsPos;

    public float verticalRecoil;
    public float horizontalRecoil;

    public float recoilRecoveryDampening;
    public float snappiness;
    
    
    
}
