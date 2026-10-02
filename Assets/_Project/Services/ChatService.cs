using System;
using System.Threading;
using System.Threading.Tasks;
using CardGame.Client;
using CardGame.Contracts;

namespace Blackjack.Services
{
    public interface IChatService
    {
        bool IsAvailable { get; }
        event Action Changed;
        event Action<ChatMessageDto> MessageReceived;
        Task<IResult> SendAsync(string message, CancellationToken token = default);
    }
    public sealed class ChatService : RoomFeature, IChatService
    {
        Action<ChatMessageDto> received;
        Action<RoomConnectionState> connection;
        public bool IsAvailable => Room?.IsConnected == true && Room.Chat.IsAvailable;
        public event Action Changed;
        public event Action<ChatMessageDto> MessageReceived;
        public ChatService(IRoomConnectionSource source, IUnityThread thread) : base(source, thread) => Initialize();
        protected override void Subscribe(RoomConnection room, bool add)
        {
            if (add)
            {
                received = message => Notify(room, () => MessageReceived?.Invoke(message));
                connection = _ => Notify(room, OnChanged);
                room.Chat.MessageReceived += received; room.StateChanged += connection;
            }
            else { room.Chat.MessageReceived -= received; room.StateChanged -= connection; }
        }
        protected override void OnChanged() => Changed?.Invoke();
        public Task<IResult> SendAsync(string message, CancellationToken token = default) => !IsAvailable
            ? Task.FromResult(Result.Fail("UnsupportedMessage", "Chat is unavailable in this room.")) : ResultBoundary.ClientAsync(() => Room.Chat.SendAsync(message, token));
    }
}
