using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField] List<GameObject> targets;
    [SerializeField] float spawnRate = 1f;
    [SerializeField] Text score;
    [SerializeField] GameObject gameOver;
    [SerializeField] GameObject gameOverButton;

    int points;
    bool isGameActive;

    void Start()
    {
        isGameActive = true;

        UpdateScore(0);
        StartCoroutine(SpawnTarget());
    }


    void Update()
    {
        
    }

    private IEnumerator SpawnTarget() 
    {
        while (isGameActive)
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

    public void GameOver() 
    {
        isGameActive = false;

        gameOver.SetActive(true);
        gameOverButton.SetActive(true);
    }

    public void RestarGame() 
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public bool GetIsGameActive() 
    {
        return isGameActive;
    }
}
