using System;
using Blackjack.Services;
using UnityEngine.UIElements;

namespace Blackjack.UI
{
    public sealed class UiContext
    {
        readonly UIDocument document;
        public VisualElement Root => document.rootVisualElement;
        public string ActiveScreen { get; internal set; }
        public event Action<string> NavigationRequested;
        public UiContext(UIDocument document) => this.document = document;
        public void Go(string screen) => NavigationRequested?.Invoke(screen);
        public void Status(string text) => Root.Q<Label>("status").text = text;
        public void Report(IResult result) { if (!result.Succeed && result.ErrorCode != "Cancelled") Status(result.ErrorMessage); }
    }
}
