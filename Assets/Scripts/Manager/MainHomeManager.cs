using UnityEngine;
using UnityEngine.SceneManagement;
using Scripts.UI;
using TMPro;

public class MainHomeManager : MonoBehaviour
{
    [Header("UI Controllers")]
    [SerializeField] private MainHomePanel mainHomeUI;
    [SerializeField] private LevelSelectPanel levelSelectUI;
    [SerializeField] private HowToPlayPanel howToPlayUI;
    [SerializeField] private HallOfFamePanel hallOfFameUI;

    [SerializeField] private CanvasGroup mainCanvasGroup;

    // ✅ 추가: 사과 UI
    [Header("사과 UI")]
    [SerializeField] private TextMeshProUGUI AppleCountText;
    [SerializeField] private TextMeshProUGUI AppleRegenTimerText;

    [Header("Ending Settings")]
    [SerializeField] private GameObject levelEndingPrefab;      // 레벨 모드 엔딩 프리팹
    [SerializeField] private GameObject challengeEndingPrefab;  // 챌린지 모드 엔딩 프리팹
    [SerializeField] private Transform endingParent;



    private ProgressData currentProgress;
    private const string SaveKey = "ProgressData";
    // 외부에서 접근 가능한 정적 변수
    public static bool ShowHallOfFameOnStart = false;

    void Start()
    {
        // 1. 데이터 로드
        currentProgress = SaveManager.LoadData<ProgressData>(SaveKey);
        
        // 2. UI 버튼들에 기능 연결
        SetupButtons();

 // ✅ 사과 UI 초기화
        UpdateAppleUI();
        CheckAndRunEndings();
        Debug.Log("checkandrunendings 완료");
        
        // ✅ 사과 변경 이벤트 구독
        if(AppleManager.Instance != null)
        {
            AppleManager.Instance.OnAppleCountChanged += OnAppleCountChanged;
        }

        // 3. 초기 화면 설정
        mainHomeUI.ShowMain();

        if (ShowHallOfFameOnStart)
        {
            OnClickHallOfFame(); // 명예의 전당 자동 열기
            ShowHallOfFameOnStart = false; // 플래그 초기화
        }
        
    }

    void Update()
    {
        UpdateRegenTimerUI();
    }

    private void UpdateRegenTimerUI()
    {
        if (AppleRegenTimerText == null || AppleManager.Instance == null) return;

        int currentApples = AppleManager.Instance.GetAppleCount();
        
        // 5개 미만이고 레벨 100 클리어 시에만 타이머 표시
        if (currentApples < 5) 
        {
            float remainingSeconds = AppleManager.Instance.GetRemainingRegenSeconds();
            if (remainingSeconds > 0)
            {
                int minutes = (int)(remainingSeconds / 60);
                int seconds = (int)(remainingSeconds % 60);
                AppleRegenTimerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
                AppleRegenTimerText.gameObject.SetActive(true);
            }
            else
            {
                AppleRegenTimerText.gameObject.SetActive(false);
            }
        }
        else
        {
            AppleRegenTimerText.gameObject.SetActive(false); // 5개 이상이면 타이머 숨김
        }
    }
     
    void OnDestroy()
    {
        // ✅ 이벤트 구독 해제
        if(AppleManager.Instance != null)
        {
            AppleManager.Instance.OnAppleCountChanged -= OnAppleCountChanged;
        }
    }

    private void CheckAndRunEndings()
    {
        // 1. 레벨 모드: 100레벨 클리어 & 엔딩 미시청
        if (currentProgress.isLevel100Completed && !currentProgress.isLevelEndingViewed)
        {
            if (levelEndingPrefab != null)
            {
                Debug.Log("미시청한 레벨 엔딩 프리팹을 생성합니다.");
                
                // ✅ 배경음악 정지
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.StopAllBGM();
                }

                // ✅ 프리팹 생성
                Instantiate(levelEndingPrefab, endingParent != null ? endingParent : transform);

                // ✅ 시청 완료 상태로 변경 및 저장
                currentProgress.isLevelEndingViewed = true;
                SaveManager.Save(currentProgress, SaveKey);
            }
            return;
        }

        // 2. 챌린지 모드: 100상자 클리어 & 엔딩 미시청 (가정된 조건)
        if (currentProgress.isChallengeCompleted && !currentProgress.isChallengeEndingViewed)
        {
            if (challengeEndingPrefab != null)
            {
                Debug.Log("미시청한 챌린지 엔딩 프리팹을 생성합니다.");
                
                // ✅ 배경음악 정지
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.StopAllBGM();
                }

                // ✅ 프리팹 생성
                Instantiate(challengeEndingPrefab, endingParent != null ? endingParent : transform);

                // ✅ 시청 완료 상태로 변경 및 저장
                currentProgress.isChallengeEndingViewed = true;
                SaveManager.Save(currentProgress, SaveKey);
            }
            return;
        }
    }

 // ✅ 사과 개수 변경 시 UI 업데이트
    private void OnAppleCountChanged(int newCount)
    {
        UpdateAppleUI();
    }
    
    // ✅ 사과 UI 갱신
    private void UpdateAppleUI()
    {
        if(AppleCountText != null && AppleManager.Instance != null)
        {
            AppleCountText.text = AppleManager.Instance.GetAppleCount().ToString();
        }
    }


    private void SetupButtons()
    {
        mainHomeUI.levelSelectButton.onClick.RemoveAllListeners();
        mainHomeUI.ChallengeButton.onClick.RemoveAllListeners();
        mainHomeUI.hallOfFameButton.onClick.RemoveAllListeners();
        mainHomeUI.howToPlayButton.onClick.RemoveAllListeners();

        mainHomeUI.levelSelectButton.onClick.AddListener(OnClickLevelSelect);
        mainHomeUI.howToPlayButton.onClick.AddListener(OnClickHowToPlay);
        mainHomeUI.hallOfFameButton.onClick.AddListener(OnClickHallOfFame);
        mainHomeUI.ChallengeButton.onClick.AddListener(OnClickChallengeMode);

        levelSelectUI.closeButton.onClick.AddListener(mainHomeUI.ShowMain);
        howToPlayUI.closeButton.onClick.AddListener(mainHomeUI.ShowMain);

        hallOfFameUI.closeButton.onClick.RemoveAllListeners();
        
        hallOfFameUI.closeButton.onClick.AddListener(() =>
        {
            Debug.Log("명예의 전당 닫기 버튼 클릭됨!");
            hallOfFameUI.gameObject.SetActive(false);
            mainHomeUI.ShowMain();
        });
    }

    private void CheckAndRunEndingSequence()
    {//수정필요 좌연주
        // 레벨 100 클리어했지만 엔딩 미시청
        if(currentProgress.LastClearedLevel >= 100 && !currentProgress.EndingCompleted)
        {
            if(NetworkManager.Instance != null && NetworkManager.Instance.IsNetworkAvailable())
            {
                if(mainCanvasGroup != null) mainCanvasGroup.interactable = false;
                
                Debug.Log("엔딩 시퀀스로 진입합니다.");
                //d=엔딩 시퀸스 실행
            }
            else
            {
                Debug.LogWarning("네트워크 연결 후 재접속 시 엔딩을 감상할 수 있습니다.");
            }
        }
    }

    public void OnClickLevelSelect()
    {
        mainHomeUI.OpenPanel(mainHomeUI.LevelSelectPanel);
        // ✅ 제거: levelSelectUI.RefreshLevelNodes() 호출 삭제
        // LevelSelectPanel이 OnEnable에서 자동 갱신
    }

    public void OnClickHowToPlay()
{
    // 기존 패널 오픈 로직을 주석 처리하거나 삭제합니다.
    // mainHomeUI.OpenPanel(mainHomeUI.HowToPlayPanel);
    // howToPlayUI.Init();

    // "Tutorial"이라는 이름의 씬으로 전환합니다. (씬 이름이 다를 경우 수정 필요)
    UnityEngine.SceneManagement.SceneManager.LoadScene("Tutorial"); 
}

    public void OnClickHallOfFame()
    {
        if(NetworkManager.Instance != null && NetworkManager.Instance.IsNetworkAvailable())
        {
            mainHomeUI.OpenPanel(mainHomeUI.HallOfFamePanel);
            hallOfFameUI.Open(); // Open() 메서드 호출
        }
        else
        {
            Debug.LogWarning("명예의 전당은 온라인 상태에서만 확인 가능합니다.");
            
            GameObject popupObj = PopupParentSetHelper.Instance.CreatePopup("Prefabs/BaseWarningPopup");
            BaseWarningPopup popup = popupObj.GetComponent<BaseWarningPopup>();
            popup.Setup("네트워크 연결이 필요합니다카피!", null);
        }
    }

    public void OnClickChallengeMode()
    {
        if (AppleManager.Instance != null)
        {
            // 1. 사과 1개 소모 시도
            if (currentProgress.SpendApples(1))
            {
                // 성공 시 바로 챌린지 씬으로 이동
                EnterChallengeScene();
            }
            else
            {
                // 2. 사과 부족 시 광고 제안 팝업 표시
                ShowChallengeAdOffer();
            }
        }
    }
    private void ShowChallengeAdOffer()
    {
        // BaseConfirmationPopup을 사용하여 유저에게 의사를 묻습니다.
        if (PopupParentSetHelper.Instance != null)
        {
            GameObject popupObj = PopupParentSetHelper.Instance.CreatePopup("Prefabs/BaseConfirmationPopup");
            BaseConfirmationPopup popup = popupObj.GetComponent<BaseConfirmationPopup>();

            if (popup != null)
            {
                popup.Setup(
                    "사과가 부족합니다카피!\n광고를 보고 챌린지에 도전하시겠습니까?",
                    () => {
                        // 확인 클릭 시 광고 실행
                        StartChallengeAd();
                    },
                    null // 취소 시에는 그냥 팝업이 닫힙니다.
                );
            }
        }
    }

    private void StartChallengeAd()
    {
        if (AdManager.Instance != null)
        {
            // 보상형 광고 시청 완료 시 보상으로 챌린지 씬 입장
            AdManager.Instance.ShowRewardedAd((success) => {
                if (success)
                {
                    Debug.Log("[MainHome] 광고 시청 완료! 챌린지 모드로 진입합니다.");
                    EnterChallengeScene();
                }
            });
        }
    }

    private void EnterChallengeScene()
    {
        // 씬 전환 로직
        UnityEngine.SceneManagement.SceneManager.LoadScene("ChallengeMode");
    }
}