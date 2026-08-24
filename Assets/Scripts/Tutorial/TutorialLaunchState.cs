using UnityEngine;
using UnityEngine.SceneManagement;

public static class TutorialLaunchState
{
    private const string STARTED_ONCE_KEY = "TutorialStartedOnce";

    public static bool HasStartedOnce => PlayerPrefs.GetInt(STARTED_ONCE_KEY, 0) == 1;



    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterMainStageLaunchObservation()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }



    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (HasStartedOnce)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            return;
        }

        GameManager gameManager = GameManager.Instance;
        if (gameManager != null && gameManager.Difficulty != DifficultyLevel.None)
            MarkStartedOnce();
    }



    public static void MarkStartedOnce()
    {
        if (HasStartedOnce) return;

        PlayerPrefs.SetInt(STARTED_ONCE_KEY, 1);
        PlayerPrefs.Save();
    }



    public static void ResetStartedOnce()
    {
        PlayerPrefs.DeleteKey(STARTED_ONCE_KEY);
        PlayerPrefs.Save();
        RegisterMainStageLaunchObservation();
    }
}
