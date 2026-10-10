using Blackjack.Services;
using _Project.Scripts;
using Blackjack._Project.Scripts.Services.Auth;
using Blackjack.Services.Chat;
using Blackjack.Services.Room;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Blackjack.UI;

namespace Blackjack.Tests
{
    public sealed class ServiceTests
    {
        [TestCase("AC", 0)]
        [TestCase("10H", 22)]
        [TestCase("QS", 37)]
        [TestCase("KD", 51)]
        public void CardNamesMapToThePreservedGraphicOrder(string card, int expected) =>
            Assert.AreEqual(expected, CardDatabase.Parse(card).Value);

        [TestCase("")]
        [TestCase("ZZ")]
        [TestCase("14C")]
        [TestCase("0H")]
        public void InvalidCardNamesReturnFailures(string card) => Assert.IsFalse(CardDatabase.Parse(card).Succeed);

        [Test]
        public void All52TexturesMatchRankAndSuit()
        {
            var database = AssetDatabase.LoadAssetAtPath<CardDatabase>("Assets/_Project/Data/New Card Database.asset");
            var ranks = new[] { "ace", "2", "3", "4", "5", "6", "7", "8", "9", "10", "jack", "queen", "king" };
            var suits = new[] { "clubs", "hearts", "spades", "diamonds" };

            for (var suit = 0; suit < 4; suit++)
            for (var rank = 0; rank < 13; rank++)
            {
                var texture = database.GetCardTexture(suit * 13 + rank);
                Assert.IsTrue(texture.Succeed);
                Assert.AreEqual(ranks[rank] + " of " + suits[suit], texture.Value.name);
            }
        }

        [Test]
        public void OfflineAuthenticationNavigatesThroughInMemoryState()
        {
            var auth = new AuthMockEmailPassword();
            Assert.IsFalse(auth.IsAuthenticated);
            Assert.IsTrue(auth.LoginAsync(new EmailPassword("player@example.test", "ignored"))
                .GetAwaiter().GetResult().Succeed);
            Assert.IsTrue(auth.IsAuthenticated);
            Assert.AreEqual("player@example.test", auth.AccountName);
            Assert.IsTrue(auth.LogoutAsync().GetAwaiter().GetResult().Succeed);
            Assert.IsFalse(auth.IsAuthenticated);
        }

        [Test]
        public void OfflineNavigationMovesBetweenLoginHomeRoomsAndGame()
        {
            var host = new GameObject("Offline UI test");
            var tableHost = new GameObject("Offline table test");
            UiNavigation navigation = null;

            try
            {
                var document = host.AddComponent<UIDocument>();
                document.visualTreeAsset =
                    AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/_Project/UI/Blackjack.uxml");
                var context = new UiContext(document);
                var auth = new AuthMockEmailPassword();
                var rooms = new OfflineRoomService();
                var login = new LoginController(context, auth);
                var home = new HomeController(context, auth);
                var roomScreen = new RoomsController(context, rooms);
                var gameScreen = new GameController(context, rooms, new OfflineGameSessionService(),
                    tableHost.AddComponent<Gameplay>());
                var chat = new ChatController(context, new ChatService());
                navigation = new UiNavigation(context, auth, login, home, roomScreen, gameScreen, chat);

                Assert.AreEqual("login", context.ActiveScreen);
                Assert.IsTrue(auth.LoginAsync(new EmailPassword("player@example.test", "ignored")).GetAwaiter()
                    .GetResult().Succeed);
                Assert.AreEqual("home", context.ActiveScreen);
                context.Go("rooms");
                Assert.AreEqual("rooms", context.ActiveScreen);
                context.Go("game");
                Assert.AreEqual("game", context.ActiveScreen);
                Assert.IsTrue(auth.LogoutAsync().GetAwaiter().GetResult().Succeed);
                Assert.AreEqual("login", context.ActiveScreen);
            }
            finally
            {
                navigation?.Dispose();
                Object.DestroyImmediate(tableHost);
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void OfflineRoomsAreEmptyAndRoomActionsSucceedWithoutState()
        {
            var rooms = new OfflineRoomService();
            var list = rooms.ListAsync().GetAwaiter().GetResult();
            Assert.IsTrue(list.Succeed);
            Assert.IsEmpty(list.Value);
            Assert.IsTrue(rooms.CreateAsync("ignored", true, 5).GetAwaiter().GetResult().Succeed);
            Assert.IsTrue(rooms.JoinAsync("ignored").GetAwaiter().GetResult().Succeed);
            Assert.IsTrue(rooms.LeaveAsync().GetAwaiter().GetResult().Succeed);
            Assert.IsTrue(rooms.CloseAsync().GetAwaiter().GetResult().Succeed);
        }

        [Test]
        public void OfflineChatAndGameplayActionsSucceedWithoutProducingState()
        {
            var chat = new ChatService();
            var game = new OfflineGameSessionService();
            Assert.IsTrue(chat.IsAvailable);
            Assert.IsTrue(chat.SendAsync("discarded").GetAwaiter().GetResult().Succeed);
            Assert.IsTrue(game.ReadyAsync(true).GetAwaiter().GetResult().Succeed);
            Assert.IsTrue(game.ActionAsync("Hit").GetAwaiter().GetResult().Succeed);
            Assert.IsTrue(game.RefreshAsync().GetAwaiter().GetResult().Succeed);
        }

        [Test]
        public void ControllerDisposalCanBeRepeatedBySceneAndContainerOwners()
        {
            var host = new GameObject("Controller test");

            try
            {
                var document = host.AddComponent<UIDocument>();
                document.visualTreeAsset =
                    AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/_Project/UI/Blackjack.uxml");
                var controller = new TestScreen(new UiContext(document));
                controller.Dispose();
                Assert.DoesNotThrow(controller.Dispose);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        sealed class TestScreen : ScreenController
        {
            public TestScreen(UiContext context) : base(context, "loginScreen")
            {
            }
        }
    }
}