using UnityEngine;
using TMPro;
using Unity.Netcode;

public class ReverseGameWords : NetworkBehaviour
{
    public string[] easyWords = new string[]
    {
        "sun", "moon", "rain", "fish", "mouse", "snow", "nose", "arm", "owl", "water",
        "ear", "eye", "leaf", "wing", "hair", "lion", "worm", "snail", "wool", "moss"
    };

    public string[] mediumWords = new string[]
    {
        "window", "jungle", "monster", "shadow", "mirror", "flower", "thunder", "feather", "yellow", "lemon",
        "spider", "animal", "umbrella", "volcano", "rainbow", "safari", "arena", "circus", "octopus", "cinema"
    };

    public string[] hardWords = new string[]
    {
        "university", "thermometer", "mysterious", "chandelier", "aluminum", "melancholy", "imagination", "watermelon", "renaissance", "hallucination",
        "aquarium", "extraordinary", "adventure", "laboratory", "anniversary", "helicopter", "memorial", "mathematics", "millennium", "philosophy"
    };

    public float easyWordChance = 0.5f;
    public float mediumWordChance = 0.45f;
    public float hardWordChance = 0.05f;
    
    public string currentWord;
    
    public TextMeshProUGUI wordDisplay;

    public NetworkVariable<int> failCount;

    public Transform failIndicatorParent;

    public void StartRoom()
    {
        PickWordRpc();
    }
    
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void PickWordRpc()
    {
        string[] pool = PickWordPool();
        currentWord = pool[Random.Range(0, pool.Length)];
        wordDisplay.text = currentWord;
        UpdateWordDisplayRpc(currentWord);
    }
    

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    public void UpdateWordDisplayRpc(string newWord)
    {
        currentWord = newWord;
        wordDisplay.text = currentWord;
    }

    string[] PickWordPool()
    {
        float randomNum = Random.Range(0f, 1f);
        if (randomNum < easyWordChance) return easyWords;
        if (randomNum < easyWordChance + mediumWordChance) return mediumWords;
        return hardWords;
    }
}