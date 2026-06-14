using UnityEditor;
using UnityEngine;

public static class AudioDebugger
{
    [MenuItem("Flow Blast/Reset Audio Settings")]
    private static void ResetAudioSettings()
    {
        PlayerPrefs.DeleteKey("SoundEnabled");
        PlayerPrefs.DeleteKey("MusicEnabled");
        PlayerPrefs.DeleteKey("VibrateEnabled");
        PlayerPrefs.Save();
        Debug.Log("Audio PlayerPrefs cleared. Sound will now default to ON.");
    }

    [MenuItem("Flow Blast/Clear All PlayerPrefs")]
    private static void ClearAllPrefs()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("All PlayerPrefs cleared.");
    }
}