using UnityEngine;

public static class IntroPlaybackState
{
    private const string STARTED_ONCE_KEY = "MainIntroStartedOnce";

    public static bool HasStartedOnce => PlayerPrefs.GetInt(STARTED_ONCE_KEY, 0) == 1;



    public static void MarkStartedOnce()
    {
        if (HasStartedOnce) return;

        PlayerPrefs.SetInt(STARTED_ONCE_KEY, 1);
        PlayerPrefs.Save();
    }
}
