using System.Collections;
using UnityEngine;
using Unity.Netcode;
public class Gun : MonoBehaviour
{

    public GunData gunData;
    PlayerGunManager manager;

    Animator anim;

    int ammo;

    Camera cam;

    RecoilAnimation recoilAnimation;

    bool isHidden;

    bool isReloading;

    public Transform muzzle;

    bool isFiring;

    LayerMask layerMask;

    AmmoUI ammoUI;

    bool canFire = true;

    void Start()
    {
        recoilAnimation = GetComponentInParent<RecoilAnimation>();
        layerMask = LayerMask.GetMask("Barrier");
        anim = GetComponentInChildren<Animator>();
        ammoUI = GameManager.Instance.ammoUI.GetComponent<AmmoUI>();
    }
    
    void OnEnable()
    {
        isHidden = false;
        isReloading = false;
        isFiring = false;
        canFire = true;
    }
    
    void OnDisable()
    {
        isHidden = true;
        isReloading = false;
        isFiring = false;
        canFire = false;
    }
        
    public void SetHidden(bool value)
    {
        isHidden = value;
    }
    
    public bool GetHidden()
    {
        return isHidden;
    }
    
    public bool IsFiring()
    {
        return isFiring;
    }
    
    public bool IsReloading()
    {
        return isReloading;
    }
    
    public void SetReloading(bool value)
    {
        isReloading = value;
    }
    
    public void SetIsFiring(bool value)
    {
        isFiring = value;
    }

    public void SetManager(PlayerGunManager mg)
    {
        manager = mg;
    }
    
    public PlayerGunManager GetManager()
    {
        return manager;
    }
    
    public void SetCamera(Camera c)
    {
        cam = c;
    }
    
    public void SetAmmo(int value)
    {
        ammo = value;
    }
    
    public int GetAmmo()
    {
        return ammo;
    }

    public IEnumerator Reload()
    {
        isReloading = true;
        anim.SetBool("Reload", true);
        GameManager.Instance.EnableReloadUI(true);

        float elapsed = 0f;
        while (elapsed < gunData.reloadTime)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / gunData.reloadTime;
            GameManager.Instance.UpdateReloadProgress(progress);
            yield return null;
        }

        ammo = gunData.magSize;
        ammoUI.ammoText.text = ammo.ToString() + " / " + gunData.magSize.ToString();
        isReloading = false;
        anim.SetBool("Reload", false);
        GameManager.Instance.EnableReloadUI(false);
    }

    protected virtual void OnShotFired(Vector3 start, Vector3 end) { }

    public virtual void Fire(bool useAmmo, int ammoLoss, bool canRecoil = true)
    {
        if (!cam) return;
        if(isHidden) return;
        if(!canFire) return;

        if (!CheckReload(useAmmo ? ammoLoss : 0)) return;

        Vector3 end = cam.transform.position + cam.transform.forward * 500f;

        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, Mathf.Infinity, ~layerMask))
        {
            Health health = hit.collider.GetComponent<Health>();
            end = hit.point;
            if(health)
            {
                health.TakeDamageRpc(gunData.damage);
            }
            
            
            if(hit.collider.CompareTag("Target"))
            {
                var targetObj = hit.collider.GetComponent<NetworkObject>();
                targetObj.GetComponent<RedTarget>().OnHitServerRpc(NetworkManager.Singleton.LocalClientId);
            }
        }

        UpdateAmmoUI(useAmmo, ammoLoss);
        OnShotFired(muzzle ? muzzle.position : cam.transform.position, end);



        if (canRecoil)
        {
            anim.SetFloat("RecoilVariance", Random.Range(0f, 1f));
            anim.SetTrigger("Shoot");

        }
        recoilAnimation.Recoil();
    }

    protected Vector3 GetAimPoint()
    {
        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, Mathf.Infinity, ~layerMask))
            return hit.point;

        return cam.transform.position + cam.transform.forward * 500f;
    }

    public virtual void AlternateFire(bool useAmmo, int ammoLoss, bool canRecoil = true)
    {

        if (!cam) return;
        if (isHidden) return;
        if (!canFire) return;


        if (!CheckReload(useAmmo ? ammoLoss : 0)) return;
        UpdateAmmoUI(useAmmo, ammoLoss);



        if(canRecoil)
        {
            anim.SetFloat("RecoilVariance", Random.Range(0f, 1f));
            anim.SetTrigger("Shoot");

        }
        recoilAnimation.Recoil();

    }

    bool CheckReload(int ammoLoss)
    {
        if (ammo >= ammoLoss) return true;

        if (!isReloading)
        {
            StartCoroutine(Reload());
        }
        
        return false;
    }

    void UpdateAmmoUI(bool useAmmo, int ammoLoss)
    {
        if (useAmmo)
        {
            ammo -= ammoLoss;
            if(ammo < 0)
            {
                ammo = 0;
            }
            ammoUI.ammoText.text = ammo.ToString() + " / " + gunData.magSize.ToString();
        }
    }

}
