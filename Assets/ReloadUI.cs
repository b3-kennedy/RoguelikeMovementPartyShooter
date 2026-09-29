using UnityEngine;

public class ReloadUI : MonoBehaviour
{
    public GameObject progressBar;
    
    public void SetProgress(float progress)
    {
        progressBar.transform.localScale = new Vector3(progress, 1f, 1f);
    }
}
