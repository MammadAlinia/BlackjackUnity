using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CardGame.Client;
using UnityEngine;

namespace Blackjack.Services
{
    public sealed class GoogleLogin : IGoogleLogin
    {
        readonly IUnityThread thread;
        public GoogleLogin(IUnityThread thread) => this.thread = thread;
        public Task<IResult<ClientSession>> LoginAsync(CardGameClient client, CancellationToken token) => ResultBoundary.RunAsync(async () =>
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(TimeSpan.FromMinutes(3));
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            using var stop = deadline.Token.Register(listener.Stop);
            try
            {
                var redirect = "http://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port + "/oauth/google/callback";
                var verifier = RandomProof(); var state = RandomProof();
                using var sha = SHA256.Create();
                var challenge = Base64Url(sha.ComputeHash(Encoding.ASCII.GetBytes(verifier)));
                var url = new Uri(client.ServerUri, "auth/google/login").AbsoluteUri + "?redirectUri=" + Uri.EscapeDataString(redirect)
                    + "&codeChallenge=" + challenge + "&state=" + state;
                await thread.InvokeAsync(() => { Application.OpenURL(url); return true; }, deadline.Token);
                while (true)
                {
                    using var connection = await listener.AcceptTcpClientAsync();
                    using var stream = connection.GetStream();
                    var read = ReadCallbackAsync(stream, redirect, state);
                    if (await Task.WhenAny(read, Task.Delay(TimeSpan.FromSeconds(5), deadline.Token)) != read)
                    { deadline.Token.ThrowIfCancellationRequested(); continue; }
                    var callback = await read;
                    if (callback == null) continue;
                    if (callback.TryGetValue("error", out _)) return Result<ClientSession>.Fail("GoogleDenied", "Google sign-in was cancelled or rejected.");
                    if (!callback.TryGetValue("code", out var code)) return Result<ClientSession>.Fail("Protocol", "Google login did not return an authorization code.");
                    return await ResultBoundary.ClientAsync(() => client.ExchangeGoogleCodeAsync(code, verifier, redirect, deadline.Token));
                }
            }
            catch (Exception) when (deadline.IsCancellationRequested)
            { return Result<ClientSession>.Fail(token.IsCancellationRequested ? "Cancelled" : "Timeout", "Google sign-in did not complete."); }
            finally { listener.Stop(); }
        });
        static async Task<Dictionary<string, string>> ReadCallbackAsync(NetworkStream stream, string redirect, string state)
        {
            // Bound input before parsing. Only this loopback GET endpoint is accepted.
            var bytes = new byte[4096]; var length = 0;
            while (length < bytes.Length)
            {
                var count = await stream.ReadAsync(bytes, length, bytes.Length - length);
                if (count == 0) break;
                length += count;
                if (Encoding.ASCII.GetString(bytes, 0, length).Contains("\r\n\r\n")) break;
            }
            var request = Encoding.ASCII.GetString(bytes, 0, length).Split('\n')[0].Trim().Split(' ');
            var values = new Dictionary<string, string>();
            var valid = request.Length == 3 && request[0] == "GET" && request[1].StartsWith("/oauth/google/callback?");
            if (valid && Uri.TryCreate(redirect + request[1].Substring("/oauth/google/callback".Length), UriKind.Absolute, out var uri))
                foreach (var pair in uri.Query.TrimStart('?').Split('&'))
                {
                    var parts = pair.Split(new[] { '=' }, 2);
                    if (parts.Length == 2) values[Uri.UnescapeDataString(parts[0])] = Uri.UnescapeDataString(parts[1]);
                }
            valid &= values.TryGetValue("state", out var returned) && returned == state;
            var body = valid ? "Sign-in received. You can return to Blackjack." : "Invalid callback.";
            var reply = Encoding.UTF8.GetBytes("HTTP/1.1 " + (valid ? "200 OK" : "400 Bad Request")
                + "\r\nContent-Type: text/plain; charset=utf-8\r\nCache-Control: no-store\r\nConnection: close\r\nContent-Length: "
                + Encoding.UTF8.GetByteCount(body) + "\r\n\r\n" + body);
            await stream.WriteAsync(reply, 0, reply.Length);
            return valid ? values : null;
        }
        static string RandomProof() { using var random = RandomNumberGenerator.Create(); var bytes = new byte[32]; random.GetBytes(bytes); return Base64Url(bytes); }
        static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
