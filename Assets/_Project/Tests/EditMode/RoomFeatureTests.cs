using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Blackjack.Services;
using CardGame.Client;
using NUnit.Framework;

namespace Blackjack.Tests
{
    public sealed class RoomFeatureTests
    {
        [Test]
        public void OldRoomCallbacksAreDroppedAndSubscriptionsAreReleased()
        {
            var source = new Source(); var thread = new QueuedThread(); var feature = new Feature(source, thread);
            var first = (RoomConnection)FormatterServices.GetUninitializedObject(typeof(RoomConnection));
            var second = (RoomConnection)FormatterServices.GetUninitializedObject(typeof(RoomConnection));
            source.Replace(first); feature.Emit(first); source.Replace(second); thread.Drain();
            Assert.AreEqual(0, feature.Delivered); Assert.AreEqual(1, feature.Subscriptions);
            feature.Emit(second); thread.Drain(); Assert.AreEqual(1, feature.Delivered);
            feature.Dispose(); feature.Emit(second); thread.Drain(); Assert.AreEqual(1, feature.Delivered);
            Assert.AreEqual(0, feature.Subscriptions);
        }
        sealed class Source : IRoomConnectionSource
        { public RoomConnection Current { get; private set; } public event Action ConnectionChanged; public void Replace(RoomConnection room) { Current = room; ConnectionChanged?.Invoke(); } }
        sealed class Feature : RoomFeature
        {
            public int Delivered, Subscriptions;
            public Feature(IRoomConnectionSource source, IUnityThread thread) : base(source, thread) => Initialize();
            public void Emit(RoomConnection room) => Notify(room, () => Delivered++);
            protected override void Subscribe(RoomConnection room, bool add) => Subscriptions += add ? 1 : -1;
            protected override void OnChanged() { }
        }
        sealed class QueuedThread : IUnityThread
        {
            readonly Queue<Action> pending = new();
            public void Post(Action action) => pending.Enqueue(action);
            public void Drain() { while (pending.Count > 0) pending.Dequeue()(); }
            public Task<T> InvokeAsync<T>(Func<T> action, CancellationToken token = default) => Task.FromResult(action());
        }
    }
}
