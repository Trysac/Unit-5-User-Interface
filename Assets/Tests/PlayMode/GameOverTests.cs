using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TargetToss.Tests
{
    /// <summary>Game over in the 2021 scenes stops the round, and Restart starts a clean one.</summary>
    public class GameOverTests
    {
        GameManager manager;

        IEnumerator StartGame(int difficulty)
        {
            SceneManager.LoadScene("Menu");
            yield return null;
            manager = Object.FindFirstObjectByType<GameManager>();
            manager.StartGame(difficulty);
            yield return null;
            yield return null;
        }

        static HashSet<int> TargetIds()
        {
            var ids = new HashSet<int>();
            foreach (Target target in Object.FindObjectsByType<Target>(FindObjectsSortMode.None))
            {
                ids.Add(target.GetInstanceID());
            }
            return ids;
        }

        Text FindLabel(string name)
        {
            foreach (Text text in manager.GetComponentsInChildren<Text>(true))
            {
                if (text.name == name)
                {
                    return text;
                }
            }
            return null;
        }

        GameObject Child(string name)
        {
            foreach (Transform child in manager.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    return child.gameObject;
                }
            }
            return null;
        }

        [UnityTearDown]
        public IEnumerator CleanUp()
        {
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
        public IEnumerator GameOverStopsTheSpawnsAndTheScoring()
        {
            yield return StartGame(3);
            yield return new WaitForSeconds(0.5f);
            manager.GameOver();
            HashSet<int> before = TargetIds();
            yield return null;
            yield return null;
            Assert.That(manager.GetIsGameActive(), Is.False, "the round stays over");

            yield return new WaitForSeconds(1.2f);
            HashSet<int> after = TargetIds();
            after.ExceptWith(before);
            Assert.That(after, Is.Empty, "no targets spawn after game over");
            Assert.That(Child("GameOverText").activeSelf, Is.True);
        }

        [UnityTest]
        public IEnumerator RestartStartsACleanRound()
        {
            yield return StartGame(3);
            manager.UpdateScore(15);
            manager.GameOver();
            yield return null;

            manager.RestartGame();
            yield return null;
            yield return null;

            Assert.That(manager.Points, Is.EqualTo(0));
            Assert.That(FindLabel("Socre").text, Is.EqualTo("Score: 0"));
            Assert.That(Child("GameOverText").activeSelf, Is.False);
            Assert.That(Child("PlayAgainButton").activeSelf, Is.False);
            Assert.That(manager.GetIsGameActive(), Is.True);

            HashSet<int> before = TargetIds();
            yield return new WaitForSeconds(0.8f);
            HashSet<int> after = TargetIds();
            after.ExceptWith(before);
            Assert.That(after, Is.Not.Empty, "targets spawn again");
        }
    }
}
