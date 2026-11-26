using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DeadlineRush.Tests.PlayMode
{
    /// <summary>
    /// Smoke test chạy trong PlayMode để đảm bảo scene đầu tiên (build index 0)
    /// load thành công và không phát sinh Error hoặc Exception trong Console.
    /// </summary>
    [TestFixture]
    public class SmokeTests
    {
        private readonly List<string> _capturedErrors = new List<string>();

        [SetUp]
        public void SetUp()
        {
            _capturedErrors.Clear();
            LogAssert.ignoreFailingMessages = false;
            Application.logMessageReceived += HandleLogMessageReceived;
        }

        [TearDown]
        public void TearDown()
        {
            Application.logMessageReceived -= HandleLogMessageReceived;
        }

        [UnityTest]
        public IEnumerator FirstScene_LoadsWithoutErrorsOrExceptions()
        {
            Assert.Greater(
                SceneManager.sceneCountInBuildSettings,
                0,
                "Build Settings phải có ít nhất 1 scene được bật (build index 0).");

            AsyncOperation loadOp = SceneManager.LoadSceneAsync(0, LoadSceneMode.Single);
            Assert.IsNotNull(loadOp, "SceneManager.LoadSceneAsync(0) trả về null.");

            while (!loadOp.isDone)
            {
                yield return null;
            }

            // Chờ thêm 2 frame để Awake / Start / Update đầu tiên thực thi xong
            yield return null;
            yield return null;

            Scene activeScene = SceneManager.GetActiveScene();
            Assert.IsTrue(activeScene.isLoaded, "Scene đầu tiên chưa ở trạng thái isLoaded.");
            Assert.AreEqual(0, activeScene.buildIndex, "Active scene không phải build index 0.");

            Assert.IsEmpty(
                _capturedErrors,
                "Phát sinh Error/Exception khi load scene đầu tiên:\n- " + string.Join("\n- ", _capturedErrors));
        }

        private void HandleLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                _capturedErrors.Add($"[{type}] {condition}\n{stackTrace}");
            }
        }
    }
}
