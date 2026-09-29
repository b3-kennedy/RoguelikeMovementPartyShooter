using System.Collections.Generic;
using UnityEngine;

public class GunReferenceHolder : MonoBehaviour
{
    public int gunID;
    public int pickupInstanceID = -1; // -1 = not a floor pickup (e.g. the currently-held gun)

    void OnEnable()
    {
        if (pickupInstanceID >= 0 && GameManager.Instance != null)
            GameManager.Instance.RegisterPickup(this);
    }

    void OnDisable()
    {
        if (pickupInstanceID >= 0)
            GameManager.Instance.UnregisterPickup(this);
    }

    public void SetPickupInstanceID(int id)
    {
        if (pickupInstanceID >= 0)
            GameManager.Instance.UnregisterPickup(this); // drop any stale registration
        pickupInstanceID = id;
        GameManager.Instance.RegisterPickup(this);        // register under the correct key
    }

}
