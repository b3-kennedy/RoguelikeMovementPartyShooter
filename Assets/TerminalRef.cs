using UnityEngine;

public class TerminalRef : MonoBehaviour
{

    public Terminal terminal;
    
    void Start()
    {
        if (terminal == null)
        {
            terminal = GetComponentInParent<Terminal>();
        }
    }

}
