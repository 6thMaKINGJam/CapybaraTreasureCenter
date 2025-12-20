using UnityEngine;
using Firebase; // Firebase 의존성 확인을 위해 필요
using Firebase.Extensions;


public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    // Firebase 초기화 상태 확인용 변수
    public bool IsFirebaseReady { get; private set; } = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeFirebase(); // 시작 시 Firebase 상태 확인
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 인터넷 연결 가용성을 확인하는 메서드 (기획안 명칭 준수)
    /// </summary>
    public bool IsNetworkAvailable()
    {
        // NotReachable이면 false, 그 외(WIFI/데이터) true
        return Application.internetReachability != NetworkReachability.NotReachable;
    }

    /// <summary>
    /// Firebase 의존성 및 초기화 완료 여부 확인 (선택 사항 구현)
    /// </summary>
    private void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                IsFirebaseReady = true;
                Debug.Log("Firebase가 정상적으로 초기화되었습니다.");
            }
            else
            {
                IsFirebaseReady = false;
                Debug.LogError($"Firebase 의존성을 해결할 수 없습니다: {dependencyStatus}");
            }
        });
    }

    // 기존 상세 상태 확인 메서드 유지
    public void CheckNetworkStatus()
    {
        switch (Application.internetReachability)
        {
            case NetworkReachability.NotReachable:
                Debug.Log("네트워크에 연결되어 있지 않습니다.");
                break;
            case NetworkReachability.ReachableViaCarrierDataNetwork:
                Debug.Log("모바일 데이터로 연결되었습니다.");
                break;
            case NetworkReachability.ReachableViaLocalAreaNetwork:
                Debug.Log("WIFI 혹은 LAN으로 연결되었습니다.");
                break;
        }
    }
}
