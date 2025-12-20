using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainHomeManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject howToPlayPanel;
    [SerializeField] private GameObject hallOfFamePanel;
    [SerializeField] private GameObject levelSelectPanel;

    [Header("Main Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button howToPlayButton;
    [SerializeField] private Button hallOfFameButton;

    [Header("Level Select UI")]
    [SerializeField] private Button[] levelButtons;         // 레벨 버튼 1~4번
    [SerializeField] private GameObject[] lockVisuals;      // 잠금 아이콘/이미지
    [SerializeField] private Button closeLevelSelectButton; // 왼쪽 상단 X 버튼

    [Header("HowToPlay Slide UI")]                          // 게임방법 창
    [SerializeField] private Image tutorialDisplayImage; 
    [SerializeField] private Sprite[] tutorialSprites;   
    [SerializeField] private Button nextButton;          
    [SerializeField] private Button prevButton;          
    [SerializeField] private Button closeHowToPlayButton; 
    private int currentTutorialIndex = 0;

    [Header("Hall Of Fame UI")]
    [SerializeField] private Button closeHallOfFameButton; 

    private ProgressData currentProgress;
    private const string SaveKey = "ProgressData";

    void Start()
    {
        // 1. 데이터 로드
        currentProgress = SaveManager.LoadData<ProgressData>(SaveKey);
        
        // 2. 버튼 리스너 연결 
        SetupButtonListeners();
        
        // 3. 초기 화면 설정
        ShowMainPanel();
    }

    private void SetupButtonListeners()
    {
        // 메인 메뉴
        startButton.onClick.AddListener(OnClickGameStart);
        howToPlayButton.onClick.AddListener(OpenHowToPlay);
        hallOfFameButton.onClick.AddListener(OpenHallOfFame);

        // 레벨 선택 창
        closeLevelSelectButton.onClick.AddListener(ShowMainPanel);
        for (int i = 0; i < levelButtons.Length; i++)
        {
            int levelIndex = i + 1;
            levelButtons[i].onClick.AddListener(() => OnLevelButtonClick(levelIndex));
            UpdateLevelUnlockState(i);
        }

        // 게임 방법 창
        nextButton.onClick.AddListener(NextTutorial);
        prevButton.onClick.AddListener(PrevTutorial);
        closeHowToPlayButton.onClick.AddListener(ShowMainPanel);

        // 명예의 전당 창
        if (closeHallOfFameButton != null)
            closeHallOfFameButton.onClick.AddListener(ShowMainPanel);
    }

    // 레벨 해금/잠금 비주얼 처리
    private void UpdateLevelUnlockState(int index)
    {
        // 레벨 1은 항상 열림, 그 외는 LastClearedLevel 확인
        bool isUnlocked = (index == 0) || (currentProgress.LastClearedLevel >= index);
        levelButtons[index].interactable = isUnlocked;

        if (lockVisuals.Length > index && lockVisuals[index] != null)
        {
            lockVisuals[index].SetActive(!isUnlocked);
        }
    }

    private void OnLevelButtonClick(int levelIndex)
    {
        // 선택한 레벨 저장 후 씬 이동
        PlayerPrefs.SetInt("SelectedLevel", levelIndex);
        PlayerPrefs.Save();
        SceneManager.LoadScene("GameScene");
    }

    #region 게임 방법(슬라이드) 로직
    public void OpenHowToPlay()
    {
        CloseAllPanels();
        howToPlayPanel.SetActive(true);
        currentTutorialIndex = 0;
        UpdateTutorialUI();
    }

    private void NextTutorial()
    {
        if (currentTutorialIndex < tutorialSprites.Length - 1)
        {
            currentTutorialIndex++;
            UpdateTutorialUI();
        }
    }

    private void PrevTutorial()
    {
        if (currentTutorialIndex > 0)
        {
            currentTutorialIndex--;
            UpdateTutorialUI();
        }
    }

    private void UpdateTutorialUI()
    {
        if (tutorialSprites.Length > 0)
            tutorialDisplayImage.sprite = tutorialSprites[currentTutorialIndex];

        prevButton.interactable = (currentTutorialIndex > 0);
        nextButton.interactable = (currentTutorialIndex < tutorialSprites.Length - 1);
    }
    #endregion

    #region 패널 전환 공통
    public void OnClickGameStart() { CloseAllPanels(); levelSelectPanel.SetActive(true); }
    public void OpenHallOfFame() { CloseAllPanels(); hallOfFamePanel.SetActive(true); }
    public void ShowMainPanel() { CloseAllPanels(); mainPanel.SetActive(true); }

    private void CloseAllPanels()
    {
        mainPanel.SetActive(false);
        levelSelectPanel.SetActive(false);
        howToPlayPanel.SetActive(false);
        hallOfFamePanel.SetActive(false);
    }
    #endregion
}