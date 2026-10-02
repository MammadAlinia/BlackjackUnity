#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using UnityEngine;
using Blackjack.Services;

namespace Blackjack.Verification
{
    public static class DevelopmentSmoke
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static async void Run()
        {
            var arguments = Environment.GetCommandLineArgs();
            var previewIndex = Array.IndexOf(arguments, "-blackjackPreview");
            if (previewIndex >= 0 && previewIndex + 1 < arguments.Length)
            {
                var preview = await ResultBoundary.RunAsync(async () =>
                {
                    var message = await UiPreview.RunAsync(arguments[previewIndex + 1]);
                    return message == "Passed" ? Result.Ok() : Result.Fail("Preview", message);
                });
                File.WriteAllText(Path.Combine(Application.dataPath, "../preview-result.txt"), preview.Succeed ? "Passed" : preview.ErrorCode + ": " + preview.ErrorMessage);
                Application.Quit(preview.Succeed ? 0 : 1); return;
            }
            var index = Array.IndexOf(arguments, "-blackjackSmoke");
            if (index < 0 || index + 1 >= arguments.Length) return;
            var result = await IntegrationScenario.RunAsync(arguments[index + 1]);
            var path = Path.Combine(Application.dataPath, "../smoke-result.txt");
            File.WriteAllText(path, result.Succeed ? "Passed" : result.ErrorCode + ": " + result.ErrorMessage);
            Application.Quit(result.Succeed ? 0 : 1);
        }
    }
}
#endif
