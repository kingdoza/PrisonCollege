#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class IntroPrefsEditor
{
    private const string INTRO_STARTED_ONCE_KEY = "MainIntroStartedOnce";

    [MenuItem("Tools/Intro/Reset Started Once")]
    private static void ResetStartedOnce()
    {
        PlayerPrefs.DeleteKey(INTRO_STARTED_ONCE_KEY);
        PlayerPrefs.Save();
        Debug.Log("MainIntroStartedOnce PlayerPrefs 값을 초기화했습니다.");
    }
}
#endif
