using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class TerminalButton : NetworkBehaviour
{
    private Terminal terminal;
    RectTransform buttonRectTransform;
    Button button;
    Image buttonImage;
    
    void Start()
    {
        buttonRectTransform = GetComponent<RectTransform>();
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();
        terminal = GetComponentInParent<Terminal>();
    }

    void Update()
    {
        if (RectTransformUtility.RectangleContainsScreenPoint(buttonRectTransform, terminal.GetCursorScreenPosition(), Camera.main) && gameObject.activeInHierarchy)
        {
            OnHover();
            
            if(Input.GetButtonDown("Fire1"))
            {
                button.onClick.Invoke();
            }
            
        }
        else
        {
            buttonImage.color = button.colors.normalColor;
        }
    }
    
    void OnHover()
    {
        buttonImage.color = button.colors.highlightedColor;
    }
}
