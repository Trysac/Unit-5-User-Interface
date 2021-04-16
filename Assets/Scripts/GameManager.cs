using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] List<GameObject> targets;
    [SerializeField] float spawnRate = 1f;
    void Start()
    {
        StartCoroutine(SpawnTarget());
    }


    void Update()
    {
        
    }

    private IEnumerator SpawnTarget() 
    {
        while (true) 
        { 
            yield return new WaitForSeconds(spawnRate);
            int index = Random.Range(0,targets.Count);
            Instantiate(targets[index]);

        }

    }
}
