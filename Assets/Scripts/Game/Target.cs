using UnityEngine;
using UnityEngine.Serialization;

public class Target : MonoBehaviour
{
    [SerializeField] float torqueForce = 10f;
    [SerializeField] float minSpeed = 14f;
    [SerializeField] float maxSpeed = 18f;
    [SerializeField] float xRange = 4f;
    [SerializeField] float ySpawnPos = -6f;
    [SerializeField] int points = 5;
    [FormerlySerializedAs("explotion")]
    [SerializeField] ParticleSystem explosion;

    Rigidbody body;
    GameManager gameManager;

    void Start()
    {
        body = GetComponent<Rigidbody>();
        body.AddForce(RandomForce(), ForceMode.Impulse);
        body.AddTorque(RandomTorque(), ForceMode.Impulse);
        gameManager = FindFirstObjectByType<GameManager>();

        transform.position = RandomSpawnPosition();
    }

    void OnMouseDown()
    {
        if (!gameManager.GetIsGameActive())
        {
            return;
        }
        if (CompareTag("Bad"))
        {
            gameManager.GameOver();
        }
        else
        {
            gameManager.UpdateScore(points);
            Instantiate(explosion, transform.position, explosion.transform.rotation);
        }
        Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        Destroy(gameObject);
    }

    Vector3 RandomForce() => Vector3.up * Random.Range(minSpeed, maxSpeed);

    Vector3 RandomTorque() =>
        new Vector3(Random.Range(-torqueForce, torqueForce), Random.Range(-torqueForce, torqueForce), Random.Range(-torqueForce, torqueForce));

    Vector3 RandomSpawnPosition() => new Vector3(Random.Range(-xRange, xRange), ySpawnPos);
}
