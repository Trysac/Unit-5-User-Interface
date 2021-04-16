using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField] List<GameObject> targets;
    [SerializeField] float spawnRate = 1f;
    [SerializeField] Text score;

    int points;

    void Start()
    {
        UpdateScore(0);
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
            int index = Random.Range(0, targets.Count);
            Instantiate(targets[index]);
        }
    }

    public void UpdateScore(int p) 
    {
        points += p;
        score.text = "Score: " + points.ToString();
    }
}
