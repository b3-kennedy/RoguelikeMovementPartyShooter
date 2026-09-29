using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;

public class TerminalInputField : NetworkBehaviour
{
    public Terminal terminal;
    RectTransform inputFieldRectTransform;
    TMP_InputField inputField;
    Image inputFieldImage;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        inputFieldRectTransform = GetComponent<RectTransform>();
        inputFieldImage = GetComponent<Image>();
        inputField = GetComponent<TMP_InputField>();
    }

    void Update()
    {
        if (RectTransformUtility.RectangleContainsScreenPoint(inputFieldRectTransform, terminal.GetCursorScreenPosition(), Camera.main))
        {
            OnHover();
            if (Input.GetButtonDown("Fire1"))
            {
                inputField.ActivateInputField();
            }

        }
        else
        {
            inputFieldImage.color = inputField.colors.normalColor;
        }
    }

    void OnHover()
    {
        inputFieldImage.color = inputField.colors.highlightedColor;
    }
}
