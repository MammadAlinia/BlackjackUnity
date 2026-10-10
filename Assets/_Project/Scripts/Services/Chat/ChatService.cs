using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Blackjack._Project.Scripts.Services.Auth;
using Blackjack.Services.Room;
using Blackjack.Services.Transport;
using Cysharp.Threading.Tasks;
using Nakama;
using Nakama.TinyJson;
using Reflex.Attributes;
using UnityEngine;

namespace Blackjack.Services.Chat
{
    public interface IChatService
    {
        bool IsAvailable { get; }
        UniTask<Result> SendAsync(string message, CancellationToken token = default);
        event Action<string, string> OnMessageReceived;
    }

    public sealed class NakamaChatService : IChatService
    {
        [Inject] AuthNakama _auth;
        [Inject] NakamaRoomService _room;
        [Inject] NakamaTransport _transport;
        public bool IsAvailable { get; set; }

        ISocket _connection;
        IChannel _channel;


        [Inject]
        public void Initialize()
        {
            _room.joinedMatch += RoomOnJoinedMatch;
            _room.leftMatch += RoomOnLeftMatch;
        }

        void RoomOnLeftMatch()
        {
            if (IsAvailable)
                _connection.ReceivedChannelMessage -= OnChatReceived;
            OnMessageReceived?.Invoke("server", "/clear");
            IsAvailable = false;
        }

        async void RoomOnJoinedMatch(IMatch obj)
        {
            IsAvailable = true;
            var joinResult = await TryJoiningMatchChat(_room.GetCurrentMatch());
            if (!joinResult.Succeed) IsAvailable = false;
            _channel = joinResult.Value.channel;
            _connection = joinResult.Value.connection;
            _connection.ReceivedChannelMessage += OnChatReceived;
            Debug.Log(_channel.ToJson());
            Debug.Log(_connection.ToJson());
        }

        async void OnChatReceived(IApiChannelMessage x)
        {
            await UniTask.SwitchToMainThread();
            var message = x.Content.FromJson<Dictionary<string, string>>();

            OnMessageReceived?.Invoke(x.Username, message["message"]);
        }

        public async UniTask<Result<(ISocket connection, IChannel channel)>> TryJoiningMatchChat(IMatch match)
        {
            var connection = await _room.TryConnectSocket();
            if (!connection.Succeed)
                return Result.Fail
                    <(ISocket connection, IChannel channel)>
                    ("0", "Failed to connect to server");
            var socket = connection.Value;
            IChannel channel = null;

            try
            {
                channel = await socket.JoinChatAsync(match.Id, ChannelType.Room);
            }
            catch (Exception e)
            {
                return Result.Fail
                    <(ISocket connection, IChannel channel)>
                    ("0", $"Failed to join chat room {e.Message}");
            }

            return Result.Ok((socket, channel));
        }

        public async UniTask<Result> SendAsync(string message, CancellationToken token = default)

        {
            var messageContent = new Dictionary<string, string>
            {
                { "message", $"{message}" }
            };

            try
            {
                var acc = await _connection.WriteChatMessageAsync(_channel, messageContent.ToJson());
                Debug.Log(acc.ToJson());
            }
            catch (Exception e)
            {
                return Result.Fail("", e.Message);
            }

            return Result.Ok();
        }

        public event Action<string, string> OnMessageReceived = delegate { };
    }

    public sealed class ChatService : IChatService
    {
        public bool IsAvailable => true;

        public UniTask<Result> SendAsync(string message, CancellationToken token = default) =>
            UniTask.FromResult(Result.Ok());

        public event Action<string, string> OnMessageReceived;
    }
}