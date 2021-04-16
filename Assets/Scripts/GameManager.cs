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
    [SerializeField] GameObject canvas;

    int points;
    bool isGameActive;
    bool isCalled;

    private void Awake()
    {
        if (FindObjectsOfType<GameManager>().Length > 1) 
        {
            Destroy(gameObject);
        }
        else 
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    void Start()
    {
        isGameActive = false;
        isCalled = false;

        UpdateScore(0);
    }


    void Update()
    {
        if (SceneManager.GetActiveScene().buildIndex == 1)
        {
            canvas.SetActive(true);
            isGameActive = true;
            if (!isCalled) 
            { 
                StartCoroutine(SpawnTarget());
                isCalled = true;
            }
            
        }
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

    public void StartGame(int difficulty) 
    {
        spawnRate = difficulty/3;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
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
