using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class PlayerGunManager : NetworkBehaviour
{

    Gun gun;
    private float nextFireTime;
    RecoilAnimation recoilAnimation;

    FirstPersonRigidbodyController controller;
    private Vector3 hipPosition;

    bool canFire = true;
    bool canAim = true;

    Animator anim;

    bool aim;
    Camera cam;

    int ammoBeforeReload;

    private Coroutine reloadCoroutine;

    bool useAmmo = true;

    static readonly string[] gunLayers = { "Pistol", "Rifle", "Shotgun", "Sniper", "SMG" };
    
    void Start()
    {
        controller = GetComponent<FirstPersonRigidbodyController>();
    }

    public void SetGun(Gun newGun)
    {
        if(!cam)
        {
            cam = GetComponent<Interact>().cam;
        }

        gun = newGun;
        gun.SetManager(this);
        gun.SetCamera(cam);
        anim = gun.GetComponentInChildren<Animator>();
        recoilAnimation = gun.GetComponentInParent<RecoilAnimation>();
        recoilAnimation.SetPlayerGunManager(this);
        recoilAnimation.OnPickup();
        hipPosition = gun.transform.localPosition;
        nextFireTime = 0f;

        foreach (var layerName in gunLayers)
        {
            int index = anim.GetLayerIndex(layerName);
            if (index >= 0) anim.SetLayerWeight(index, 0f);
        }


        switch (newGun.gunData.gunType)
        {
            case GunData.GunType.PISTOL:
                anim.SetLayerWeight(anim.GetLayerIndex("Pistol"), 1f);
                break;
            case GunData.GunType.RIFLE:
                anim.SetLayerWeight(anim.GetLayerIndex("Rifle"), 1f);
                break;
            case GunData.GunType.SHOTGUN:
                break;
            case GunData.GunType.SNIPER:
                break;
            case GunData.GunType.SMG:
                break;
        }

        Debug.Log($"Picked up {newGun.name}");
    }
    
    public void CanFire(bool value)
    {
        canFire = value;
    }

    public void SetUseAmmo(bool value)
    {
        useAmmo = value;
    }
    
    public bool IsUsingAmmo()
    {
        return useAmmo;
    }
    
    public void CanAim(bool value)
    {
        canAim = value;
    }
    
    public bool GetCanAim()
    {
        return canAim;
    }
    
    public bool GetCanFire()
    {
        return canFire;
    }
    
    public void RemoveGun()
    {
        gun.SetManager(null);
        gun.SetCamera(null);
        gun = null;
    }
    
    public bool isAiming()
    {
        return aim;
    }
    
    public Animator GetAnimator()
    {
        return anim;
    }
    
    public Gun GetGun()
    {
        return gun;
    }

    // Update is called once per frame
    void Update()
    {
        if (!gun) return;
        
        if(Input.GetKeyDown(KeyCode.R) && !gun.IsReloading() && !controller.IsSprinting())
        {
            ammoBeforeReload = gun.GetAmmo();
            reloadCoroutine = StartCoroutine(gun.Reload());
        }

        if (gun.IsReloading() && controller.IsSprinting())
        {
            StopCoroutine(reloadCoroutine);
            GameManager.Instance.EnableReloadUI(false);
            anim.SetBool("Reload", false);
            gun.SetAmmo(ammoBeforeReload);
            gun.SetReloading(false);
        }

        if (!gun.IsReloading() && canFire)
        {
            switch (gun.gunData.fireType)
            {
                case GunData.FireType.SINGLE:
                    if (Input.GetButtonDown("Fire1") && Time.time >= nextFireTime)
                    {
                        gun.Fire(useAmmo);
                        gun.SetIsFiring(true);
                        ScheduleNextFireTime();
                    }
                    break;

                case GunData.FireType.AUTO:
                    if (Input.GetButton("Fire1") && Time.time >= nextFireTime)
                    {
                        gun.Fire(useAmmo);
                        gun.SetIsFiring(true);
                        ScheduleNextFireTime();
                    }
                    break;
            }
        }

        
        if(Input.GetButtonUp("Fire1") && gun.IsFiring())
        {
            gun.SetIsFiring(false);
        }


        if(canAim)
        {
            aim = Input.GetButton("Fire2");
        }
        else
        {
            aim = false;
        }
        
        Vector3 targetPos = aim ? gun.gunData.adsPos : hipPosition;

        gun.transform.localPosition = Vector3.Lerp(
            gun.transform.localPosition,
            targetPos,
            Time.deltaTime * gun.gunData.adsSpeed
        );
    }
    
    
    
    void ScheduleNextFireTime()
    {
        nextFireTime = Time.time + 1f / gun.gunData.fireRate;
    }
}
