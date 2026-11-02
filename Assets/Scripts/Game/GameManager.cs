using System.Collections.Generic;
using TargetToss.Core;
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

    readonly ScoreBoard scoreBoard = new ScoreBoard();
    SpawnClock spawnClock;
    bool isGameActive;
    bool isSpawning;

    public int Points => scoreBoard.Points;

    public float SpawnInterval => spawnRate;

    void Awake()
    {
        if (FindObjectsByType<GameManager>(FindObjectsSortMode.None).Length > 1)
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
        isSpawning = false;
        UpdateScore(0);
    }

    void Update()
    {
        if (SceneManager.GetActiveScene().buildIndex == 1)
        {
            canvas.SetActive(true);
            isGameActive = true;
            if (!isSpawning)
            {
                spawnClock = new SpawnClock(spawnRate);
                isSpawning = true;
            }
        }

        if (isSpawning && isGameActive)
        {
            int due = spawnClock.Advance(Time.deltaTime);
            for (int i = 0; i < due; i++)
            {
                Instantiate(targets[Random.Range(0, targets.Count)]);
            }
        }
    }

    public void UpdateScore(int points)
    {
        scoreBoard.Add(points);
        score.text = scoreBoard.Label;
    }

    public void GameOver()
    {
        isGameActive = false;
        gameOver.SetActive(true);
        gameOverButton.SetActive(true);
    }

    public void StartGame(int difficulty)
    {
        spawnRate = difficulty / 3;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public bool GetIsGameActive()
    {
        return isGameActive;
    }
}
