using UnityEngine;
using System.Collections;

public class MomentumBoost : MonoBehaviour
{
    public float momentumBoost;


    
    public void Disable()
    {
        GetComponent<Collider>().enabled = false;
        GetComponent<MeshRenderer>().enabled = false;
        StartCoroutine(Enable());
    }
    
    
    IEnumerator Enable()
    {
        yield return new WaitForSeconds(1);
        GetComponent<Collider>().enabled = true;
        GetComponent<MeshRenderer>().enabled = true;
    }
}
