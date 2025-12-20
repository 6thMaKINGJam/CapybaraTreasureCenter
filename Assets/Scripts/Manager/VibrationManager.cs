using UnityEngine;
using System.Collections;

// 진동 매니저 - 싱글톤
public class VibrationManager : MonoBehaviour
{
    private static VibrationManager instance;
    public static VibrationManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("VibrationManager");
                instance = go.AddComponent<VibrationManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    // 진동 실행 (패턴 + 딜레이)
    public void Vibrate(VibrationPattern pattern, float delay = 0f, long[] customPattern = null)
    {
        if (delay > 0f)
        {
            StartCoroutine(VibrateAfterDelay(pattern, delay, customPattern));
        }
        else
        {
            ExecuteVibration(pattern, customPattern);
        }
    }

    private IEnumerator VibrateAfterDelay(VibrationPattern pattern, float delay, long[] customPattern)
    {
        yield return new WaitForSeconds(delay);
        ExecuteVibration(pattern, customPattern);
    }

    private void ExecuteVibration(VibrationPattern pattern, long[] customPattern)
    {
#if UNITY_ANDROID || UNITY_IOS
        switch (pattern)
        {
            case VibrationPattern.None:
                // 진동 없음
                break;

            case VibrationPattern.Short:
                Handheld.Vibrate();
                break;

            case VibrationPattern.Long:
                VibrateWithDuration(500);
                break;

            case VibrationPattern.Double:
                StartCoroutine(VibratePattern(new long[] { 0, 100, 100, 100 }));
                break;

            case VibrationPattern.Triple:
                StartCoroutine(VibratePattern(new long[] { 0, 100, 100, 100, 100, 100 }));
                break;

            case VibrationPattern.Success:
                StartCoroutine(VibratePattern(new long[] { 0, 100, 50, 100, 50, 300 }));
                break;

            case VibrationPattern.Warning:
                StartCoroutine(VibratePattern(new long[] { 0, 300, 100, 100, 100, 300 }));
                break;

            case VibrationPattern.Custom:
                if (customPattern != null && customPattern.Length > 0)
                {
                    StartCoroutine(VibratePattern(customPattern));
                }
                break;
        }
#else
        Debug.Log($"[VibrationManager] Vibration triggered: {pattern} (Editor Mode - No actual vibration)");
#endif
    }

    // Android/iOS에서 지정된 시간만큼 진동
    private void VibrateWithDuration(long milliseconds)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
        using (AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
        {
            vibrator.Call("vibrate", milliseconds);
        }
#elif UNITY_IOS && !UNITY_EDITOR
        Handheld.Vibrate();
#endif
    }

    // 패턴 배열로 진동 (Android 전용, iOS는 단순 진동만 지원)
    private IEnumerator VibratePattern(long[] pattern)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
        using (AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
        {
            vibrator.Call("vibrate", pattern, -1);
        }
        
        // 패턴 총 시간만큼 대기
        long totalTime = 0;
        foreach (long time in pattern)
        {
            totalTime += time;
        }
        yield return new WaitForSeconds(totalTime / 1000f);
#elif UNITY_IOS && !UNITY_EDITOR
        // iOS는 패턴 진동 미지원, 대신 여러 번 진동
        for (int i = 0; i < pattern.Length; i += 2)
        {
            if (i + 1 < pattern.Length)
            {
                yield return new WaitForSeconds(pattern[i] / 1000f);
                Handheld.Vibrate();
                yield return new WaitForSeconds(pattern[i + 1] / 1000f);
            }
        }
#else
        yield return null;
#endif
    }

    // 진동 중지 (Android 전용)
    public void CancelVibration()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
        using (AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
        {
            vibrator.Call("cancel");
        }
#endif
    }
}