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
    bool roundStarted;

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
        roundStarted = false;
        UpdateScore(0);
    }

    void Update()
    {
        // The manager outlives the scenes, so a round begins once each time the game scene loads.
        if (!roundStarted && SceneManager.GetActiveScene().buildIndex == 1)
        {
            BeginRound();
        }

        if (isGameActive)
        {
            int due = spawnClock.Advance(Time.deltaTime);
            for (int i = 0; i < due; i++)
            {
                Instantiate(targets[Random.Range(0, targets.Count)]);
            }
        }
    }

    void BeginRound()
    {
        roundStarted = true;
        canvas.SetActive(true);
        gameOver.SetActive(false);
        gameOverButton.SetActive(false);
        scoreBoard.Reset();
        score.text = scoreBoard.Label;
        spawnClock = new SpawnClock(spawnRate);
        isGameActive = true;
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
        spawnRate = DifficultyRules.SpawnInterval(difficulty);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void RestartGame()
    {
        roundStarted = false;
        isGameActive = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public bool GetIsGameActive()
    {
        return isGameActive;
    }
}
