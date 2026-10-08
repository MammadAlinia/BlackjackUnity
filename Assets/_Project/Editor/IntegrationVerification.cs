using System;
using System.IO;
using System.Linq;
using _Project.Scripts;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.UIElements;

namespace Blackjack.Editor
{
    // Allows repeatable verification in the already-open Editor without editing scene YAML.
    [InitializeOnLoad]
    public static class IntegrationVerification
    {
        const string CommandPath = "Temp/Blackjack-command.json";
        static double nextUpdate;
        static readonly double readyAfter = EditorApplication.timeSinceStartup + 5;
        static TestRunnerApi runner;
        [Serializable] sealed class Command { public string action; }
        [Serializable] sealed class Status
        { public bool compiling, playing, configured; public string activeScreen, lastAction, result; }
        static readonly Status status = new();
        static IntegrationVerification()
        {
            runner = ScriptableObject.CreateInstance<TestRunnerApi>(); runner.RegisterCallbacks(new Callbacks());
            EditorApplication.update += Update;
        }
        static void Update()
        {
            if (EditorApplication.timeSinceStartup < nextUpdate) return;
            nextUpdate = EditorApplication.timeSinceStartup + 1;
            status.compiling = EditorApplication.isCompiling; status.playing = EditorApplication.isPlaying;
            status.configured = UnityEngine.Object.FindAnyObjectByType<SceneInstaller>() != null;
            var document = UnityEngine.Object.FindAnyObjectByType<UIDocument>();
            status.activeScreen = "";
            if (document != null && document.rootVisualElement != null)
                foreach (var name in new[] { "login", "home", "rooms", "game" })
                {
                    var screen = document.rootVisualElement.Q(name + "Screen");
                    if (screen != null && !screen.ClassListContains("hidden")) status.activeScreen = name;
                }
            Directory.CreateDirectory("Temp");
            File.WriteAllText("Temp/Blackjack-status.json", JsonUtility.ToJson(status, true));
            if (status.compiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup < readyAfter || !File.Exists(CommandPath)) return;
            var command = JsonUtility.FromJson<Command>(File.ReadAllText(CommandPath)); File.Delete(CommandPath);
            status.lastAction = command.action; status.result = "Running";
            try
            {
                switch (command.action)
                {
                    case "configure": IntegrationSetup.Configure(); status.result = "Configured"; break;
                    case "open-scene":
                        var active = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
                        if (active.isDirty) { status.result = "Save the current scene before opening SampleScene."; break; }
                        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
                        IntegrationSetup.Configure(); status.result = "Scene opened"; break;
                    case "edit-tests": RunTests(TestMode.EditMode); break;
                    case "play-tests": RunTests(TestMode.PlayMode); break;
                    case "list-tests": runner.RetrieveTestList(TestMode.PlayMode, root => File.WriteAllLines("Temp/Blackjack-test-list.txt", Names(root))); break;
                    case "build": IntegrationSetup.BuildWindows(); status.result = File.ReadAllText("Builds/Windows/build-result.txt"); break;
                    case "play": EditorApplication.isPlaying = true; break;
                    case "stop": EditorApplication.isPlaying = false; break;
                    case "capture": ScreenCapture.CaptureScreenshot("Temp/blackjack-ui.png"); status.result = "Capture requested"; break;
                    default: status.result = "Unknown command"; break;
                }
            }
            catch (Exception exception) { status.result = exception.GetType().Name + ": " + exception.Message; Debug.LogException(exception); }
        }
        static System.Collections.Generic.IEnumerable<string> Names(ITestAdaptor test) => new[] { test.FullName }.Concat(test.Children.SelectMany(Names));
        static void RunTests(TestMode mode)
        {
            runner.Execute(new ExecutionSettings(new Filter { testMode = mode, assemblyNames = new[] { mode == TestMode.EditMode ? "Blackjack.EditTests" : "Blackjack.PlayTests" } }));
        }
        sealed class Callbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor tests) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            { TestRunnerApi.SaveResultToFile(result, "Temp/Blackjack-test-results.xml"); status.result = result.Test.TestCaseCount == 0 ? "No tests discovered" : result.TestStatus.ToString(); }
        }
    }
}
