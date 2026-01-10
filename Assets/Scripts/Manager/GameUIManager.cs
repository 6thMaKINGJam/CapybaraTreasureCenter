using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance { get; private set; }


    [Header("세부 매니저 연결")]
    public BundleGridManager GridManager; // 하단 12개 그리드 관리
    public SelectedBundlesUIPanel SelectionPanel; // 선택된 묶음 패널 관리
    public GemCountPanelManager GemCountStatusPanel;

    [Header("상자 정보")]
    public TMP_Text BoxIndexText; // 상자 #번호
    public TMP_Text BoxProgressText; // 담은 개수 / 총개수

    [Header("상단/하단 버튼들")]
    public Button PauseButton;
    public Button ResumeButton;
    public Button HintButton;
    public Button CancelSelectButton;
    public Button UndoButton;
    public Button CompleteButton;
    public Button Undo1Button;
    public Button ReselectReqButton;

    [Header("팝업 패널")]
    public GameObject PausePopupPanel;

    [Header("타이머 바")]
    public Slider TimerSlider;

    [Header("아이템 남은 횟수 텍스트")]
    public TextMeshProUGUI HintCountText;
    public TextMeshProUGUI Undo1CountText; // 이미지의 RetryButton이 새로고침 역할이라면
    public TextMeshProUGUI UndoCountText;
    

    [Header("카운트다운 연출")]
    public TextMeshProUGUI CountdownText;
    private CanvasGroup countdownCanvasGroup;

    [Header("연출 컨트롤러")]
    public BoxVisualController BoxVisual; // ★ 추가! 인스펙터에서 연결



    void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        if (CountdownText != null)
            countdownCanvasGroup = CountdownText.GetComponent<CanvasGroup>();
    }

    void Start()
    {
        SetupButtons();
    }

// 팝업 생성 헬퍼 함수
// ★ 연출 실행용 래퍼 함수 추가
    public void AnimateBoxChange(System.Action onFinished)
    {
        if(BoxVisual != null)
        {
            BoxVisual.PlayCompleteAnimation(onFinished);
        }
        else
        {
            // 연출기가 없으면 그냥 즉시 완료 처리
            onFinished?.Invoke();
        }
    }
// ✅ 수정: 최대 횟수 파라미터 추가
public void UpdateHintAndItemUI(
    int hintLeft, int maxHint,
    int refreshLeft, int maxRefresh,
    int undoLeft, int maxUndo)
{
    if (HintCountText != null) 
        HintCountText.text = $"{hintLeft}";
    
    if (Undo1CountText != null) 
        Undo1CountText.text = $"{refreshLeft}";
    
    if (UndoCountText != null) 
        UndoCountText.text = $"{undoLeft}";
}
    

    // ========== 버튼 연결 ==========
    private void SetupButtons(){
            // 1. 일시정지 및 재개 (Core.SceneLoader 활용)
        PauseButton.onClick.RemoveAllListeners();
        PauseButton.onClick.AddListener(() => {
            Core.SceneLoader.Instance.TogglePause(true);
            OpenPausePopup(); // 기존 팝업 열기 함수 호출
        });

        if (ResumeButton != null)
        {
            ResumeButton.onClick.RemoveAllListeners();
            ResumeButton.onClick.AddListener(() => {
                Core.SceneLoader.Instance.TogglePause(false);
                ClosePausePopup(); // 기존 팝업 닫기 함수 호출
            });
        }

        // 2. 게임 플레이 관련 버튼 (Manager 존재 여부에 따라 분기)
        CompleteButton.onClick.RemoveAllListeners();
        Undo1Button.onClick.RemoveAllListeners();
        Undo1Button.onClick.AddListener(() => Core.SceneLoader.Instance.ExecuteUndo1());
        // 선택 취소 버튼 리스너 초기화
        if (CancelSelectButton != null) {
            CancelSelectButton.onClick.RemoveAllListeners();
        }

        if (GameManager.Instance != null)
        {
            // 레벨 모드일 때
            CompleteButton.onClick.AddListener(() => GameManager.Instance.OnClickComplete());
            // 상자 전체 되돌리기 버튼 연결
            if (UndoButton != null) {
                UndoButton.onClick.RemoveAllListeners();
                UndoButton.onClick.AddListener(() => GameManager.Instance.ProcessUndo());
            }

            // 힌트 버튼 연결
            if (HintButton != null) {
                HintButton.onClick.RemoveAllListeners();
                HintButton.onClick.AddListener(() => GameManager.Instance.ProcessHint());
            }
            
            // 레벨 모드 선택 취소 연결
            if (CancelSelectButton != null) {
                CancelSelectButton.onClick.AddListener(() => GameManager.Instance.CancelSelection());
            }
        }
        else if (ChallengeManager.Instance != null)
        {
            // 챌린지 모드일 때
            CompleteButton.onClick.AddListener(() => ChallengeManager.Instance.OnClickComplete());

            if (CancelSelectButton != null) {
                CancelSelectButton.onClick.AddListener(() => ChallengeManager.Instance.CancelSelection());
            }
            if (GridManager != null)
            {
                // ChallengeManager가 보관 중인 현재 디스플레이 데이터를 사용하여 클릭 이벤트를 연결합니다.
                // (ChallengeManager.InitChallengeMode에서 수행하지만, UI 설정 시 확답을 위해 추가)
            }
            if (GridManager != null) {
                // ChallengeManager가 이미 생성한 데이터를 사용하여 그리드를 다시 그려주며 콜백을 확실히 연결합니다.
                ChallengeManager.Instance.Invoke("ExtractDisplayBundles", 0.05f); 
            }
            // [추가] 조건 재선택 버튼 연결
            if (ReselectReqButton != null)
            {
                ReselectReqButton.onClick.RemoveAllListeners();
                ReselectReqButton.onClick.AddListener(() => ChallengeManager.Instance.OnClickReselectRequirement());
            }
        }
        
        
    }

// ✅ 기존 UpdateItemUI() 대신 사용
public void UpdateHintAndItemUI(int hintLeft, int refreshLeft, int undoLeft)
{
    if (HintCountText != null) 
        HintCountText.text = hintLeft.ToString();
    
    if (Undo1CountText != null) 
        Undo1CountText.text = refreshLeft.ToString();
    
    if (UndoCountText != null) 
        UndoCountText.text = undoLeft.ToString();
}

  

    // ========== 상자 진행도 표시 업데이트 ==========
    // ========== 상자 진행도 표시 업데이트 ==========
public void UpdateBoxUI(int boxIndex, int currentAmount, int requiredAmount, int totalBoxCount)
{
    BoxIndexText.text = $"상자 #{boxIndex + 1} / {totalBoxCount}";
    BoxProgressText.text = $"{currentAmount} / {requiredAmount}";
}

    // ========== 일시정지 팝업 ==========
    public void OpenPausePopup()
    {
        if(PausePopupPanel != null)
        {
            PausePopupPanel.SetActive(true);
            Time.timeScale = 0f;
        }
    }

    public void ClosePausePopup()
    {
        if(PausePopupPanel != null)
        {
            PausePopupPanel.SetActive(false);
            Time.timeScale = 1f;
        }
    }
    public void StartCountdownEffect(int number)
    {
        StopAllCoroutines(); // 겹침 방지
        StartCoroutine(CountdownRoutine(number));
    }

    private System.Collections.IEnumerator CountdownRoutine(int number)
    {
        CountdownText.gameObject.SetActive(true);
        CountdownText.text = number.ToString();

        // 연출 시작: 투명도 0에서 1로, 크기 작게에서 크게
        float duration = 0.8f; // 1초보다 약간 짧게 (다음 숫자와의 간격)
        float elapsed = 0f;

        Vector3 startScale = Vector3.one * 0.5f;
        Vector3 endScale = Vector3.one * 1.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            // 투명도 조절 (나타났다가 서서히 사라짐)
            if (progress < 0.2f) // 처음 20% 동안 나타남
                countdownCanvasGroup.alpha = progress / 0.2f;
            else // 나머지 동안 서서히 사라짐
                countdownCanvasGroup.alpha = 1f - (progress - 0.2f) / 0.8f;

            // 크기 조절
            CountdownText.transform.localScale = Vector3.Lerp(startScale, endScale, progress);

            yield return null;
        }

        CountdownText.gameObject.SetActive(false);
    }
}