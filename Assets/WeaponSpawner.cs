using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[System.Serializable]
public class WeaponTier
{
    public enum Tier { Common, Rare, Epic, Legendary }

    public Tier tier;
    public GameObject[] weapons;
}

[System.Serializable]
public class TierRow
{
    public float points;
    public float common;
    public float rare;
    public float epic;
    public float legendary;

    public float Sum => common + rare + epic + legendary;

    public float Get(WeaponTier.Tier tier)
    {
        switch (tier)
        {
            case WeaponTier.Tier.Common: return common;
            case WeaponTier.Tier.Rare: return rare;
            case WeaponTier.Tier.Epic: return epic;
            case WeaponTier.Tier.Legendary: return legendary;
            default: return 0f;
        }
    }
}

public class WeaponSpawner : NetworkBehaviour
{
    public Transform spawnPoint;
    [SerializeField] private WeaponTier[] tiers;

    [Tooltip("Rows are interpolated by points. Keep them in ascending order. Each row should sum to 100.")]
    [SerializeField]
    private List<TierRow> table = new List<TierRow>
    {
        new TierRow { points = 0,     common = 100, rare = 0,  epic = 0,  legendary = 0  },
        new TierRow { points = 5000,  common = 59,  rare = 24, epic = 14, legendary = 3  },
        new TierRow { points = 10000, common = 32,  rare = 39, epic = 23, legendary = 6  },
        new TierRow { points = 15000, common = 20,  rare = 27, epic = 41, legendary = 12 },
        new TierRow { points = 20000, common = 0,   rare = 10, epic = 70, legendary = 20 },
    };

    [Header("Debug")]
    [SerializeField] private float debugPoints;

    private GameObject spawnedWeapon;

    public override void OnNetworkSpawn()
    {
        // Only the server rolls and spawns
        if (!IsServer) return;

        table.Sort((a, b) => a.points.CompareTo(b.points));
        //SpawnWeapon();
    }
    
    

    private void OnValidate()
    {
        if (table == null) return;

        foreach (var row in table)
        {
            if (Mathf.Abs(row.Sum - 100f) > 0.01f)
                Debug.LogWarning($"Row at {row.points} points sums to {row.Sum}, not 100", this);
        }
    }

    private void Update()
    {
        if(IsServer)
        {
            if(Input.GetKeyDown(KeyCode.L))
            {
                SpawnWeapon();
            }
        }
    
        if (spawnedWeapon != null)
            spawnedWeapon.transform.Rotate(Vector3.up, 20f * Time.deltaTime);
    }

    private void SpawnWeapon()
    {
        if (!IsServer)
        {
            Debug.LogWarning("SpawnWeapon called on a client. This should only be called on the server.");
            return;
        }

        GameObject prefab = GetRandomWeapon();
        if (prefab == null)
        {
            Debug.LogWarning("No weapon could be rolled. Check the tiers and table.", this);
            return;
        }

        var holder = prefab.GetComponent<GunReferenceHolder>();
        if (holder == null)
        {
            Debug.LogError($"{prefab.name} has no GunReferenceHolder", prefab);
            return;
        }

        SpawnWeaponRpc(spawnPoint.position, holder.gunID);
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    public void SpawnWeaponRpc(Vector3 position, int gunID)
    {
        GameObject prefab = GameManager.Instance.guns[gunID].pickupObject;
        if (prefab != null)
        {
            spawnedWeapon = Instantiate(prefab, position, Quaternion.identity);
            spawnedWeapon.GetComponent<Rigidbody>().isKinematic = true;
            spawnedWeapon.GetComponent<GunReferenceHolder>().SetPickupInstanceID(GameManager.Instance.nextPickupID);
            GameManager.Instance.nextPickupID++;
        }
    }

    private float GetTableValue(WeaponTier.Tier tier, float points)
    {
        if (table == null || table.Count == 0) return 0f;
        if (points <= table[0].points) return table[0].Get(tier);

        for (int i = 1; i < table.Count; i++)
        {
            if (points <= table[i].points)
            {
                TierRow a = table[i - 1];
                TierRow b = table[i];
                float span = b.points - a.points;
                float t = span > 0f ? (points - a.points) / span : 1f;
                return Mathf.Lerp(a.Get(tier), b.Get(tier), t);
            }
        }

        return table[table.Count - 1].Get(tier);
    }

    private float GetWeight(WeaponTier t, float points)
    {
        // Tiers with no weapons can't be rolled
        if (t.weapons == null || t.weapons.Length == 0) return 0f;
        return Mathf.Max(0f, GetTableValue(t.tier, points));
    }


    public GameObject GetRandomWeapon()
    {
        WeaponTier tier = RollTier();
        if (tier == null) return null;

        return tier.weapons[Random.Range(0, tier.weapons.Length)];
    }

    private WeaponTier RollTier()
    {
        float points = GameManager.Instance.points.Value;

        float total = 0f;
        foreach (var t in tiers)
            total += GetWeight(t, points);

        if (total <= 0f) return null;

        float roll = Random.Range(0f, total);
        float cumulative = 0f;

        foreach (var t in tiers)
        {
            float w = GetWeight(t, points);
            if (w <= 0f) continue;

            cumulative += w;
            if (roll <= cumulative)
                return t;
        }

        return null;
    }


    [ContextMenu("Log Tier Chances")]
    private void LogChances()
    {
        float total = 0f;
        foreach (var t in tiers) total += GetWeight(t, debugPoints);

        if (total <= 0f)
        {
            Debug.Log("No valid tiers");
            return;
        }

        foreach (var t in tiers)
            Debug.Log($"{t.tier} @ {debugPoints}: {GetWeight(t, debugPoints) / total * 100f:F1}%");
    }
}