using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

//Made by Rajendra Abhinaya, 2023

namespace DiabolicalGames
{
    public class DestructibleObject : NetworkBehaviour
    {
        enum DebrisAmount{
            Low,
            Medium,
            High,
            Random
        }

        enum DespawnType{
            None,
            Timed,
            DistanceFromPlayer
        }

        [System.Serializable]
        struct DebrisPrefab{
            public string name;
            public GameObject prefab;
        }

        [Header("Debris")]
        [SerializeField, Tooltip("List of debris prefabs that will be spawned when the object is destroyed. Editing the list is not recommended")]
        private List<DebrisPrefab> debrisPrefabs = new List<DebrisPrefab>();

        [SerializeField, Tooltip("Amount of debris(Gameobjects) that will be spawned when the object breaks")]
        private DebrisAmount debrisAmount = new DebrisAmount();

        [SerializeField, Tooltip("Force required to break the object")]
        private float forceRequired;

        [Header("Despawning")]
        [SerializeField, Tooltip("Despawn mode used for despawning the debris created by destroying the object\nNone: debris will not be despawned\nTimed: debris will despawned after a certain time\nDistance from Player: debris will despawn when the player moves a certain distance away from the debris")]
        private DespawnType despawnType = new DespawnType();

        [SerializeField, Range(0, 100), Tooltip("Percentage of debris objects that will be despawned")]
        private int despawnPercentage;

        [SerializeField, Tooltip("Time in seconds before debris will despawn when using the Timed despawn mode")]
        private float despawnTime;

        [SerializeField, Tooltip("Player gameobject for when using the Distance from Player despawn mode")]
        private GameObject player;

        [SerializeField, Tooltip("Distance between debris and player before debris despawns when using the Distance from Player despawn mode")]
        private float distanceFromPlayer;

        [Header("Audio")]
        [SerializeField, Tooltip("List of audio clips that will be played when the object breaks. Audio clips are selected randomly from the list")]
        private List<AudioClip> audioClips = new List<AudioClip>();

        [SerializeField, Tooltip("Volume of the audio clip when played"), Range(0f, 1f)]
        private float volume;

        [SerializeField, Tooltip("Amount of variation in the volume of each audio clip played"), Range(0f, 0.2f)]
        private float volumeVariation;

        [SerializeField, Tooltip("Amount of variation in the pitch volume of each audio clip played"), Range(0f, 0.5f)]
        private float pitchVariation;

        [SerializeField] float shotForce = 8f;
        [SerializeField] float shotRandomness = 3f;
        [SerializeField] float explosionForce = 300f;
        [SerializeField] float explosionRadius = 2f;

        [SerializeField] int pointOrbCount = 3;

        private GameObject debris;
        private new Rigidbody rigidbody;

            public GameObject pointsCollectablePrefab;

            public void Break(bool isCollision, Vector3 hitPoint = default, Vector3 hitDirection = default)
            {
                Vector3 baseVelocity = rigidbody.linearVelocity;
                float velocityMagnitude = baseVelocity.magnitude;

                debris.transform.SetPositionAndRotation(transform.position, transform.rotation);
                debris.transform.localScale = transform.localScale;
                debris.SetActive(true);

                for (int i = 0; i < debris.transform.childCount; i++)
                {
                    Rigidbody debrisRb = debris.transform.GetChild(i).GetComponent<Rigidbody>();

                    if (isCollision)
                    {
                        // Inherit the box's velocity, plus some scatter scaled by impact speed
                        Vector3 randomise = Random.insideUnitSphere * (velocityMagnitude / 2f);
                        debrisRb.linearVelocity = baseVelocity + randomise;
                    }
                    else
                    {
                        // Push along the bullet direction, plus random scatter
                        Vector3 randomise = Random.insideUnitSphere * shotRandomness;
                        debrisRb.linearVelocity = baseVelocity + hitDirection.normalized * shotForce + randomise;

                        // Optional: radial burst outward from the impact point
                        debrisRb.AddExplosionForce(explosionForce, hitPoint, explosionRadius);
                    }
                }

                if (IsServer)
                {
                    Bounds bounds = GetComponent<Collider>().bounds;
                    for (int i = 0; i < pointOrbCount; i++)
                    {
                        Vector3 spawnPoint = new Vector3(
                            Random.Range(bounds.min.x, bounds.max.x),
                            Random.Range(bounds.min.y, bounds.max.y),
                            Random.Range(bounds.min.z, bounds.max.z)
                        );

                        GameObject collectable = Instantiate(pointsCollectablePrefab, spawnPoint, Quaternion.identity);
                        collectable.GetComponent<NetworkObject>().Spawn();
                    }
                }

            //Sends variable values to the debris
            debris.GetComponent<Despawn>().SetVariables(despawnPercentage, despawnTime, distanceFromPlayer, player, audioClips[Random.Range(0, audioClips.Count)], volume, volumeVariation, pitchVariation);

            //Activates the despawning mechanism of the debris
            switch(despawnType){
                case DespawnType.Timed:
                    debris.GetComponent<Despawn>().BeginCoroutine("Timed");
                    break;
                case DespawnType.DistanceFromPlayer:
                    debris.GetComponent<Despawn>().BeginCoroutine("Distance from Player");
                    break;
            }

            //Destroys the game object
            Destroy(gameObject);
        }

            [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Everyone)]
            public void BreakRpc(bool isCollision, Vector3 hitPoint = default, Vector3 hitDirection = default)
            {
                Break(isCollision, hitPoint, hitDirection);
            }

            void Start()
        {
            rigidbody = GetComponent<Rigidbody>();

            //Instantiates the debris prefab based on the amount chosen and then disables the debris object
            switch(debrisAmount){
                case DebrisAmount.Low:
                    debris = Instantiate(debrisPrefabs[0].prefab, transform.position, Quaternion.identity);
                    break;
                case DebrisAmount.Medium:
                    debris = Instantiate(debrisPrefabs[1].prefab, transform.position, Quaternion.identity);
                    break;
                case DebrisAmount.High:
                    debris = Instantiate(debrisPrefabs[2].prefab, transform.position, Quaternion.identity);
                    break;
                case DebrisAmount.Random:
                    debris = Instantiate(debrisPrefabs[Random.Range(0,3)].prefab, transform.position, Quaternion.identity);
                    break;
                default:
                    debris = Instantiate(debrisPrefabs[0].prefab, transform.position, Quaternion.identity);
                    break;
            }
            debris.SetActive(false);
        }

        void OnCollisionEnter(Collision collision){
            if(collision.relativeVelocity.magnitude > forceRequired){
                Break(true);
            }
        }
    }
}