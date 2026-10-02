using System;
using CardGame.Client;

namespace Blackjack.Services
{
    // Centralizes room replacement and stale callback protection for room-dependent services.
    public abstract class RoomFeature : IDisposable
    {
        readonly IRoomConnectionSource source;
        protected readonly IUnityThread Thread;
        protected RoomConnection Room { get; private set; }
        protected RoomFeature(IRoomConnectionSource source, IUnityThread thread)
        { this.source = source; Thread = thread; source.ConnectionChanged += Rebind; }
        protected void Initialize() => Rebind();
        void Rebind()
        {
            if (ReferenceEquals(Room, source.Current)) return;
            if (Room != null) Subscribe(Room, false);
            Room = source.Current;
            if (Room != null) Subscribe(Room, true);
            OnChanged();
        }
        protected void Notify(RoomConnection room, Action action) => Thread.Post(() => { if (ReferenceEquals(Room, room)) action(); });
        protected abstract void Subscribe(RoomConnection room, bool add);
        protected abstract void OnChanged();
        public void Dispose() { source.ConnectionChanged -= Rebind; if (Room != null) Subscribe(Room, false); Room = null; }
    }
}
