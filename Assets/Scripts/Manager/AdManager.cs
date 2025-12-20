using System;
using System.Collections;
using UnityEngine;
using Unity.Services.LevelPlay;

public class AdManager : MonoBehaviour
{
    // ====================================================
    // 1. 싱글톤 패턴 (Singleton)
    // ====================================================
    private static AdManager instance;
    public static AdManager Instance
    {
        get
        {
            if (instance == null)
            {
                // 씬에 이미 존재하는지 확인
                instance = FindObjectOfType<AdManager>();

                // 없다면 새로 생성
                if (instance == null)
                {
                    GameObject go = new GameObject("AdManager");
                    instance = go.AddComponent<AdManager>();
                }
            }
            return instance;
        }
    }

    // ====================================================
    // 2. 변수 및 상수 정의
    // ====================================================
    private LevelPlayRewardedAd rewardedAd;
    private Action<bool> currentAdCallback;
    
    private bool isShowingAd = false;   // 광고가 표시 중인지
    private bool isRewardGranted = false; // 보상을 받았는지

    // TODO: LevelPlay 대시보드에서 발급받은 실제 키값으로 교체하세요.
    private const string ANDROID_AD_UNIT_ID = "9ymbl3n8t2fxuban";
    private const string IOS_AD_UNIT_ID = "rkkq1nwzwcgt5ey0";

    private const string ANDROID_APP_KEY = "24a3feddd";
    private const string IOS_APP_KEY = "24a3fb455";

    // ====================================================
    // 3. 초기화 (Awake & Init)
    // ====================================================
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 초기화 시작
            InitializeLevelPlay();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void InitializeLevelPlay()
    {
        // [검증] User ID 설정 (트래킹 및 서버 검증 기초 단계)
        string userId = SystemInfo.deviceUniqueIdentifier;
 
        Debug.Log($"[AdManager] UserID 설정 완료: {userId}");

        // 이벤트 구독
        LevelPlay.OnInitSuccess += OnInitSuccess;
        LevelPlay.OnInitFailed += OnInitFailed;

        string appKey = GetAppKey();
        
        // LevelPlay SDK 초기화
        LevelPlay.Init(appKey, userId);
        
        Debug.Log("[AdManager] LevelPlay 초기화 시작");
    }

    private string GetAppKey()
    {
#if UNITY_ANDROID
        return ANDROID_APP_KEY;
#elif UNITY_IOS
        return IOS_APP_KEY;
#else
        return "UNSUPPORTED_PLATFORM";
#endif
    }

    private string GetAdUnitId()
    {
#if UNITY_ANDROID
        return ANDROID_AD_UNIT_ID;
#elif UNITY_IOS
        return IOS_AD_UNIT_ID;
#else
        return "UNSUPPORTED_PLATFORM";
#endif
    }

    // ====================================================
    // 4. 광고 생성 및 로드
    // ====================================================
    private void OnInitSuccess(LevelPlayConfiguration config)
    {
        Debug.Log("[AdManager] LevelPlay 초기화 성공");
        // UI나 로직 생성을 위해 메인 스레드에서 실행 보장
        ExecuteOnMainThread(CreateRewardedAd);
    }

    private void OnInitFailed(LevelPlayInitError error)
    {
        Debug.LogError($"[AdManager] LevelPlay 초기화 실패: {error.ErrorMessage}");
    }

    private void CreateRewardedAd()
    {
        string adUnitId = GetAdUnitId();
        rewardedAd = new LevelPlayRewardedAd(adUnitId);

        // 광고 관련 이벤트 구독
        rewardedAd.OnAdLoaded += OnAdLoaded;
        rewardedAd.OnAdLoadFailed += OnAdLoadFailed;
        rewardedAd.OnAdDisplayed += OnAdDisplayed;
        rewardedAd.OnAdDisplayFailed += OnAdDisplayFailed;
        rewardedAd.OnAdRewarded += OnAdRewarded;
        rewardedAd.OnAdClosed += OnAdClosed;

        // 첫 광고 로드
        LoadAd();
    }

    private void LoadAd()
    {
        if (rewardedAd != null)
        {
            rewardedAd.LoadAd();
            Debug.Log("[AdManager] 광고 로드 요청");
        }
    }

    // ====================================================
    // 5. 외부 호출 메서드 (광고 보여주기)
    // ====================================================
   // ====================================================
    // 5. 외부 호출 메서드 (수정됨: 에디터/PC 테스트 지원)
    // ====================================================
    public void ShowRewardedAd(Action<bool> onAdComplete)
    {
        if (isShowingAd)
        {
            Debug.LogWarning("[AdManager] 광고가 이미 재생 중입니다.");
            return;
        }

        currentAdCallback = onAdComplete;
        isRewardGranted = false;

        // 1. 네트워크 체크
        if (!NetworkManager.Instance.IsNetworkAvailable())
        {
            ShowNetworkWarningPopup();
            return;
        }

        // 2. [추가됨] 에디터이거나 PC 플랫폼인 경우 -> 가짜(Mock) 광고 재생
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL
        Debug.Log("[AdManager-Mock] 에디터/PC 환경 감지: 가짜 광고를 재생합니다.");
        StartCoroutine(MockAdProcess());
        return; 
#else  

        // 3. 실제 모바일 기기인 경우 -> 진짜 광고 재생
        if (rewardedAd != null && rewardedAd.IsAdReady())
        {
            isShowingAd = true;
            rewardedAd.ShowAd();
        }
        else
        {
            ShowAdNotReadyPopup();
            LoadAd();
        }
#endif
    }

    // [추가됨] 가짜 광고 처리 코루틴
    private IEnumerator MockAdProcess()
{
    isShowingAd = true;
    
    Debug.Log("[AdManager-Mock] Mock 광고 팝업 표시");
    
    // BaseWarningPopup으로 간단하게 표시
    if(PopupParentSetHelper.Instance != null)
    {
        GameObject popupObj = PopupParentSetHelper.Instance.CreatePopup("Prefabs/BaseWarningPopup");
        BaseWarningPopup popup = popupObj.GetComponent<BaseWarningPopup>();
        
        bool adCompleted = false;
        
        popup.Setup(
            "🎬 [테스트] 광고 시청 중...\n\n실제 빌드에서는 진짜 광고가 재생됩니다.\n\n(3초 후 자동 완료)", 
            () => {
                adCompleted = true;
                isShowingAd = false;
                currentAdCallback?.Invoke(true);
                currentAdCallback = null;
            }
        );
        
        // 3초 후 자동으로 버튼 클릭
        yield return new WaitForSeconds(3f);
        
        if(!adCompleted) // 사용자가 수동으로 안 닫았다면
        {
            Destroy(popupObj); // 팝업 강제 종료
            isShowingAd = false;
            currentAdCallback?.Invoke(true);
            currentAdCallback = null;
        }
        
        yield break;
    }
    
    // Fallback
    yield return new WaitForSeconds(1.0f); 
    isShowingAd = false;
    currentAdCallback?.Invoke(true);
    currentAdCallback = null;
}


    // ====================================================
    // 6. LevelPlay 이벤트 콜백 (스레드 안전 처리 적용)
    // ====================================================
    
    private void OnAdLoaded(LevelPlayAdInfo adInfo)
    {
        Debug.Log("[AdManager] 광고 로드 완료");
    }

    private void OnAdLoadFailed(LevelPlayAdError error)
    {
        Debug.LogError($"[AdManager] 광고 로드 실패: {error.ErrorMessage}");
        // 2초 뒤에 다시 로드 시도 (메인 스레드에서 실행)
        ExecuteOnMainThread(() => Invoke(nameof(LoadAd), 2f));
    }

    private void OnAdDisplayed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("[AdManager] 광고 표시 시작");
    }

    private void OnAdDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        Debug.LogError($"[AdManager] 광고 표시 실패: {error.ErrorMessage}");
        
        ExecuteOnMainThread(() => {
            isShowingAd = false;
            ShowAdFailedPopup();
            LoadAd(); // 실패했으니 다음 광고 미리 로드
        });
    }

    private void OnAdRewarded(LevelPlayAdInfo adInfo, LevelPlayReward reward)
    {
        Debug.Log($"[AdManager] 보상 지급 확인: {reward.Name} x {reward.Amount}");
        isRewardGranted = true;
    }

    private void OnAdClosed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("[AdManager] 광고 닫힘");
        
        ExecuteOnMainThread(() => {
            isShowingAd = false;

            if (isRewardGranted)
            {
                // 성공 콜백
                currentAdCallback?.Invoke(true);
            }
            else
            {
                // 중간에 닫음 -> 실패 팝업 처리
                ShowAdIncompletePopup();
            }

            currentAdCallback = null;
            LoadAd(); // 다음 광고 미리 로드
        });
    }

    // ====================================================
    // 7. 팝업 관리 (UI Logic)
    // ====================================================

    private void ShowNetworkWarningPopup()
    {
        // Resources/Prefabs/UI/BaseWarningPopup 프리팹이 존재해야 함
        GameObject popupObj = PopupParentSetHelper.Instance.CreatePopup("Prefabs/BaseWarningPopup");
      

        BaseWarningPopup popup = popupObj.GetComponent<BaseWarningPopup>();
        Transform loadingPanel = popupObj.transform.Find("LoadingPanel");
        
        if (loadingPanel != null)
        {
            loadingPanel.gameObject.SetActive(false);
        }

        popup.Setup("네트워크 연결을 한 뒤\n재시도 해주세요", () => {
            OnNetworkRetryClicked(popupObj, popup, loadingPanel);
        });
    }

    private void OnNetworkRetryClicked(GameObject popupObj, BaseWarningPopup popup, Transform loadingPanel)
    {
        if (loadingPanel != null)
        {
            loadingPanel.gameObject.SetActive(true);
        }

        StartCoroutine(CheckNetworkAndRetry(popupObj, popup, loadingPanel));
    }

    private IEnumerator CheckNetworkAndRetry(GameObject popupObj, BaseWarningPopup popup, Transform loadingPanel)
    {
        // 0.5초 정도 대기 (UX상 로딩 느낌)
        yield return new WaitForSeconds(0.5f);

        if (loadingPanel != null)
        {
            loadingPanel.gameObject.SetActive(false);
        }

        if (NetworkManager.Instance.IsNetworkAvailable())
        {
            Destroy(popupObj);
            // 네트워크 연결 확인 후 다시 광고 시청 시도
            ShowRewardedAd(currentAdCallback);
        }
        else
        {
            popup.MessageText.text = "다시 네트워크 연결 확인 후\n시도해주세요";
        }
    }

    private void ShowAdNotReadyPopup()
    {
        GameObject popupObj = PopupParentSetHelper.Instance.CreatePopup("Prefabs/BaseWarningPopup");
       

        BaseWarningPopup popup = popupObj.GetComponent<BaseWarningPopup>();
        popup.Setup("광고를 불러오는 중입니다.\n잠시 후 다시 시도해주세요.", () => {
            // 실패 처리
            currentAdCallback?.Invoke(false);
            currentAdCallback = null;
        });
    }

    private void ShowAdIncompletePopup()
    {
        GameObject popupObj = PopupParentSetHelper.Instance.CreatePopup("Prefabs/BaseWarningPopup");
    

        BaseWarningPopup popup = popupObj.GetComponent<BaseWarningPopup>();
        popup.Setup("광고 시청이 제대로 완료되지\n않았습니다. 다시 시도해주세요.", () => {
            if (currentAdCallback != null)
            {
                currentAdCallback.Invoke(false);
                currentAdCallback = null;
            }
        });
    }

    private void ShowAdFailedPopup()
    {
        GameObject popupObj = PopupParentSetHelper.Instance.CreatePopup("Prefabs/BaseWarningPopup");
        

        BaseWarningPopup popup = popupObj.GetComponent<BaseWarningPopup>();
        popup.Setup("광고를 불러오는 데 실패했습니다.\n다시 시도해주세요.", () => {
            if (currentAdCallback != null)
            {
                currentAdCallback.Invoke(false);
                currentAdCallback = null;
            }
        });
    }

    // ====================================================
    // 8. 유틸리티 (메인 스레드 실행 보장)
    // ====================================================
    private void ExecuteOnMainThread(Action action)
    {
        if (action == null) return;
        StartCoroutine(MainThreadRunner(action));
    }

    private IEnumerator MainThreadRunner(Action action)
    {
        yield return null; // 한 프레임 대기 (자동으로 메인 스레드 스케줄링)
        action.Invoke();
    }

    // ====================================================
    // 9. 종료 처리 (메모리 정리)
    // ====================================================
    private void OnDestroy()
    {
        if (rewardedAd != null)
        {
            rewardedAd.OnAdLoaded -= OnAdLoaded;
            rewardedAd.OnAdLoadFailed -= OnAdLoadFailed;
            rewardedAd.OnAdDisplayed -= OnAdDisplayed;
            rewardedAd.OnAdDisplayFailed -= OnAdDisplayFailed;
            rewardedAd.OnAdRewarded -= OnAdRewarded;
            rewardedAd.OnAdClosed -= OnAdClosed;
            
            // 리소스 해제
            rewardedAd.Dispose();
        }

        LevelPlay.OnInitSuccess -= OnInitSuccess;
        LevelPlay.OnInitFailed -= OnInitFailed;
    }
}