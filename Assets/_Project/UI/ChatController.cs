using System;
using Blackjack.Services;
using Blackjack.Services.Chat;
using Blackjack.Services.Room;
using UnityEngine.UIElements;
using LitMotion;
using UnityEngine;

namespace Blackjack.UI
{
    public sealed class ChatController : ScreenController
    {
        readonly IChatService _chat;
        readonly Action toggle;
        readonly Button toggleButton;
        MotionHandle transition;

        public ChatController(UiContext context, IChatService chat) : base(context, "chatOverlay")
        {
            _chat = chat;

            toggleButton = context.Root.Q<Button>("toggleChat");
            toggle = () => Show(View.ClassListContains("hidden"));
            toggleButton.clicked += toggle;
            chat.OnMessageReceived += ChatOnOnMessageReceived;
            Click("sendChat", async token =>
            {
                var input = Find<TextField>("chatInput");
                var result = await chat.SendAsync(input.value, token);
                if (result.Succeed) input.value = string.Empty;
                return result;
            });
        }

        void ChatOnOnMessageReceived(string user, string content)
        {
            Debug.Log($"channel received {user} : {content}");
            var messages = Find<ScrollView>("messages");

            if (content == "/clear")
            {
                messages.contentContainer.Clear();

                return;
            }


            var label = new Label($"{user}: {content}");
            messages.Add(label);

            while (messages.contentContainer.childCount > 100)
                messages.contentContainer.RemoveAt(0);

            label.RegisterCallbackOnce<GeometryChangedEvent>(_ => messages.ScrollTo(label));
            Refresh();
        }

        public override void Refresh()
        {
            toggleButton.SetEnabled(_chat.IsAvailable);
            Find<Button>("sendChat").SetEnabled(_chat.IsAvailable);
        }

        public override void Show(bool visible)
        {
            transition.TryCancel();
            base.Show(visible);
            if (visible) transition = LMotion.Create(0f, 1f, 0.15f).Bind(opacity => View.style.opacity = opacity);
        }

        public override void Dispose()
        {
            transition.TryCancel();
            toggleButton.clicked -= toggle;
            base.Dispose();
        }
    }
}