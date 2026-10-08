using _Project.Scripts;
using NUnit.Framework;
using UnityEngine;

namespace Blackjack.Tests
{
    public sealed class OfflineGameplayTests
    {
        [Test]
        public void OfflinePresenterCreatesNoCardsOrHandBounds()
        {
            var host = new GameObject("Offline presenter test");
            try
            {
                var presenter = host.AddComponent<Gameplay>();
                presenter.Render();
                Assert.AreEqual(0, host.transform.childCount);
                Assert.IsFalse(presenter.TryGetHandBounds("dealer", out _));
                presenter.Clear();
                Assert.AreEqual(0, host.transform.childCount);
            }
            finally { Object.DestroyImmediate(host); }
        }
    }
}
