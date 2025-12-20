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

    [Header("보석별 총량 표시")]
    public List<TMP_Text> GemCountTexts; // 각 보석 종류별 UI Text (5개)

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
    public Button RefreshButton;

    [Header("팝업 패널")]
    public GameObject PausePopupPanel;

    [Header("타이머 바")]
    public Slider TimerSlider;

    [Header("아이템 남은 횟수 텍스트")]
    public TextMeshProUGUI HintCountText;
    public TextMeshProUGUI RefreshCountText; // 이미지의 RetryButton이 새로고침 역할이라면
    public TextMeshProUGUI UndoCountText;

    [Header("카운트다운 연출")]
    public TextMeshProUGUI CountdownText;
    private CanvasGroup countdownCanvasGroup;



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
    

    // ========== 버튼 연결 ==========
    private void SetupButtons()
    {
        PauseButton.onClick.AddListener(() => GameManager.Instance.TogglePause());
        if(ResumeButton != null)
        {
            ResumeButton.onClick.AddListener(() => GameManager.Instance.Resume());
        }else{
            Debug.LogError("<color=red>[UI]</color> ResumeButton이 인스펙터에 할당되지 않았습니다카피!");
        }
        HintButton.onClick.AddListener(() => GameManager.Instance.ProcessHint());
        CancelSelectButton.onClick.AddListener(() => GameManager.Instance.CancelSelection());
        UndoButton.onClick.AddListener(() => GameManager.Instance.ProcessUndo());
        CompleteButton.onClick.AddListener(() => GameManager.Instance.OnClickComplete());
        RefreshButton.onClick.AddListener(() => GameManager.Instance.ProcessRefresh());
    }

    // 횟수 UI를 한 번에 업데이트하는 함수
    public void UpdateItemCounts(int hintUsed, int refreshUsed, int undoUsed)
    {   
        if (HintCountText != null)
            HintCountText.text = Mathf.Max(0, 1 - hintUsed).ToString();

        if (RefreshCountText != null)
            RefreshCountText.text = Mathf.Max(0, 3 - refreshUsed).ToString();

        if (UndoCountText != null)
            UndoCountText.text = Mathf.Max(0, 3 - undoUsed).ToString();
    }

    public void UpdateItemUI(int hintUsed, int refreshUsed, int undoUsed, int maxCount = 3)
    {
        // 1. 남은 횟수 계산
        int hintLeft = Mathf.Max(0, maxCount - hintUsed);
        int refreshLeft = Mathf.Max(0, maxCount - refreshUsed);
        int undoLeft = Mathf.Max(0, maxCount - undoUsed);

        // 2. 텍스트 업데이트
        if (HintCountText != null) HintCountText.text = hintLeft.ToString();
        if (RefreshCountText != null) RefreshCountText.text = refreshLeft.ToString();
        if (UndoCountText != null) UndoCountText.text = undoLeft.ToString();

        // 3. 버튼 활성화/비활성화 제어 (0이면 클릭 불가)
        // 만약 광고를 보고 계속 쓸 수 있게 하려면 이 부분을 수정해야 하지만,
        // 지금은 "누르지 못하게" 하는 것이 목적이므로 false로 설정합니다.
        if (HintButton != null) HintButton.interactable = (hintLeft > 0);
        if (RefreshButton != null) RefreshButton.interactable = (refreshLeft > 0);
        if (UndoButton != null) UndoButton.interactable = (undoLeft > 0);
        
        // (팁) 비활성화된 버튼의 색상을 어둡게 하고 싶다면 
        // Button 컴포넌트의 Transition -> Disabled Color를 조절하면 됩니다.
    }

    // ========== 보석별 총량 표시 업데이트 ==========
    public void UpdateTotalGemUI(Dictionary<GemType, int> gemCounts)
    {
        foreach(var kvp in gemCounts)
        {
            int index = (int)kvp.Key;
            if(index < GemCountTexts.Count && GemCountTexts[index] != null)
            {
                GemCountTexts[index].text = kvp.Value.ToString();
                
                // 0개면 빨간색 강조
                GemCountTexts[index].color = (kvp.Value <= 0) ? Color.red : Color.white;
            }
        }
    }

    // ========== 상자 진행도 표시 업데이트 ==========
    public void UpdateBoxUI(int boxIndex, int currentAmount, int requiredAmount)
    {
        BoxIndexText.text = $"상자 #{boxIndex + 1}";
        BoxProgressText.text = $"{currentAmount} / {requiredAmount}";

        // 정확히 맞으면 완료 버튼 활성화
        CompleteButton.interactable = (currentAmount == requiredAmount);
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