using System;
using System.Threading;
using Blackjack.Services;
using Blackjack.Services.Transport;
using Cysharp.Threading.Tasks;
using Nakama;
using Reflex.Attributes;
using UnityEngine;

namespace Blackjack._Project.Scripts.Services.Auth
{
    public sealed class AuthNakama : IAuthenticationService
    {
        [Inject] NetworkTransport<Client> _networkTransport;
        public string AccountName { get; set; }
        public bool IsAuthenticated { get; set; }
        public event Action Changed;
        public ISession Session { get; set; }

        public UniTask<Result> LoginAsync(IAuthCredentials credentials, CancellationToken token = default)
        {
            return credentials switch
            {
                EmailPassword ep => EmailLogin(ep, token),
                DeviceInfo di => DeviceLogin(di, token),
                _ => UniTask.FromResult(Result.Fail("401", "Invalid login method"))
            };
        }

        public UniTask<Result> RegisterAsync(IAuthCredentials credentials, CancellationToken token = default)
        {
            return credentials switch
            {
                EmailPassword ep => EmailRegister(ep, token),
                DeviceInfo di => DeviceRegister(di, token),
                _ => UniTask.FromResult(Result.Fail("401", "Invalid login method"))
            };
        }

        public async UniTask<Result> LogoutAsync(CancellationToken token = default)
        {
            try
            {
                await _networkTransport.Client.SessionLogoutAsync(Session, null, token).AsUniTask();
            }
            catch (Exception e)
            {
                return Result.Fail("0", $"failed to logout {e.Message}");
            }

            return Result.Ok();
        }

        async UniTask<Result> DeviceRegister(DeviceInfo info, CancellationToken token = default)
        {
            try
            {
                Session = await _networkTransport.Client.AuthenticateDeviceAsync(
                    info.DeviceId, null, true, canceller: token);
            }
            catch (Exception e)
            {
                return RegistrationResult(Result.Fail("0", $"failed to register device {e.Message}"));
            }

            if (!Session.Created || Session == null)
                return RegistrationResult(Result.Fail("0", "failed to register device"));

            return RegistrationResult(Result.Ok());
        }

        async UniTask<Result> EmailRegister(EmailPassword emailPassword, CancellationToken token = default)
        {
            try
            {
                Session = await _networkTransport.Client.AuthenticateEmailAsync(emailPassword.Email,
                    emailPassword.Password, null,
                    true, canceller: token);
            }
            catch (Exception e)
            {
                return RegistrationResult(Result.Fail("0", $"failed to register email {e.Message}"));
            }

            if (!Session.Created || Session == null)
                return RegistrationResult(Result.Fail("0", "failed to register email"));

            return RegistrationResult(Result.Ok());
        }

        async UniTask<Result> DeviceLogin(DeviceInfo info, CancellationToken token = default)
        {
            try
            {
                Session = await _networkTransport.Client.AuthenticateDeviceAsync(
                    info.DeviceId, null, false, canceller: token);
            }
            catch (Exception e)
            {
                return AuthenticationResult(Result.Fail("0", $"failed to login device {e.Message}"));
            }

            if (!Session.Created || Session == null)
                return AuthenticationResult(Result.Fail("401", "failed to login device"));
            return AuthenticationResult(Result.Ok());
        }

        async UniTask<Result> EmailLogin(EmailPassword emailPassword, CancellationToken token = default)
        {
            try
            {
                Session = await _networkTransport.Client.AuthenticateEmailAsync(emailPassword.Email,
                    emailPassword.Password, null,
                    false, canceller: token);
            }
            catch (Exception e)
            {
                return AuthenticationResult(Result.Fail("0", $"failed to login email [ {e.Message} ]"));
            }

            Debug.Log($"created :{Session.Created}\nexpired :{Session.IsExpired}");
            return AuthenticationResult(Session == null ? Result.Fail("401", "failed to login email") : Result.Ok());
        }

        Result AuthenticationResult(Result result)
        {
            Changed?.Invoke();
            IsAuthenticated = result.Succeed;
            return result;
        }

        Result RegistrationResult(Result result)
        {
            Changed?.Invoke();
            return result;
        }
    }
}