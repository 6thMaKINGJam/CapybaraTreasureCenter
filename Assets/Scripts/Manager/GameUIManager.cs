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
}