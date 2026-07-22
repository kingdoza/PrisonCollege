#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class TutorialPrefsEditor
{
    private const string TUTORIAL_STARTED_ONCE_KEY = "TutorialStartedOnce";

    [MenuItem("Tools/Tutorial/Reset Started Once")]
    private static void ResetStartedOnce()
    {
        PlayerPrefs.DeleteKey(TUTORIAL_STARTED_ONCE_KEY);
        PlayerPrefs.Save();
        Debug.Log("TutorialStartedOnce PlayerPrefs 값을 초기화했습니다.");
    }
}
#endif
