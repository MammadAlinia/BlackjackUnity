using System;
using System.Threading;
using Blackjack.Services;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Blackjack._Project.Scripts.Services.Auth
{
    public interface IAuthCredentials
    {
    }


    public record EmailPassword(string Email, string Password) : IAuthCredentials
    {
        public string Email { get; } = Email;
        public string Password { get; } = Password;
    }

    public record DeviceInfo : IAuthCredentials
    {
        public string DeviceId { get; } = SystemInfo.deviceUniqueIdentifier;
    }

    public interface IAuthenticationService
    {
        UniTask<Result> LoginAsync(IAuthCredentials credentials, CancellationToken token = default);
        UniTask<Result> RegisterAsync(IAuthCredentials credentials, CancellationToken token = default);
        UniTask<Result> LogoutAsync(CancellationToken token = default);
        string AccountName { get; set; }
        bool IsAuthenticated { get; set; }
        event Action Changed;
    }

    public sealed class AuthMockEmailPassword : IAuthenticationService
    {
        public UniTask<Result> LoginAsync(IAuthCredentials credentials, CancellationToken token = default)
        {
            IsAuthenticated = true;
            Changed?.Invoke();
            return UniTask.FromResult(Result.Ok());
        }

        public UniTask<Result> RegisterAsync(IAuthCredentials credentials, CancellationToken token = default)
        {
            Changed?.Invoke();

            return UniTask.FromResult(Result.Ok());
        }

        public UniTask<Result> LogoutAsync(CancellationToken token = default)
        {
            IsAuthenticated = false;
            Changed?.Invoke();

            return UniTask.FromResult(Result.Ok());
        }

        public string AccountName { get; set; }
        public bool IsAuthenticated { get; set; }
        public event Action Changed;
    }
}