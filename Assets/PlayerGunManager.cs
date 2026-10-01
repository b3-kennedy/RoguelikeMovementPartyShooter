using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Analytics;

public class PlayerGunManager : NetworkBehaviour
{

    Gun gun;

    List<Gun> guns = new List<Gun>();
    private float nextFireTime;
    RecoilAnimation recoilAnimation;

    public NetworkVariable<int> activeGunIndex;
    FirstPersonRigidbodyController controller;
    private Vector3 hipPosition;

    bool canFire = true;
    bool canAim = true;

    Animator anim;

    bool canSwitchGuns = true;
    bool aim;
    Camera cam;

    AmmoUI ammoUI;
    
    Transform gunModel;

    int ammoBeforeReload;

    GunBob bob;
    private Coroutine reloadCoroutine;

    bool gunsAreHidden;

    bool useAmmo = true;

    static readonly string[] gunLayers = { "Pistol", "Rifle", "Shotgun", "Sniper", "SMG" };
    
    void Start()
    {
        controller = GetComponent<FirstPersonRigidbodyController>();
        ammoUI = GameManager.Instance.ammoUI.GetComponent<AmmoUI>();
    }

    public void SetGun(Gun newGun, int ammo)
    {
        if (!cam) cam = GetComponent<Interact>().cam;

        newGun.SetManager(this);
        newGun.SetCamera(cam);
        newGun.GetComponentInChildren<GunBob>().enabled = true;
        newGun.SetAmmo(ammo);
        ammoUI.gunName.text = newGun.gunData.gunName;
        ammoUI.ammoText.text = ammo.ToString() + " / " + newGun.gunData.magSize;

        if (guns.Count < 2) guns.Add(newGun);

        EquipGun(newGun, isPickup: true);
    }
    
    public void CanSwitchGuns(bool value)
    {
        canSwitchGuns = value;
    }
    
    public bool GetCanSwitchGuns()
    {
        return canSwitchGuns;
    }
        
    public List<Gun> GetGuns()
    {
        return guns;
    }
    
    public GunBob GetGunBob()
    {
        return bob;
    }
    
    public bool AreGunsHidden()
    {
        return gunsAreHidden;
    }
    
    public void SetGunsHidden(bool value)
    {
        gunsAreHidden = value;
    }

    public Gun GetActiveGun()
    {
        return gun;
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Everyone)]
    void SetActiveGunRpc(int index)
    {
        activeGunIndex.Value = index;
    }
    
    public int GetActiveGunIndex()
    {
        for (int i = 0; i < guns.Count; i++)
        {
            var gun = guns[i];
            if (gun.gameObject.activeSelf)
            {
                return i;
            }
        }
        return -1;
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

    public void RemoveGun(int index)
    {
        Debug.Log($"RemoveGun index={index} count={guns.Count} equipped={(gun ? gun.name : "null")}");
        if (index < 0 || index >= guns.Count) return;

        Gun removed = guns[index];
        bool wasEquipped = (removed == gun);

        if (wasEquipped) CancelReload();

        guns.RemoveAt(index);

        if (removed) // false if already destroyed
        {
            removed.SetManager(null);
            removed.SetCamera(null);
        }

        if (!wasEquipped) return;

        if (guns.Count > 0)
        {
            EquipGun(guns[0]);
        }
        else
        {
            gun = null;
            anim = null;
            recoilAnimation = null;
        }
    }

    public void EquipGun(Gun g, bool isPickup = false)
    {
        if (gun && gun != g)
        {
            gun.SetIsFiring(false);
            gun.gameObject.SetActive(false);
        }

        gun = g;
        gun.gameObject.SetActive(true);
        Debug.Log(gun);
        anim = gun.GetComponentInChildren<Animator>(true);
        Debug.Log(anim);
        recoilAnimation = gun.GetComponentInParent<RecoilAnimation>();
        recoilAnimation.SetPlayerGunManager(this);
        gunModel = gun.GetComponentInChildren<MeshRenderer>().transform;
        gunModel.localPosition = Vector3.zero;
        
        bob = gun.GetComponentInChildren<GunBob>(true);
        if(bob)
        {
            bob.enabled = true;
        }
        

        if (isPickup)
        {
            recoilAnimation.OnPickup();
        }

        gun.transform.localPosition = hipPosition;
        nextFireTime = 0f;

        SetAnimLayer(gun);

       // if (IsOwner) SetActiveGunRpc(guns.IndexOf(gun));
       
        SwitchWeaponRpc(GetActiveGunIndex(), OwnerClientId);
        ammoUI.gunName.text = GetActiveGun().gunData.gunName;
        ammoUI.ammoText.text = GetActiveGun().GetAmmo().ToString() + " / " + GetActiveGun().gunData.magSize;
    }

    public void SetGunsVisible(bool visible)
    {
        gunsAreHidden = !visible;
        canFire = visible;

        if (visible)
        {
            if (guns.Count == 0) return;

            // Always bring back the first gun in the list
            Gun first = guns[0];

            foreach (Gun g in guns)
            {
                if(g == first)
                {
                    g.gameObject.SetActive(true);
                    //SetAnimLayer(g);
                }


            }

            //SwitchGun();
        }
        else
        {
            foreach (Gun g in guns)
                g.gameObject.SetActive(false);
        }

        
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
    
    void SetAnimLayer(Gun g)
    {
        foreach (var layerName in gunLayers)
        {
            int index = anim.GetLayerIndex(layerName);
            if (index >= 0) anim.SetLayerWeight(index, 0f);
        }

        switch (g.gunData.gunType)
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
    }

    void SwitchGun()
    {
        if (guns.Count < 2 || !gun) return;

        CancelReload();

        int next = (guns.IndexOf(gun) + 1) % guns.Count;
        EquipGun(guns[next]);
    }

    void CancelReload()
    {
        if (!gun || !gun.IsReloading()) return;

        if (reloadCoroutine != null) StopCoroutine(reloadCoroutine);
        GameManager.Instance.EnableReloadUI(false);
        anim.SetBool("Reload", false);
        gun.SetAmmo(ammoBeforeReload);
        gun.SetReloading(false);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void SwitchWeaponRpc(int index, ulong clientID)
    {
        SwitchWeaponClientRpc(index, clientID);
    }

    [Rpc(SendTo.Everyone)]
    void SwitchWeaponClientRpc(int index, ulong clientID)
    {
        if (NetworkManager.Singleton.LocalClientId == clientID) return;
    
        var player = NetworkManager.Singleton.ConnectedClients[clientID].PlayerObject;
        var camera = player.GetComponent<Interact>().cam;
        var weaponHolder = camera.GetComponent<NetworkPlayerCamera>().transform;
        for (int i = 0; i < weaponHolder.childCount; i++)
        {
            weaponHolder.GetChild(0).GetChild(i).gameObject.SetActive(i == index);
        }
    }



    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;
        if (!gun) return;

        if (Input.GetKeyDown(KeyCode.Alpha1) && canSwitchGuns)
        {
            SwitchGun();
        }
        
        if(Input.GetKeyDown(KeyCode.R) && !gun.IsReloading() && !controller.IsSprinting())
        {
            ammoBeforeReload = gun.GetAmmo();
            reloadCoroutine = StartCoroutine(gun.Reload());
        }

        if (gun.IsReloading() && controller.IsSprinting())
        {
            CancelReload();
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

        gunModel.localPosition = Vector3.Lerp(
            gunModel.localPosition,
            targetPos,
            Time.deltaTime * gun.gunData.adsSpeed
        );
    }
    
    
    
    void ScheduleNextFireTime()
    {
        nextFireTime = Time.time + 1f / gun.gunData.fireRate;
    }
}
