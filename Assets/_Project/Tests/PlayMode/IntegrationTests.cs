using System.Collections;
using System.IO;
using Blackjack.Verification;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Blackjack.Tests
{
    public sealed class IntegrationTests
    {
        [UnityTest]
        public IEnumerator TwoUnityClientsPlayChatRestoreAndLogout()
        {
            if (!File.Exists("Temp/Blackjack-smoke-server.txt")) Assert.Ignore("Start Tools/SmokeServer before running the live integration test.");
            var task = IntegrationScenario.RunAsync(File.ReadAllText("Temp/Blackjack-smoke-server.txt").Trim());
            while (!task.IsCompleted) yield return null;
            Assert.IsFalse(task.IsFaulted, "The scenario faulted unexpectedly.");
            Assert.IsTrue(task.Result.Succeed, task.Result.ErrorCode + ": " + task.Result.ErrorMessage);
        }
    }
}
