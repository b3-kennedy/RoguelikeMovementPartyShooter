using UnityEngine;
using Unity.Netcode;

public class RoomButton : NetworkBehaviour
{
    NetworkVariable<bool> isPressed = new NetworkVariable<bool>(false);
    
    public Material unpressedMat;
    public Material pressedMat;
    
    void OnEnable()
    {
        isPressed.OnValueChanged += OnPressedStateChanged;
    }
    
    void OnDisable()
    {
        isPressed.OnValueChanged -= OnPressedStateChanged;
    }

    public void OnButtonPressed()
    {
        if(isPressed.Value) return;
        int id = transform.GetSiblingIndex();
        GetComponentInParent<Room>().OnRoomButtonPressedRpc(id);
    }
    
    public void SetPressedState(bool pressed)
    {
        if(!IsServer) return;
        isPressed.Value = pressed;
    }
    
    void OnPressedStateChanged(bool previousValue, bool newValue)
    {
        Debug.Log($"{name} pressed changed {previousValue} -> {newValue}, IsServer={IsServer}");
        if (newValue)
        {
            GetComponent<MeshRenderer>().material = pressedMat;
        }
        else
        {
            GetComponent<MeshRenderer>().material = unpressedMat;
        }
    }
}
