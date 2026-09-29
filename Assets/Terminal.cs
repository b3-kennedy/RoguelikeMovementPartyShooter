using UnityEngine;
using Unity.Netcode;

//script for baisic terminal functionality. Logic for specific terminal functionalty should be included in a separate script that inherits from this class.
public class Terminal : NetworkBehaviour
{
    public Canvas canvas;

    public GameObject cursor;

    private Vector2 cursorScreenPoint;


    public override void OnNetworkSpawn()
    {
        canvas.worldCamera = GameManager.Instance.GetLocalPlayerCamera();
    }

    public void SetCursorScreenPoint(Vector2 screenPoint)
    {
        cursorScreenPoint = screenPoint;
    }

    public Vector2 GetCursorScreenPosition()
    {
        return cursorScreenPoint;
    }
}
