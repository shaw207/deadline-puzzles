using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PortfolioPlayer
{
    const string Root = "Assets/CompanyDay/Scenes/";
    static readonly string[] Scenes = { Root + "Memory.unity", Root + "WaterLight.unity" };

    [MenuItem("DeadLine/Build Windows")]
    public static void BuildWindows()
    {
        foreach (string path in Scenes)
        {
            var scene = EditorSceneManager.OpenScene(path);
            var components = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true));
            foreach (var t in components)
            {
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) > 0)
                    throw new InvalidDataException(path + ": missing script on " + t.name);
            }
            if (!Camera.main || !GameObject.FindGameObjectWithTag("Player"))
                throw new InvalidDataException(path + ": missing player or camera.");
        }
        var folder = Environment.GetEnvironmentVariable("DEADLINE_REVIEW_BUILD");
        if (string.IsNullOrWhiteSpace(folder))
            folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../Build/Windows"));
        Directory.CreateDirectory(folder);
        var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = Scenes, target = BuildTarget.StandaloneWindows64,
            locationPathName = Path.Combine(folder, "DeadLine-Original.exe")
        });
        if (result.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("Windows build: " + result.summary.result + ", " + result.summary.totalErrors + " errors.");
        Debug.Log("DeadLine independent Windows build verified: " + result.summary.totalSize + " bytes.");
    }
}
