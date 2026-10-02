using System;
using System.IO;
using System.Linq;
using _Project.Scripts;
using Reflex.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Blackjack.Editor
{
    public static class IntegrationSetup
    {
        const string Project = "Assets/_Project/";
        [InitializeOnLoadMethod]
        static void Initialize() => EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool("Blackjack.Setup", false)) return;
            if (AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(Project + "UI/Blackjack.uxml") == null) return;
            Configure(); SessionState.SetBool("Blackjack.Setup", true);
        };
        [MenuItem("Blackjack/Configure current scene")]
        public static void Configure()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/SampleScene.unity") return;
            var manager = UnityEngine.Object.FindAnyObjectByType<GameManager>();
            if (manager == null) manager = new GameObject("Blackjack Application").AddComponent<GameManager>();
            var table = UnityEngine.Object.FindAnyObjectByType<Gameplay>();
            if (table == null) table = manager.gameObject.AddComponent<Gameplay>();
            table.cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Project + "Prefabs/Card.prefab").GetComponent<Card>();
            table.cardDatabase = AssetDatabase.LoadAssetAtPath<CardDatabase>(Project + "Data/New Card Database.asset");
            ConfigureCards(table.cardDatabase);
            var ui = GameObject.Find("Blackjack UI") ?? new GameObject("Blackjack UI");
            var document = ui.GetComponent<UIDocument>() ?? ui.AddComponent<UIDocument>();
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(Project + "UI/Blackjack.uxml");
            const string panelPath = Project + "UI/BlackjackPanel.asset";
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(panelPath);
            if (panel == null) { panel = ScriptableObject.CreateInstance<PanelSettings>(); AssetDatabase.CreateAsset(panel, panelPath); }
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1280, 720);
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panel.match = 0.5f;
            document.panelSettings = panel;
            var scope = UnityEngine.Object.FindAnyObjectByType<ContainerScope>();
            if (scope == null) scope = new GameObject("SceneScope").AddComponent<ContainerScope>();
            var installer = scope.GetComponent<SceneInstaller>() ?? scope.gameObject.AddComponent<SceneInstaller>();
            installer.document = document; installer.table = table;
            var camera = Camera.main;
            if (camera != null) { camera.backgroundColor = new Color(0.04f, 0.13f, 0.10f); camera.orthographicSize = 5.2f; }
            EditorUtility.SetDirty(panel); EditorUtility.SetDirty(installer); EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Debug.Log("Blackjack integration configured in the current scene.");
        }
        static void ConfigureCards(CardDatabase database)
        {
            var ranks = new[] { "ace", "2", "3", "4", "5", "6", "7", "8", "9", "10", "jack", "queen", "king" };
            Texture2D[] Suit(string suit) => ranks.Select(rank => AssetDatabase.LoadAssetAtPath<Texture2D>(Project
                + "Art/Textures/png cards/card fronts/" + suit + "/" + rank + " of " + suit + ".png")).ToArray();
            database.clubs = Suit("clubs"); database.hearts = Suit("hearts"); database.spades = Suit("spades"); database.diamonds = Suit("diamonds");
            EditorUtility.SetDirty(database);
        }
        [MenuItem("Blackjack/Build Windows IL2CPP")]
        public static void BuildWindows()
        {
            Configure();
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.IL2CPP);
            Directory.CreateDirectory("Builds/Windows");
            var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/SampleScene.unity" },
                locationPathName = "Builds/Windows/Blackjack.exe", target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            File.WriteAllText("Builds/Windows/build-result.txt", result.summary.result + "\nErrors: " + result.summary.totalErrors);
        }
    }
}
