using System;
using System.Threading.Tasks;
using Blackjack.Services;
using CardGame.Contracts;
using UnityEngine.UIElements;
using LitMotion;

namespace Blackjack.UI
{
    public sealed class ChatController : ScreenController
    {
        readonly IChatService chat;
        readonly IRoomService rooms;
        readonly Action toggle;
        readonly Button toggleButton;
        string roomId;
        MotionHandle transition;
        public ChatController(UiContext context, IChatService chat, IRoomService rooms) : base(context, "chatOverlay")
        {
            this.chat = chat; this.rooms = rooms;
            chat.MessageReceived += Receive; chat.Changed += Refresh; rooms.Changed += Refresh;
            toggleButton = context.Root.Q<Button>("toggleChat");
            toggle = () => { if (View.ClassListContains("hidden")) Show(true); else Show(false); };
            toggleButton.clicked += toggle;
            Click("sendChat", async ct =>
            {
                var input = Find<TextField>("chatInput"); var result = await chat.SendAsync(input.value, ct);
                if (result.Succeed) input.value = ""; return result;
            });
        }
        public override void Refresh()
        {
            if (roomId != rooms.RoomId) { roomId = rooms.RoomId; Find<ScrollView>("messages").Clear(); Show(false); }
            toggleButton.SetEnabled(chat.IsAvailable);
            Find<Button>("sendChat").SetEnabled(chat.IsAvailable);
        }
        public override void Show(bool visible)
        {
            transition.TryCancel(); base.Show(visible);
            if (visible) transition = LMotion.Create(0f, 1f, 0.15f).Bind(opacity => View.style.opacity = opacity);
        }
        void Receive(ChatMessageDto message)
        {
            var messages = Find<ScrollView>("messages");
            messages.Add(new Label((message.UserId == rooms.UserId ? "You" : message.Username) + ": " + message.Message));
            while (messages.contentContainer.childCount > 100) messages.contentContainer.RemoveAt(0);
            if (messages.contentContainer.childCount > 0) messages.ScrollTo(messages.contentContainer[messages.contentContainer.childCount - 1]);
        }
        public override void Dispose()
        { transition.TryCancel(); chat.MessageReceived -= Receive; chat.Changed -= Refresh; rooms.Changed -= Refresh; toggleButton.clicked -= toggle; base.Dispose(); }
    }
}
