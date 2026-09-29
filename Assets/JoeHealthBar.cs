using UnityEngine;

public class JoeHealthBar : MonoBehaviour
{
    public Transform healthBar;
    Health health;
    
    void Start()
    {
        health = GetComponent<Health>();
        health.health.OnValueChanged += OnHealthChanged;
    }
    
    void OnHealthChanged(float oldValue, float newValue)
    {
        float healthPercentage = newValue / health.maxHealth;
        healthBar.localScale = new Vector3(healthPercentage, 1f, 1f);
    }
}
