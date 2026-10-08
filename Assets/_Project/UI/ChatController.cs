using System;
using Blackjack.Services;
using UnityEngine.UIElements;
using LitMotion;

namespace Blackjack.UI
{
    public sealed class ChatController : ScreenController
    {
        readonly IChatService chat;
        readonly Action toggle;
        readonly Button toggleButton;
        MotionHandle transition;

        public ChatController(UiContext context, IChatService chat) : base(context, "chatOverlay")
        {
            this.chat = chat;
            toggleButton = context.Root.Q<Button>("toggleChat");
            toggle = () => Show(View.ClassListContains("hidden"));
            toggleButton.clicked += toggle;
            Click("sendChat", async token =>
            {
                var input = Find<TextField>("chatInput");
                var result = await chat.SendAsync(input.value, token);
                if (result.Succeed) input.value = string.Empty;
                return result;
            });
        }

        public override void Refresh()
        {
            toggleButton.SetEnabled(chat.IsAvailable);
            Find<Button>("sendChat").SetEnabled(chat.IsAvailable);
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
