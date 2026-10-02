using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using _Project.Scripts;
using Blackjack.Services;
using CardGame.Client;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Blackjack.UI;

namespace Blackjack.Tests
{
    public sealed class ServiceTests
    {
        [TestCase("AC", 0)] [TestCase("10H", 22)] [TestCase("QS", 37)] [TestCase("KD", 51)]
        public void ServerCardsMapToExplicitUnitySuitOrder(string card, int expected) => Assert.AreEqual(expected, CardDatabase.Parse(card).Value);
        [TestCase("")] [TestCase("ZZ")] [TestCase("14C")] [TestCase("0H")]
        public void InvalidCardsReturnFailures(string card) => Assert.IsFalse(CardDatabase.Parse(card).Succeed);
        [Test]
        public void All52TexturesMatchRankAndSuit()
        {
            var database = AssetDatabase.LoadAssetAtPath<CardDatabase>("Assets/_Project/Data/New Card Database.asset");
            var ranks = new[] { "ace", "2", "3", "4", "5", "6", "7", "8", "9", "10", "jack", "queen", "king" };
            var suits = new[] { "clubs", "hearts", "spades", "diamonds" };
            for (var suit = 0; suit < 4; suit++)
                for (var rank = 0; rank < 13; rank++)
                { var texture = database.GetCardTexture(suit * 13 + rank); Assert.IsTrue(texture.Succeed); Assert.AreEqual(ranks[rank] + " of " + suits[suit], texture.Value.name); }
        }
        [Test]
        public void ControllerDisposalCanBeRepeatedBySceneAndContainerOwners()
        {
            var host = new GameObject("Controller test");
            try
            {
                var document = host.AddComponent<UIDocument>();
                document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/_Project/UI/Blackjack.uxml");
                var controller = new TestScreen(new UiContext(document));
                controller.Dispose(); Assert.DoesNotThrow(controller.Dispose);
            }
            finally { UnityEngine.Object.DestroyImmediate(host); }
        }
        sealed class TestScreen : ScreenController { public TestScreen(UiContext context) : base(context, "loginScreen") { } }
        [Test]
        public async Task ExpectedFailuresNeverEscapeTheBoundary()
        {
            Assert.AreEqual("Timeout", (await ResultBoundary.RunAsync(() => Task.FromException<IResult>(new ClientTimeoutException("detail")))).ErrorCode);
            Assert.AreEqual("Network", (await ResultBoundary.RunAsync(() => Task.FromException<IResult>(new ClientTransportException("detail")))).ErrorCode);
            Assert.AreEqual("Cancelled", (await ResultBoundary.RunAsync(() => Task.FromException<IResult>(new OperationCanceledException()))).ErrorCode);
            Assert.AreEqual("Protocol", (await ResultBoundary.RunAsync(() => Task.FromException<IResult>(new ClientProtocolException("detail")))).ErrorCode);
            var rejected = await ResultBoundary.ClientAsync(() => Task.FromResult(CardGame.Contracts.Result.Fail(CardGame.Contracts.ErrorCode.RoomFull, "full")));
            Assert.AreEqual("RoomFull", rejected.ErrorCode);
        }
        [UnityTest]
        public IEnumerator LocalStorageRoundTripAndCancellation()
        {
            var storage = new LocalStorage(new UnityThread()); const string key = "blackjack.test.storage";
            var write = storage.SetAsync(key, "value"); while (!write.IsCompleted) yield return null;
            Assert.IsTrue(write.Result.Succeed);
            var read = storage.GetAsync(key); while (!read.IsCompleted) yield return null;
            Assert.AreEqual("value", read.Result.Value);
            var remove = storage.RemoveAsync(key); while (!remove.IsCompleted) yield return null;
            var missing = storage.GetAsync(key); while (!missing.IsCompleted) yield return null;
            Assert.IsNull(missing.Result.Value);
            using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
            var cancelled = storage.SetAsync(key, "never", cancellation.Token); while (!cancelled.IsCompleted) yield return null;
            Assert.AreEqual("Cancelled", cancelled.Result.ErrorCode);
        }
        [Test]
        public async Task PersistenceDeletionWaitsForPreviousWritesAndSurfacesStorageFailure()
        {
            var storage = new DelayedStorage(); var persistence = new SessionPersistence(storage, new ImmediateThread());
            IResult reported = null; persistence.Failed += result => reported = result;
            var first = persistence.ClearAsync("https://server/"); var second = persistence.ClearAsync("https://server/");
            Assert.AreEqual(1, storage.Calls);
            storage.Release.SetResult(Result.Fail("Storage", "disk failed"));
            Assert.IsFalse((await first).Succeed); Assert.IsTrue((await second).Succeed);
            Assert.AreEqual("Storage", reported.ErrorCode); Assert.AreEqual(2, storage.Calls);
            Assert.AreNotEqual(SessionPersistence.Key("https://one/"), SessionPersistence.Key("https://two/"));
        }
        sealed class ImmediateThread : IUnityThread
        { public void Post(Action action) => action(); public Task<T> InvokeAsync<T>(Func<T> action, CancellationToken token = default) => Task.FromResult(action()); }
        sealed class DelayedStorage : IStorage
        {
            public int Calls; public TaskCompletionSource<IResult> Release = new();
            public Task<IResult> RemoveAsync(string key, CancellationToken token = default) => ++Calls == 1 ? Release.Task : Task.FromResult(Result.Ok());
            public Task<IResult<string>> GetAsync(string key, CancellationToken token = default) => Task.FromResult(Result<string>.Ok(null));
            public Task<IResult> SetAsync(string key, string value, CancellationToken token = default) => Task.FromResult(Result.Ok());
        }
    }
}
