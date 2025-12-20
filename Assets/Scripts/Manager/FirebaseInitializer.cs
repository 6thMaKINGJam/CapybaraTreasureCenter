using Firebase;
using Firebase.Extensions;
using UnityEngine;

public class FirebaseInitializer : MonoBehaviour
{
    void Start()
    {
        // 플랫폼에 관계없이 Firebase 의존성(Google Play 서비스 등) 상태를 확인합니다.
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                // 초기화 성공 - 여기서 Database나 Analytics를 시작합니다.
                FirebaseApp app = FirebaseApp.DefaultInstance;
                Debug.Log("Firebase 초기화 성공 (플랫폼: " + Application.platform + ")");
            }
            else
            {
                Debug.LogError($"Firebase 의존성을 해결할 수 없습니다: {dependencyStatus}");
            }
        });
    }
}