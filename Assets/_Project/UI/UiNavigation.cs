using System;
using System.Collections.Generic;
using Blackjack._Project.Scripts.Services.Auth;
using Blackjack.Services;
using LitMotion;
using UnityEngine.UIElements;

namespace Blackjack.UI
{
    public sealed class UiNavigation : IDisposable
    {
        readonly UiContext context;
        readonly IAuthenticationService auth;
        readonly Dictionary<string, ScreenController> screens;
        readonly ChatController chat;
        MotionHandle transition;
        bool disposed;
        public UiNavigation(UiContext context, IAuthenticationService auth, LoginController login, HomeController home,
            RoomsController rooms, GameController game, ChatController chat)
        {
            this.context = context; this.auth = auth; this.chat = chat;
            screens = new() { ["login"] = login, ["home"] = home, ["rooms"] = rooms, ["game"] = game };
            context.NavigationRequested += Navigate; auth.Changed += AuthenticationChanged;
            Navigate(auth.IsAuthenticated ? "home" : "login");
        }
        void AuthenticationChanged() => Navigate(auth.IsAuthenticated ? "home" : "login");
        void Navigate(string screen)
        {
            if (!auth.IsAuthenticated && screen != "login") screen = "login";
            if (!screens.ContainsKey(screen)) return;
            transition.TryCancel(); context.ActiveScreen = screen;
            foreach (var pair in screens) pair.Value.Show(pair.Key == screen);
            chat.Show(false);
            var view = context.Root.Q<VisualElement>(screen + "Screen");
            transition = LMotion.Create(0f, 1f, 0.15f).Bind(opacity => view.style.opacity = opacity);
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            transition.TryCancel(); context.NavigationRequested -= Navigate; auth.Changed -= AuthenticationChanged;
            foreach (var screen in screens.Values) screen.Dispose(); chat.Dispose();
        }
    }
}
