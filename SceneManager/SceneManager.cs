using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadshotModAPI;

/// <summary>
/// Scene loading and asset bundle management for Deadshot mods.
/// </summary>
public static class SceneManager
{
    /// <summary>
    /// Loads a scene while keeping Deadshot's gameplay scene loaded
    /// so the existing player and gameplay systems remain available.
    /// </summary>
    /// <param name="sceneName">The name of the scene to load.</param>
    public static void LoadScene(string sceneName)
    {
        Logger.Log($"LoadScene called with: '{sceneName}'");

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Logger.Error("LoadScene received an empty scene name.");
            return;
        }

        try
        {
            Scene existingScene = default;

            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);

                if (scene.name == sceneName)
                {
                    existingScene = scene;
                    break;
                }
            }

            if (existingScene.IsValid() && existingScene.isLoaded)
            {
                Logger.Log($"Scene '{sceneName}' is already loaded. Unloading before reloading.");
                _ = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(existingScene);
            }

            bool c1l2Loaded = false;

            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);

                if (scene.name == "C1L2" && scene.isLoaded)
                {
                    c1l2Loaded = true;
                    break;
                }
            }

            if (!c1l2Loaded)
            {
                Logger.Log("Loading Deadshot gameplay scene: C1L2");

                UnityEngine.SceneManagement.SceneManager.LoadScene("C1L2", LoadSceneMode.Additive);
            }

            var waiterObject = new GameObject("DeadshotModAPI_SceneLoadWaiter");
            UnityEngine.Object.DontDestroyOnLoad(waiterObject);
            SceneLoadWaiter waiter = waiterObject.AddComponent<SceneLoadWaiter>();
            waiter.Initialize(sceneName);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load scene '{sceneName}': {ex}");
        }
    }

    /// <summary>
    /// Loads the Deadshot Mod API asset bundles from
    /// BepInEx/mods/DeadshotModAPI.
    /// </summary>
    internal static void LoadModsBundle()
    {
        try
        {
            AssetBundleManager bundleManager = new();

            List<string> bundlesFiles = new()
            {
                "DeadshotModApi/deadshotmodapi",
                "DeadshotModApi/deadshotapi_assets"
            };

            List<UniverseLib.AssetBundle> bundles = bundleManager.LoadAssetBundles(bundlesFiles);

            if (bundles == null)
            {
                Logger.Error("Failed to load asset bundles.");
                return;
            }

            Logger.Info($"Loaded {bundles.Count} asset bundles.");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load Mods Menu bundle: {ex}");
        }
    }
}