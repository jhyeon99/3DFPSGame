using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoader
{
    private const string LoadingScene = "LoadingScene";

    public static void Load(string sceneName)
    {
        SceneManager.LoadScene(LoadingScene);
    }
}
