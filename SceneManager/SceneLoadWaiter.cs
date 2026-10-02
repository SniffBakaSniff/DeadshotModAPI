using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadshotModAPI;

internal class SceneLoadWaiter : MonoBehaviour
{
    private string _sceneName = string.Empty;
    private GameObject _player;
    private bool _initialized;
    private bool _sceneLoadRequested;
    private bool _playerPlaced;

    internal void Initialize(string sceneName)
    {
        try
        {
            Logger.Log($"SceneLoadWaiter initialized with scene: '{sceneName}'");

            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Logger.Error("SceneLoadWaiter received an empty scene name.");

                Destroy(gameObject);
                return;
            }

            _sceneName = sceneName;
        }
        catch (Exception ex)
        {
            Logger.Error($"SceneLoadWaiter.Initialize failed: {ex}");
        }
    }

    internal void Update()
    {
        try
        {
            if (string.IsNullOrEmpty(_sceneName))
            {
                return;
            }

            if (!_initialized)
            {
                InitializePlayer();
            }

            Scene customScene = GetCustomScene();

            if (!customScene.IsValid() || !customScene.isLoaded)
            {
                if (!_sceneLoadRequested)
                {
                    _sceneLoadRequested = true;

                    Logger.Log($"Loading custom scene: {_sceneName}");

                    UnityEngine.SceneManagement.SceneManager.LoadScene(_sceneName, LoadSceneMode.Additive);
                }

                return;
            }

            if (_playerPlaced)
            {
                return;
            }

            // Unity's overloaded == also catches destroyed objects.
            if (_player == null)
            {
                Logger.Error("Player is not available yet; cannot place player.");
                return;
            }

            Logger.Log($"Custom scene loaded: {_sceneName}");

            GameObject spawnPoint = FindCustomPlayerSpawn(customScene);

            if (spawnPoint == null)
            {
                Logger.Error("Could not find CustomPlayerSpawn.");
                return;
            }

            CharacterController controller = _player.GetComponent<CharacterController>();

            if (controller == null)
            {
                Logger.Error("Failed to find player controller.");
                return;
            }

            controller.enabled = false;

            try
            {
                _player.transform.SetPositionAndRotation(
                    spawnPoint.transform.position,
                    spawnPoint.transform.rotation
                );
            }
            finally
            {
                controller.enabled = true;
            }

            Logger.Log($"Moved player to CustomPlayerSpawn: {spawnPoint.transform.position}");

            _playerPlaced = true;
        }
        catch (Exception ex)
        {
            Logger.Error($"SceneLoadWaiter.Update failed: {ex}");
        }
    }

    private void InitializePlayer()
    {
        try
        {
            if (!GetPlayer()) return;
            if (!DisableAllCameras()) return;

            DisableMenuAndSceneRenderers();

            _initialized = true;
        }
        catch (Exception ex)
        {
            Logger.Error($"SceneLoadWaiter.InitializePlayer failed: {ex}");
        }
    }

    private bool GetPlayer()
    {
        Deadshot.GameManager gameManager = Deadshot.GameManager.INSTANCE;

        if (gameManager == null)
        {
            Logger.Error("Could not find GameManager.");
            return false;
        }

        if (gameManager.PlayerManager == null)
        {
            Logger.Error("Could not find PlayerManager.");
            return false;
        }

        _player = gameManager.PlayerManager.gameObject;

        if (_player == null)
        {
            Logger.Error("Could not find player GameObject.");
            return false;
        }

        Logger.Log($"Found player: {_player.name}");
        return true;
    }

    private bool DisableAllCameras()
    {
        Transform playerCameraTransform = _player.transform.Find("Head/MainCamera");

        if (playerCameraTransform == null)
        {
            Logger.Error("Could not find Player/Head/MainCamera.");
            return false;
        }

        Camera playerCamera = playerCameraTransform.GetComponent<Camera>();

        if (playerCamera == null)
        {
            Logger.Error("Player MainCamera has no Camera component.");
            return false;
        }

        foreach (Camera camera in FindObjectsByType<Camera>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None))
        {
            if (camera == null)
            {
                continue;
            }

            camera.enabled = camera == playerCamera;
        }

        return true;
    }

    private void DisableMenuAndSceneRenderers()
    {
        foreach (Canvas canvas in FindObjectsByType<Canvas>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None))
        {
            if (canvas == null)
            {
                continue;
            }

            if (canvas.gameObject.scene.name == "MainMenu")
            {
                canvas.enabled = false;
            }
        }

        foreach (Renderer renderer in FindObjectsByType<Renderer>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None))
        {
            if (renderer == null)
            {
                continue;
            }

            if (renderer.gameObject.scene.name != "C1L2")
            {
                continue;
            }

            if (renderer.transform.IsChildOf(_player.transform))
            {
                continue;
            }

            renderer.enabled = false;
        }
    }

    private Scene GetCustomScene()
    {
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
        {
            Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);

            if (scene.name == _sceneName)
            {
                return scene;
            }
        }

        return default;
    }

    private static GameObject FindCustomPlayerSpawn(Scene customScene)
    {
        try
        {
            if (!customScene.IsValid() || !customScene.isLoaded)
            {
                return null;
            }

            foreach (GameObject root in customScene.GetRootGameObjects())
            {
                if (root == null)
                {
                    continue;
                }

                GameObject spawn = FindChildRecursive(root.transform, "CustomPlayerSpawn");

                if (spawn != null)
                {
                    return spawn;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"SceneLoadWaiter.FindCustomPlayerSpawn failed: {ex}");
        }

        return null;
    }

    private static GameObject FindChildRecursive(Transform parent, string name)
    {
        if (parent == null)
        {
            return null;
        }

        if (parent.name == name)
        {
            return parent.gameObject;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child == null)
            {
                continue;
            }

            if (child.name == name)
            {
                return child.gameObject;
            }
        }

        return null;
    }
}