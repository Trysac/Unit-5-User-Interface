using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TargetToss.Tests
{
    /// <summary>The difficulty buttons in the 2021 scenes: harder levels spawn targets faster.</summary>
    public class DifficultyTests
    {
        GameManager manager;

        IEnumerator StartGame(int difficulty)
        {
            SceneManager.LoadScene("Menu");
            yield return null;
            manager = Object.FindFirstObjectByType<GameManager>();
            Assert.That(manager, Is.Not.Null);
            manager.StartGame(difficulty);
            yield return null;
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Prototype 5"));
        }

        [UnityTearDown]
        public IEnumerator CleanUp()
        {
            // The manager survives scene loads (DontDestroyOnLoad); drop it between tests.
            foreach (GameManager leftover in Object.FindObjectsByType<GameManager>(FindObjectsSortMode.None))
            {
                Object.Destroy(leftover.gameObject);
            }
            foreach (Target target in Object.FindObjectsByType<Target>(FindObjectsSortMode.None))
            {
                Object.Destroy(target.gameObject);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator EasySpawnsOneTargetPerSecond()
        {
            yield return StartGame(1);
            Assert.That(manager.SpawnInterval, Is.EqualTo(1f).Within(1e-4f));

            yield return new WaitForSeconds(1.5f);
            int alive = Object.FindObjectsByType<Target>(FindObjectsSortMode.None).Length;
            Assert.That(alive, Is.EqualTo(1), "one target after 1.5 seconds on Easy");
        }

        [UnityTest]
        public IEnumerator HarderLevelsSpawnFaster()
        {
            yield return StartGame(2);
            float normal = manager.SpawnInterval;
            yield return CleanUp();
            yield return StartGame(3);
            float hard = manager.SpawnInterval;

            Assert.That(normal, Is.EqualTo(0.5f).Within(1e-4f));
            Assert.That(hard, Is.EqualTo(1f / 3f).Within(1e-4f));
            Assert.That(hard, Is.GreaterThan(0f));
        }
    }
}
