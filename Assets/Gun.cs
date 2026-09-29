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

    bool isFiring;

    LayerMask layerMask;

    void Start()
    {
        recoilAnimation = GetComponentInParent<RecoilAnimation>();
        ammo = gunData.magSize;
        layerMask = LayerMask.GetMask("Barrier");
        anim = GetComponentInChildren<Animator>();
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

        isReloading = false;
        anim.SetBool("Reload", false);
        GameManager.Instance.EnableReloadUI(false);
    }

    public virtual void Fire(bool useAmmo)
    {
        if (!cam) return;
        if(isHidden) return;
        
        if(ammo <= 0)
        {
            if(!isReloading)
            {                
                StartCoroutine(Reload());
            }
            return;
        }
        
        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, Mathf.Infinity, ~layerMask))
        {
            Health health = hit.collider.GetComponent<Health>();
            
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
        
        if(useAmmo)
        {
            ammo--;
        }
        
        
        anim.SetFloat("RecoilVariance", Random.Range(0f, 1f));
        anim.SetTrigger("Shoot");
        recoilAnimation.Recoil();
    }
    
    public virtual void AlternateFire()
    {
        
    }

}
