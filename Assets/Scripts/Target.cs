using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Target : MonoBehaviour
{
    [SerializeField] float torqueForce = 10f;
    [SerializeField] float minSpeed = 14f;
    [SerializeField] float maxSpeed = 18f;
    [SerializeField] float xRange = 4f;
    [SerializeField] float ySpawnPos = -6f;
    //[SerializeField] float force = 0f;

    Rigidbody myRigidbody;

    void Start()
    {
        myRigidbody = GetComponent<Rigidbody>();
        myRigidbody.AddForce(RandomForce(), ForceMode.Impulse);
        myRigidbody.AddTorque(RandomTorque(), ForceMode.Impulse);

        transform.position = RandomSpawnPosition();
    }


    void Update()
    {
        
    }

    private void OnMouseDown() 
    { 
        Destroy(gameObject); 
    }

    private void OnTriggerEnter(Collider other) 
    { 
        Destroy(gameObject); 
    }

    private Vector3 RandomForce() 
    {
        return Vector3.up * Random.Range(minSpeed, maxSpeed);
    }
    private Vector3 RandomTorque() 
    {
        return new Vector3(Random.Range(-torqueForce, torqueForce), Random.Range(-torqueForce, torqueForce), Random.Range(-torqueForce, torqueForce)) ;
    }
    private Vector3 RandomSpawnPosition() 
    { 
        return new Vector3(Random.Range(-xRange, xRange), ySpawnPos);
    }
}
