using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public enum TutorialItemType { Undo1, Undo, Refresh }

[System.Serializable]
public class TutorialItemConfig
{
    [Header("경고 팝업")]
    public string warningMessage; // "잠깐!!! 지금은 ~~~"
    
    [Header("메인 패널")]
    public GameObject tutorialPanelPrefab; // 아이템별 프리팹
    
    [Header("버튼 안내")]
    public string buttonPromptText; // "[버튼]을 눌러봐!"
}

public class ItemTutorialManager : MonoBehaviour
{
    public static ItemTutorialManager Instance { get; private set; }
    
    [Header("Warning Popup Prefab")]
    [SerializeField] private GameObject warningPopupPrefab;
    
    [Header("Tutorial Configs")]
    [SerializeField] private TutorialItemConfig undo1Config;
    [SerializeField] private TutorialItemConfig undoConfig;
    [SerializeField] private TutorialItemConfig refreshConfig;
    
    [Header("Spawn Parent")]
    [SerializeField] private Transform tutorialParent;
    
    [Header("References")]
    [SerializeField] private GameUIManager uiManager;
    [SerializeField] private CapyDialogue capyDialogue;
    [SerializeField] private TextMeshProUGUI capyText;
    
    private GameObject currentWarningPopup;
    private ItemTutorialPanel currentPanel;
    private TutorialItemType currentTutorial;
    private bool isTutorialActive = false;
    private bool isTriggeredByCondition = false;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
    }
    
    
    // ========== 트리거 플래그 설정 ==========
    
    public void SetTriggeredByCondition(bool value)
    {
        isTriggeredByCondition = value;
    }
    
    // ========== 첫 클릭 핸들러 ==========
    
    private void OnUndo1FirstClick()
    {
        uiManager.Undo1Button.onClick.RemoveListener(OnUndo1FirstClick);
        
        if (!isTutorialActive)
        {
            isTriggeredByCondition = false;
            ShowUndo1Tutorial();
        }
    }
    
    private void OnUndoFirstClick()
    {
        uiManager.UndoButton.onClick.RemoveListener(OnUndoFirstClick);
        
        if (!isTutorialActive)
        {
            isTriggeredByCondition = false;
            ShowUndoTutorial();
        }
    }
    
    private void OnRefreshFirstClick()
    {
        uiManager.RefreshButton.onClick.RemoveListener(OnRefreshFirstClick);
        
        if (!isTutorialActive)
        {
            isTriggeredByCondition = false;
            ShowRefreshTutorial();
        }
    }
    
    // ========== Undo1 ==========
    
    public void ShowUndo1Tutorial()
    {
        var progress = SaveManager.LoadData<TutorialProgress>("TutorialProgress");
        if (progress.undo1TutorialShown) return;
        
        isTutorialActive = true;
        currentTutorial = TutorialItemType.Undo1;
        
        if (isTriggeredByCondition)
        {
            ShowWarningPopup(undo1Config.warningMessage, () => ShowUndo1MainPanel());
        }
        else
        {
            ShowUndo1MainPanel();
        }
    }
    
    private void ShowUndo1MainPanel()
    {
        GameObject panelObj = Instantiate(undo1Config.tutorialPanelPrefab, tutorialParent);
        currentPanel = panelObj.GetComponent<ItemTutorialPanel>();
        
        if (currentPanel != null)
        {
            currentPanel.OnClosed += OnUndo1PanelClosed;
            currentPanel.Show();
        }
    }
    
    private void OnUndo1PanelClosed()
    {
        if (currentPanel != null)
        {
            currentPanel.OnClosed -= OnUndo1PanelClosed;
            currentPanel.Hide();
            currentPanel = null;
        }
        
        Time.timeScale = 1f;
        
        DisableAllButtonsExcept(uiManager.Undo1Button);
        uiManager.StartWiggle(uiManager.Undo1Button.gameObject);
        
        if (capyDialogue != null && capyText != null)
        {
            capyDialogue.ShowDialogue(
                capyText, 
                undo1Config.buttonPromptText, 
                false, 
                0f, 
                true
            );
        }
        
        uiManager.Undo1Button.onClick.AddListener(OnUndo1ButtonClicked);
    }
    
    private void OnUndo1ButtonClicked()
    {
        uiManager.Undo1Button.onClick.RemoveListener(OnUndo1ButtonClicked);
        uiManager.StopWiggleInTutorial(uiManager.Undo1Button.gameObject);
        EnableAllButtons();
        
        if (capyDialogue != null && capyText != null)
        {
            capyDialogue.ShowDialogue(capyText, DialogueType.Default);
        }
        
        var progress = SaveManager.LoadData<TutorialProgress>("TutorialProgress");
        progress.undo1TutorialShown = true;
        SaveManager.Save(progress, "TutorialProgress");
        
        isTutorialActive = false;
        isTriggeredByCondition = false;
        
        LevelModeManager.Instance.UpdateTutorialFlags();
        
        Debug.Log("[ItemTutorial] Undo1 튜토리얼 완료");
    }
    
    // ========== Undo ==========
    
    public void ShowUndoTutorial()
    {
        var progress = SaveManager.LoadData<TutorialProgress>("TutorialProgress");
        if (progress.undoTutorialShown) return;
        
        isTutorialActive = true;
        currentTutorial = TutorialItemType.Undo;
        
        if (isTriggeredByCondition)
        {
            ShowWarningPopup(undoConfig.warningMessage, () => ShowUndoMainPanel());
        }
        else
        {
            ShowUndoMainPanel();
        }
    }
    
    private void ShowUndoMainPanel()
    {
        GameObject panelObj = Instantiate(undoConfig.tutorialPanelPrefab, tutorialParent);
        currentPanel = panelObj.GetComponent<ItemTutorialPanel>();
        
        if (currentPanel != null)
        {
            currentPanel.OnClosed += OnUndoPanelClosed;
            currentPanel.Show();
        }
    }
    
    private void OnUndoPanelClosed()
    {
        if (currentPanel != null)
        {
            currentPanel.OnClosed -= OnUndoPanelClosed;
            currentPanel.Hide();
            currentPanel = null;
        }
        
        Time.timeScale = 1f;
        
        DisableAllButtonsExcept(uiManager.UndoButton);
        uiManager.StartWiggle(uiManager.UndoButton.gameObject);
        
        if (capyDialogue != null && capyText != null)
        {
            capyDialogue.ShowDialogue(
                capyText, 
                undoConfig.buttonPromptText, 
                false, 
                0f, 
                true
            );
        }
        
        uiManager.UndoButton.onClick.AddListener(OnUndoButtonClicked);
    }
    
    private void OnUndoButtonClicked()
    {
        uiManager.UndoButton.onClick.RemoveListener(OnUndoButtonClicked);
        uiManager.StopWiggleInTutorial(uiManager.UndoButton.gameObject);
        EnableAllButtons();
        
        if (capyDialogue != null && capyText != null)
        {
            capyDialogue.ShowDialogue(capyText, DialogueType.Default);
        }
        
        var progress = SaveManager.LoadData<TutorialProgress>("TutorialProgress");
        progress.undoTutorialShown = true;
        SaveManager.Save(progress, "TutorialProgress");
        
        isTutorialActive = false;
        isTriggeredByCondition = false;
        
        LevelModeManager.Instance.UpdateTutorialFlags();
        
        Debug.Log("[ItemTutorial] Undo 튜토리얼 완료");
    }
    
    // ========== Refresh ==========
    
    public void ShowRefreshTutorial()
    {
        var progress = SaveManager.LoadData<TutorialProgress>("TutorialProgress");
        if (progress.refreshTutorialShown) return;
        
        isTutorialActive = true;
        currentTutorial = TutorialItemType.Refresh;
        
        if (isTriggeredByCondition)
        {
            ShowWarningPopup(refreshConfig.warningMessage, () => ShowRefreshMainPanel());
        }
        else
        {
            ShowRefreshMainPanel();
        }
    }
    
    private void ShowRefreshMainPanel()
    {
        GameObject panelObj = Instantiate(refreshConfig.tutorialPanelPrefab, tutorialParent);
        currentPanel = panelObj.GetComponent<ItemTutorialPanel>();
        
        if (currentPanel != null)
        {
            currentPanel.OnClosed += OnRefreshPanelClosed;
            currentPanel.Show();
        }
    }
    
    private void OnRefreshPanelClosed()
    {
        if (currentPanel != null)
        {
            currentPanel.OnClosed -= OnRefreshPanelClosed;
            currentPanel.Hide();
            currentPanel = null;
        }
        
        Time.timeScale = 1f;
        
        DisableAllButtonsExcept(uiManager.RefreshButton);
        uiManager.StartWiggle(uiManager.RefreshButton.gameObject);
        
        if (capyDialogue != null && capyText != null)
        {
            capyDialogue.ShowDialogue(
                capyText, 
                refreshConfig.buttonPromptText, 
                false, 
                0f, 
                true
            );
        }
        
        uiManager.RefreshButton.onClick.AddListener(OnRefreshButtonClicked);
    }
    
    private void OnRefreshButtonClicked()
    {
        uiManager.RefreshButton.onClick.RemoveListener(OnRefreshButtonClicked);
        uiManager.StopWiggleInTutorial(uiManager.RefreshButton.gameObject);
        EnableAllButtons();
        
        if (capyDialogue != null && capyText != null)
        {
            capyDialogue.ShowDialogue(capyText, DialogueType.Default);
        }
        
        var progress = SaveManager.LoadData<TutorialProgress>("TutorialProgress");
        progress.refreshTutorialShown = true;
        SaveManager.Save(progress, "TutorialProgress");
        
        isTutorialActive = false;
        isTriggeredByCondition = false;
        
        LevelModeManager.Instance.UpdateTutorialFlags();
        
        Debug.Log("[ItemTutorial] Refresh 튜토리얼 완료");
    }
    
    // ========== 경고 팝업 (프리팹 직접 조작) ==========
    
    private void ShowWarningPopup(string message, System.Action onNextClicked)
{
    Time.timeScale = 0f;
    
    // 프리팹 생성
    currentWarningPopup = Instantiate(warningPopupPrefab, tutorialParent);
    
    // 화면 흔들림
    Camera.main.DOShakePosition(0.5f, strength: 0.3f, vibrato: 10).SetUpdate(true);
    
    // 컴포넌트 찾기 (간단하게)
    CanvasGroup canvasGroup = currentWarningPopup.GetComponent<CanvasGroup>();
    TextMeshProUGUI warningText = currentWarningPopup.transform.Find("warningMesage").GetComponent<TextMeshProUGUI>();
    Button nextButton = currentWarningPopup.GetComponentInChildren<Button>();
    RectTransform contentPanel = currentWarningPopup.GetComponent<RectTransform>();
    
    // 텍스트 설정
    if (warningText != null)
        warningText.text = message;
    
    // 다음 버튼 이벤트
    if (nextButton != null)
    {
        nextButton.onClick.AddListener(() =>
        {
            HideWarningPopup(onNextClicked);
        });
    }
    
    // 페이드인 애니메이션
    if (canvasGroup != null)
    {
        canvasGroup.alpha = 0;
        canvasGroup.DOFade(1, 0.3f).SetUpdate(true);
    }
    
    // 콘텐츠 팝업 애니메이션
    if (contentPanel != null)
    {
        contentPanel.localScale = Vector3.zero;
        contentPanel.DOScale(1f, 0.4f).SetEase(Ease.OutBack).SetUpdate(true);
    }
}

    
    private void HideWarningPopup(System.Action onComplete)
    {
        if (currentWarningPopup == null) return;
        
        CanvasGroup canvasGroup = currentWarningPopup.GetComponent<CanvasGroup>();
        
        if (canvasGroup != null)
        {
            canvasGroup.DOFade(0, 0.2f).SetUpdate(true).OnComplete(() =>
            {
                if (currentWarningPopup != null)
                {
                    Destroy(currentWarningPopup);
                    currentWarningPopup = null;
                }
                onComplete?.Invoke();
            });
        }
        else
        {
            Destroy(currentWarningPopup);
            currentWarningPopup = null;
            onComplete?.Invoke();
        }
    }
    public bool IsTutorialActive()
{
    return isTutorialActive;
}

    // ========== 공통 메서드 ==========
    
    private void DisableAllButtonsExcept(Button exception)
    {
        if (uiManager.RefreshButton != null)
            uiManager.RefreshButton.interactable = (exception == uiManager.RefreshButton);
        
        if (uiManager.Undo1Button != null)
            uiManager.Undo1Button.interactable = (exception == uiManager.Undo1Button);
        
        if (uiManager.UndoButton != null)
            uiManager.UndoButton.interactable = (exception == uiManager.UndoButton);
        
        if (uiManager.CompleteButton != null)
            uiManager.CompleteButton.interactable = false;
        
        if (uiManager.CancelSelectButton != null)
            uiManager.CancelSelectButton.interactable = false;

            // ✅ 그리드 보석 버튼들 비활성화
    if (uiManager.GridManager != null)
        uiManager.GridManager.SetAllBundlesInteractable(false);


    }
    
    private void EnableAllButtons()
    {
        if (uiManager.RefreshButton != null)
            uiManager.RefreshButton.interactable = true;
        
        if (uiManager.Undo1Button != null)
            uiManager.Undo1Button.interactable = true;
        
        if (uiManager.UndoButton != null)
            uiManager.UndoButton.interactable = true;
        
        if (uiManager.CompleteButton != null)
            uiManager.CompleteButton.interactable = true;
        
        if (uiManager.CancelSelectButton != null)
            uiManager.CancelSelectButton.interactable = true;

            // ✅ 그리드 보석 버튼들 비활성화
    if (uiManager.GridManager != null)
        uiManager.GridManager.SetAllBundlesInteractable(true);

    }
}