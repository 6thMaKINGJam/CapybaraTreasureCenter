using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.UI;
using Scripts.UI;
using DG.Tweening;
using System;
using UnityEngine.Video;
using System.IO;

public class LevelModeManager : MonoBehaviour
{
    public static LevelModeManager Instance;
    public bool IsTutorialScene = false; // 인스펙터에서 체크
    
    [Header("매니저 참조")]
    public ChunkGenerator ChunkGenerator;
    public GameUIManager UIManager;
    public BundleGridManager GridManager;
    
    [Header("레벨 설정")]
    public LevelConfig CurrentLevelConfig;
    
    [Header("엔딩 프리팹")]
    public GameObject EndingPrefab;
    public Transform EndingPopupTransfrom;

    
    [Header("카피바라 대사 시스템")]
    public CapyDialogue CapyDialogue;
    public TextMeshProUGUI CapyDialogueText; // 대사 표시할 UI Text
    public GameObject CapyDialogueBUbble;
    
    [Header("효과")]
    public Image FlashOverlay; // 빨간 화면 깜박임용 Image (전체 화면 크기)

    [Header("UI 매니저 참조")]
    public GemCountPanelManager GemCountStatusPanel;
    [Header("배경 Video Player")]
    public VideoPlayer backgroundVideoPlayer; // Inspector에서 할당
    // 게임 데이터
    private GameData gameData;
    private int RemainingBoxes => gameData.Boxes.Count - gameData.CurrentBoxIndex;
    private ChunkData chunkData;
      [Header("힌트 로딩 UI")]
    public GameObject HintLoadingUI;  // ✅ 추가
    // 시간 관련

    [Header("알림")]
    public GameObject NotificationPanel; // Inspector 할당
    public TextMeshProUGUI NotificationText; // Panel 내부 텍스트
    public float NotificationDuration = 2f; // 표시 시간
    private float levelStartTime;
    private Coroutine timeCheckCoroutine;
    private int tutorialStep = 0;
    



[Header("Visual Management")]
[SerializeField] private ChapterVisualController chapterVisualController;

    // 연속 성공 카운트
    private int consecutiveSuccessCount = 0;
    private int lastCountedSecond = -1; // 중복 호출 방지용
  
private Dictionary<GemBundle, GemBundlePrefab> selectedBundleOriginalPrefabs 
    = new Dictionary<GemBundle, GemBundlePrefab>();
private Dictionary<GemBundle, int> selectedBundleOriginalIndices 
    = new Dictionary<GemBundle, int>(); // Bundle → 원래 Grid 인덱스

 private HintManager hintManager;
    
 // ✅ 시간추가 관련
    private bool isWaitingForTimeAdd = false;
    

    
    void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            // ✅ HintManager 초기화
            hintManager = new HintManager();
        }
        else
        {
            return;
        }
    }
    
    void Start()
    {
        InitGame();
    
        // 튜토리얼 실습 모드일 때 추가 설정
        Debug.Log($"[levelmodeManager] TutorialPracticeMode: {PlayerPrefs.GetInt("TutorialPracticeMode", 0)}");
        if (PlayerPrefs.GetInt("TutorialPracticeMode", 0) == 1)
        {
            ApplyTutorialPracticeRules();
        }
    }

    private void ApplyTutorialPracticeRules()
    {
        IsTutorialScene = true;
        if (UIManager != null)
        {
            UIManager.LevelText.gameObject.SetActive(false);
            // 초기 상태: 모든 버튼 비활성화 (보석 클릭은 허용)
            SetAllButtonsInteractable(false);
            // 2. 완료 버튼만 개별적으로 활성화 (추가 코드)
            if (UIManager.CompleteButton != null)
            {
                UIManager.CompleteButton.interactable = true;
            }
        }
        StartTutorialPopups();
    }

    private void StartTutorialPopups()
    {
        ShowTutorialPopup("기본 규칙: 모든 종류의 보석을 개수에 맞게 넣으세요!\n[빨->노->초->파->보] 순서로 보충됩니다.", () => {
            ShowTutorialPopup("별을 많이 얻을수록 더 많은 사과를 얻을 수 있어요!\n사과는 아이템 구매에 사용됩니다. 이제 보석을 담아보세요.", null);
        });
    }

    private IEnumerator ShowTutorialNotificationsSequentially()
    {
        // 첫 번째 알림
        ShowTopNotification("기본 규칙: 모든 종류의 보석을 개수에 맞게 넣으세요! [빨->노->초->파->보] 순서로 보충됩니다.");
        
        // 알림이 떠 있는 시간(NotificationDuration) + 페이드 인/아웃 시간만큼 대기
        // 현재 코드상 NotificationDuration은 2f이며, 페이드는 왕복 0.6f입니다.
        yield return new WaitForSeconds(3.0f); 

        // 두 번째 알림
        ShowTopNotification("별을 많이 얻을수록 더 많은 사과를 얻을 수 있어요! 사과는 아이템 구매에 사용됩니다. 이제 보석을 담아보세요.");
        yield return new WaitForSeconds(3.0f); 
    }

    // 버튼 상호작용 일괄 제어 헬퍼
    private void SetAllButtonsInteractable(bool interactable)
    {
        UIManager.UndoButton.interactable = interactable;
        UIManager.Undo1Button.interactable = interactable;
        UIManager.RefreshButton.interactable = interactable;
        UIManager.CancelSelectButton.interactable = interactable;
        UIManager.CompleteButton.interactable = interactable;
    }

    void Update()
    {
        // gameData가 생성된 상태이고 게임 상태가 Playing일 때만 작동
        if (gameData != null && gameData.GameState == GameState.Playing)
        {
            float remainingTime = GetDynamicTimeLimit() - gameData.ElapsedTime;

            // 5.5초 이하일 때 카운트다운 시작
            if (remainingTime <= 5.5f && remainingTime > 0)
            {
                int currentSecond = Mathf.CeilToInt(remainingTime);

                if (currentSecond != lastCountedSecond)
                {
                    lastCountedSecond = currentSecond;
                    GameUIManager.Instance.StartCountdownEffect(currentSecond);
                }
            }
        }
    }

// ✅ 동적 제한시간 반환 (ChunkGenerator가 사용한 tempConfig 정보)
    private float GetDynamicTimeLimit()
    {
        // tempConfig 대신 실제 계산된 값을 저장해야 함
        // 간단하게 Boxes 개수로 역산 (레벨1~11은 상자 개수로 판단 가능)
        int selectedLevel = PlayerPrefs.GetInt("SelectedLevel", 1);
        int chapterStartLevel = GetChapterStartLevel(CurrentLevelConfig.ChapterNumber);
        int levelInChapter = selectedLevel - chapterStartLevel + 1;
        
        var (_, timeLimit) = CurrentLevelConfig.CalculateDifficulty(levelInChapter);
        return timeLimit;
    }

    // ========== 초기화 ==========
    public void InitGame()
    {
        Time.timeScale = 1f;
        
        // ✅ 기존 음악 즉시 정리
        if(SoundManager.Instance != null)
        {
            SoundManager.Instance.StopAllBGM();
        }
        
        
        // 레벨 선택 정보 로드
        int selectedLevel = PlayerPrefs.GetInt("SelectedLevel", 1);
        if(PlayerPrefs.GetInt("TutorialPracticeMode", 0) != 0)
        {
            selectedLevel = 0; // 튜토리얼 전용
        }
        LoadLevelConfig(selectedLevel);
        
        // ✅ 난이도 계산 (챕터 내 레벨 번호)
        int chapterStartLevel = GetChapterStartLevel(CurrentLevelConfig.ChapterNumber);
        int levelInChapter = selectedLevel - chapterStartLevel + 1;
        
        var (boxCount, timeLimit) = CurrentLevelConfig.CalculateDifficulty(levelInChapter);
        if (UIManager != null)
        {
            UIManager.SetupStarMarkers(timeLimit);
        }
        
        Debug.Log($"[levelmodeManager] 레벨 {selectedLevel} (챕터 {CurrentLevelConfig.ChapterNumber}, 챕터 내 {levelInChapter}) - 상자 {boxCount}개, 시간 {timeLimit}초");
        
        // ✅ BGM + 레이어 재생
        if(SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayBGMWithLayer(
                "Game", 
                CurrentLevelConfig.AdditionalBGMLayer
            );
        }
        
        // ✅ Video 재생
        if(backgroundVideoPlayer != null)
        {
            string videoPath = Path.Combine(
                Application.streamingAssetsPath, 
                CurrentLevelConfig.BackgroundVideoFileName
            );
            backgroundVideoPlayer.url = videoPath;
            backgroundVideoPlayer.Prepare();
            backgroundVideoPlayer.Play();
            
            Debug.Log($"[LevelModeManager] Video 재생: {videoPath}");
        }
        
        // ✅ 카피바라 의상 전환 
        // 게임 설정 (계산된 난이도 적용)
        SetupNewGameWithDifficulty(boxCount, timeLimit);
        
        levelStartTime = Time.time;
        timeCheckCoroutine = StartCoroutine(CheckTimeOver());
        
        // UI 초기화
        RefreshUI();
        
        // ===== CapyDialogue 연결: 게임 시작 =====
        if(CapyDialogue != null && CapyDialogueText != null)
        {
            CapyDialogue.ShowDialogue(CapyDialogueText, DialogueType.Default);
        }
        // 튜토리얼 학습 모드일 경우 타이머를 시작하지 않음*(수정필요)

        UIManager.TimerSlider.gameObject.SetActive(true);
        
    }
    
// ✅ 난이도 적용된 게임 설정
   // SetupNewGameWithDifficulty() 메서드 내부
private void SetupNewGameWithDifficulty(int boxCount, float timeLimit)
{
    gameData = new GameData();
    gameData.CurrentLevelIndex = PlayerPrefs.GetInt("SelectedLevel", 1);
    gameData.CurrentBoxIndex = 0;
    gameData.GameState = GameState.Playing;
    gameData.StartTime = Time.time;
    gameData.ElapsedTime = 0f;
    gameData.UndoCount = 0;
    gameData.Undo1Count = 0;
    gameData.RefreshCount = 0;
    
    // ✅ 임시 Config 생성 제거! 직접 파라미터 전달
    chunkData = ChunkGenerator.GenerateAllChunks(
        boxCount,                                    // 계산된 상자 개수
        CurrentLevelConfig.GemTypeCount,             // Config에서
        CurrentLevelConfig.MaxRequiredPerBox         // Config에서
    );

    if(GemCountStatusPanel != null)
    {
        GemCountStatusPanel.InitLevelGemStatus(
            chunkData.TotalRemainingGems, 
            CurrentLevelConfig.GemTypeCount
        );

        // 2. 현재 남은 상자 수를 전달하여 슬라이더/상세 표시 여부를 결정하게 함
        int remainingBoxes = gameData.Boxes.Count - gameData.CurrentBoxIndex;
        GemCountStatusPanel.UpdateGemCount(
            (GemType)0, 
            chunkData.TotalRemainingGems[(GemType)0], 
            remainingBoxes
        );
    }
    
    gameData.Boxes = new List<Box>(chunkData.AllBoxes);
    gameData.BundlePool = new List<GemBundle>(chunkData.MergedBundlePool);
    gameData.RemainingGems = new Dictionary<GemType, int>(chunkData.TotalRemainingGems);
    
    ExtractDisplayBundles();
    
    Debug.Log($"[LevelModeManager] 게임 설정 완료: 상자 {boxCount}개, 제한시간 {timeLimit}초");
}

    

     // ✅ 챕터별 시작 레벨 반환
    private int GetChapterStartLevel(int chapterNumber)
    {
        switch(chapterNumber)
        {
            case 0: return 0;
            case 1: return 1;
            case 2: return 34;
            case 3: return 67;
            case 100: return 100;
            default: return 1;
        }
    }


    private void LoadLevelConfig(int levelIndex)
    {
        // 1. 튜토리얼 실습 모드인지 확인
        bool isPracticeMode = PlayerPrefs.GetInt("TutorialPracticeMode", 0) != 0;
        if (isPracticeMode)
        {
            // 튜토리얼 전용 컨피그 로드
            CurrentLevelConfig = Resources.Load<LevelConfig>("LevelData/LevelConfig_Tutorial");
            Debug.Log($"[LevelModeManager] {CurrentLevelConfig.name} 로드 시도");
            
            if (CurrentLevelConfig == null)
            {
                Debug.LogError("[LevelModeManager] LevelConfig_Tutorial을 찾을 수 없습니다! Resources 폴더를 확인하세요.");
                // 실패 시 기본 1챕터 컨피그로 대체
                CurrentLevelConfig = Resources.Load<LevelConfig>("LevelData/LevelConfig_1");
            }
            else
            {
                Debug.Log("[LevelModeManager] 튜토리얼 실습용 컨피그 로드 완료");
            }
            return; // 튜토리얼 로직 종료
        }

        // ✅ 챕터별 LevelConfig 로드
        int chapterNumber;
        if(levelIndex == 100)
        {
            chapterNumber = 100;
        }
        else if(levelIndex >= 67)
        {
            chapterNumber = 3;
        }
        else if(levelIndex >= 34)
        {
            chapterNumber = 2;
        }
        else
        {
            chapterNumber = 1;
        }
        
        CurrentLevelConfig = Resources.Load<LevelConfig>($"LevelData/LevelConfig_{chapterNumber}");
        
        if(CurrentLevelConfig == null)
        {
            Debug.LogError($"[LevelModeManager] LevelConfig_{chapterNumber}를 찾을 수 없습니다!");
        }
        else
        {
             // ✅ Visual 적용
        if(chapterVisualController != null)
        {
            chapterVisualController.ApplyVisuals(CurrentLevelConfig);
        }
            Debug.Log($"[LevelModeManager] LevelConfig_{chapterNumber} 로드 완료");
        }
    }
    
    
    private void ExtractDisplayBundles()
    {
        gameData.CurrentDisplayBundles.Clear();
        
        int count = Mathf.Min(12, gameData.BundlePool.Count);
        for(int i = 0; i < count; i++)
        {
            gameData.CurrentDisplayBundles.Add(gameData.BundlePool[i]);
        }
        
        GridManager.RefreshGrid(gameData.CurrentDisplayBundles, OnBundleClicked);
    }
    

   // ===== OnBundleClicked() - 완전 재작성 =====
private void OnBundleClicked(GemBundlePrefab clickedPrefab)
{
    GemBundle bundle = clickedPrefab.GetData();
    
    // Placeholder 클릭 방지
    if(bundle == null) return;
    
    // 힌트 흔들림 중지
    GridManager.StopShakingBundle(bundle);
    
    // 현재 Grid 인덱스 찾기
    int gridIndex = clickedPrefab.transform.GetSiblingIndex();
    
    // ===== 선택 취소 =====
    if(gameData.SelectedBundles.Contains(bundle))
    {
        gameData.SelectedBundles.Remove(bundle);
        gameData.RemainingGems[bundle.GemType] += bundle.GemCount;
        
        if(!selectedBundleOriginalIndices.ContainsKey(bundle))
        {
            Debug.LogError($"[OnBundleClicked] {bundle.BundleID}의 원래 인덱스를 찾을 수 없습니다!");
            return;
        }
        
        int originalIndex = selectedBundleOriginalIndices[bundle];
        selectedBundleOriginalIndices.Remove(bundle);
        
        if(!gameData.BundlePool.Contains(bundle))
        {
            gameData.BundlePool.Add(bundle);
        }
        
        GemBundle currentBundle = gameData.CurrentDisplayBundles[originalIndex];
        
        if(currentBundle != null && currentBundle != bundle)
        {
            if(!gameData.BundlePool.Contains(currentBundle))
            {
                gameData.BundlePool.Add(currentBundle);
            }
        }
        
        gameData.CurrentDisplayBundles[originalIndex] = bundle;
        
        GridManager.ReplaceBundleAtIndex(
            originalIndex,
            bundle,
            OnBundleClicked,
            isRestoring: true
        );
        
        // 취소는 즉시 UI 업데이트
        UIManager.SelectionPanel.UpdateUI(gameData.SelectedBundles);
        UIManager.UpdateBoxUI(
    gameData.CurrentBoxIndex,
    CalculateSelectedTotal(),
    GetCurrentBox().RequiredAmount,
    gameData.Boxes.Count // ← 추가
);

        
        if (GemCountStatusPanel != null)
        {
            GemCountStatusPanel.UpdateGemCount(bundle.GemType, gameData.RemainingGems[bundle.GemType], RemainingBoxes);
        }
    }
    // ===== 선택 =====
    else
    {
       // ✅ 디버그 로그만 추가 (에러는 발생시키지 않음)
    if(gameData.RemainingGems[bundle.GemType] < bundle.GemCount)
    {
        Debug.LogWarning($"[OnBundleClicked] ⚠️ 동기화 문제 감지!");
        Debug.LogWarning($"  - {bundle.GemType} 남은 개수: {gameData.RemainingGems[bundle.GemType]}");
        Debug.LogWarning($"  - {bundle.GemType} 필요 개수: {bundle.GemCount}");
        Debug.LogWarning($"  - BundlePool의 {bundle.GemType} 총합: {gameData.BundlePool.Where(b => b.GemType == bundle.GemType).Sum(b => b.GemCount)}");
        Debug.LogWarning($"  - 선택 계속 진행...");
    }
    
        gameData.SelectedBundles.Add(bundle);
        selectedBundleOriginalIndices[bundle] = gridIndex;
        gameData.BundlePool.Remove(bundle);
        
       // GemBundle newBundle = GetRandomFromRemainingPool();
 // ★ 수정: 색상 순환 로직 적용
    GemType nextColor = GetNextColorInCycle(bundle.GemType);
    GemBundle newBundle = GetNextBundleByColor(nextColor);

        gameData.CurrentDisplayBundles[gridIndex] = newBundle;
        
        gameData.RemainingGems[bundle.GemType] -= bundle.GemCount;
        
        if (GemCountStatusPanel != null)
        {
            GemCountStatusPanel.UpdateGemCount(bundle.GemType, gameData.RemainingGems[bundle.GemType], RemainingBoxes);
        }
        
        // Grid 교체 시작 (애니메이션 포함)
        GridManager.ReplaceBundleAtIndex(
            gridIndex,
            newBundle,
            OnBundleClicked,
            isRestoring: false
        );
        
        // 애니메이션 완료 후 UI 업데이트 (0.5초 딜레이)
        StartCoroutine(UpdateSelectionUIAfterAnimation());
    }
}
// ✅ 새 메서드: 애니메이션 완료 후 UI 업데이트
private IEnumerator UpdateSelectionUIAfterAnimation()
{
    // BundleGridManager의 애니메이션 시간과 동기화
    // - 축소: 0.3초
    // - 팝업: 0.2초
    // 총 0.5초 대기
    yield return new WaitForSeconds(0.25f);
    
    // 선택 패널 업데이트
    UIManager.SelectionPanel.UpdateUI(gameData.SelectedBundles);
    
    // 상자 진행도 업데이트
    UIManager.UpdateBoxUI(
        gameData.CurrentBoxIndex,
        CalculateSelectedTotal(),
        GetCurrentBox().RequiredAmount,
        gameData.Boxes.Count
    );
}

// ========== 색상 순환 시스템 ==========

/// <summary>
/// 현재 색의 다음 색상을 순환 순서대로 반환 (사용 가능한 색만)
/// </summary>
private GemType GetNextColorInCycle(GemType currentColor)
{
    // 전체 색상 순서: Red → Yellow → Green → Blue → Purple
    GemType[] fullCycle = new GemType[] 
    { 
        GemType.Red, 
        GemType.Yellow, 
        GemType.Green, 
        GemType.Blue, 
        GemType.Purple 
    };
    
    // 현재 레벨에서 사용 중인 색상만 필터링
    List<GemType> availableColors = new List<GemType>();
    for(int i = 0; i < CurrentLevelConfig.GemTypeCount; i++)
    {
        availableColors.Add((GemType)i);
    }
    
    // 현재 색 다음부터 순환하며 사용 가능한 색 찾기
    int startIdx = Array.IndexOf(fullCycle, currentColor);
    
    for(int i = 1; i < fullCycle.Length; i++)
    {
        int checkIdx = (startIdx + i) % fullCycle.Length;
        GemType candidateColor = fullCycle[checkIdx];
        
        if(availableColors.Contains(candidateColor))
        {
            return candidateColor;
        }
    }
    
    // 모든 색을 순회했는데도 없으면 현재 색 반환 (fallback)
    return currentColor;
}

/// <summary>
/// 다음 색상에 해당하는 번들을 BundlePool에서 찾아 반환
/// </summary>
private GemBundle GetNextBundleByColor(GemType targetColor)
{
    // 1. 해당 색상의 번들 필터링
    List<GemBundle> colorBundles = gameData.BundlePool
        .Where(b => b != null && b.GemType == targetColor)
        .ToList();
    
    // 2. 이미 화면에 표시된 번들 제외
    foreach(var displayedBundle in gameData.CurrentDisplayBundles)
    {
        if(displayedBundle != null)
        {
            colorBundles.Remove(displayedBundle);
        }
    }
    
    // 3. 남은 번들이 없으면 null
    if(colorBundles.Count == 0)
    {
        return null;
    }
    
    // 4. 랜덤 선택 (같은 색 중에서는 랜덤)
    int randomIdx = UnityEngine.Random.Range(0, colorBundles.Count);
    return colorBundles[randomIdx];
}


// ===== 남은 Pool에서 랜덤 1개 선택 =====
// ===== 남은 Pool에서 랜덤 선택 =====
private GemBundle GetRandomFromRemainingPool()
{
    // ✅ 수정: null 제외하고 사용 가능한 번들만 필터링
    List<GemBundle> availableBundles = gameData.BundlePool
        .Where(b => b != null) // ← null 제외
        .ToList();
    foreach(var displayedBundle in gameData.CurrentDisplayBundles)
    {
        if(displayedBundle != null)
        {
            availableBundles.Remove(displayedBundle);
        }
    }
    
    if(availableBundles.Count == 0)
    {
        return null;
    }
    
    int randomIndex = UnityEngine.Random.Range(0, availableBundles.Count);
    return availableBundles[randomIndex];
}
// ===== CancelSelection() - 간단 버전 =====
public void CancelSelection()
{
    if(gameData.SelectedBundles.Count == 0)
    {
        UIManager.SelectionPanel.UpdateUI(gameData.SelectedBundles);
        UIManager.UpdateBoxUI(gameData.CurrentBoxIndex, 0, GetCurrentBox().RequiredAmount,  gameData.Boxes.Count);
        return;
    }
    
    // 복원 정보 수집 (인덱스 순서대로 정렬)
    List<BundleRestoreInfo> restoreInfos = new List<BundleRestoreInfo>();
    
    foreach(var bundle in gameData.SelectedBundles)
    {
        if(!selectedBundleOriginalIndices.ContainsKey(bundle))
        {
            Debug.LogWarning($"[CancelSelection] {bundle.BundleID}의 인덱스를 찾을 수 없습니다!");
            continue;
        }
        
        gameData.RemainingGems[bundle.GemType] += bundle.GemCount;
        
        if (GemCountStatusPanel != null)
        {
            GemCountStatusPanel.UpdateGemCount(bundle.GemType, gameData.RemainingGems[bundle.GemType], RemainingBoxes);
        }

        int originalIndex = selectedBundleOriginalIndices[bundle];
        GemBundle currentBundle = gameData.CurrentDisplayBundles[originalIndex];
        
        restoreInfos.Add(new BundleRestoreInfo
        {
            OriginalBundle = bundle,
            OriginalIndex = originalIndex,
            CurrentBundle = currentBundle
        });
    }
    
    // 인덱스 순서대로 정렬
    restoreInfos.Sort((a, b) => a.OriginalIndex.CompareTo(b.OriginalIndex));
    
    // 복원 실행
    foreach(var info in restoreInfos)
    {
        // BundlePool에 원래 번들 추가
        if(!gameData.BundlePool.Contains(info.OriginalBundle))
        {
            gameData.BundlePool.Add(info.OriginalBundle);
        }
        
        // 현재 번들 반환
        if(info.CurrentBundle != null && info.CurrentBundle != info.OriginalBundle)
        {
            if(!gameData.BundlePool.Contains(info.CurrentBundle))
            {
                gameData.BundlePool.Add(info.CurrentBundle);
            }
        }
        
        // CurrentDisplayBundles 복원
        gameData.CurrentDisplayBundles[info.OriginalIndex] = info.OriginalBundle;
        
        // Grid 복원
        GridManager.ReplaceBundleAtIndex(
            info.OriginalIndex,
            info.OriginalBundle,
            OnBundleClicked,
            isRestoring: true
        );
    }

    
    
    // 전체 초기화
    gameData.SelectedBundles.Clear();
    selectedBundleOriginalIndices.Clear();
    
    // UI 업데이트
    UIManager.SelectionPanel.UpdateUI(gameData.SelectedBundles);
    UIManager.UpdateBoxUI(gameData.CurrentBoxIndex, 0, GetCurrentBox().RequiredAmount,gameData.Boxes.Count);
    GridManager.ClearAllSelections();
    if (IsTutorialScene && tutorialStep == 5) 
    {
        OnCancelSelectionSuccess(); // 튜토리얼 콜백 추가
    }
}

public void OnCancelSelectionSuccess()
{
    if (IsTutorialScene && tutorialStep == 5)
    {
        UIManager.StopWigggleInTutorial(UIManager.CancelSelectButton.gameObject);
        
        // 모든 기능을 써봤으므로 모든 버튼 활성화
        CompleteTutorialPractice();
    }
}
private void CompleteTutorialPractice()
{
    ShowTutorialPopup("모든 기능을 익히셨습니다!\n이제 자유롭게 보석을 담아 레벨을 클리어하세요.", null);
    SetAllButtonsInteractable(true);
    IsTutorialScene = false;
    tutorialStep = 10;
}

// ===== 복원 정보 클래스 =====
private class BundleRestoreInfo
{
    public GemBundle OriginalBundle;
    public int OriginalIndex;
    public GemBundle CurrentBundle;
}
   
    // ========== 완료 버튼 ==========
   public void OnClickComplete()
    {    // ✅ 모든 흔들림 중지
        GridManager.StopAllShaking();
    

        Box currentBox = GetCurrentBox();
        int selectedTotal = CalculateSelectedTotal();
        
        // 1. 개수 검증 실패
        if(selectedTotal != currentBox.RequiredAmount)
        {
            HandleUnmatchGemFailure(); // 실패 처리 함수 호출
            return;
        }
        
        // 2. 종류 검증 실패 (모든 종류 1개 이상)
        if(!ValidateGemTypes())
        {
            HandleDifficientFailure(); // 실패 처리 함수 호출
            return;
        }
        
        // 3. 처리 (성공)
        ProcessBoxCompletion();
    }

    // [추가됨] 실패 시 공통 처리 로직
    private void HandleDifficientFailure()
    {
        // 연속 성공 카운트 초기화
        consecutiveSuccessCount = 0; 

        // ===== CapyDialogue 연결: 검증 실패 =====
        // 경고 메시지를 띄우지만, 내부적으로 연속 성공은 깨짐
        ShowDifficientWarning(null); 
        FlashRedScreen();
        VibrationManager.Instance.Vibrate(VibrationPattern.Warning);
      
    if(gameData.SelectedBundles.Count > 0)
    {
        CancelSelection();
    }
    }
    private void HandleUnmatchGemFailure()
    {
        // 연속 성공 카운트 초기화
        consecutiveSuccessCount = 0; 

        // ===== CapyDialogue 연결: 검증 실패 =====
        // 경고 메시지를 띄우지만, 내부적으로 연속 성공은 깨짐
        ShowUnmatchGemWarning(null); 
        FlashRedScreen();
        VibrationManager.Instance.Vibrate(VibrationPattern.Warning);
      
    if(gameData.SelectedBundles.Count > 0)
    {
        CancelSelection();
    }
    }

    private bool ValidateGemTypes()
    {
        Dictionary<GemType, int> typeCount = new Dictionary<GemType, int>();
        for(int i = 0; i < CurrentLevelConfig.GemTypeCount; i++)
        {
            typeCount[(GemType)i] = 0;
        }
        
        foreach(var bundle in gameData.SelectedBundles)
        {
            typeCount[bundle.GemType] += bundle.GemCount;
        }
        
        foreach(var kvp in typeCount)
        {
            if(kvp.Value < 1) return false;
        }
        
        return true;
    }
    
 // LevelModeManager.cs 내부

    public void ProcessBoxCompletion()
    {
        // 1. 흔들림 등 효과 중지
        GridManager.StopAllShaking();

        // 2. [데이터 처리] 사용한 번들 제거 및 정리 (즉시 실행)
        foreach (var bundle in gameData.SelectedBundles)
        {
            // Pool과 Display 목록에서 제거
            if (gameData.BundlePool.Contains(bundle))
            {
                gameData.BundlePool.Remove(bundle);
            }

            if (gameData.CurrentDisplayBundles.Contains(bundle))
            {
                gameData.CurrentDisplayBundles.Remove(bundle);
            }
        }

        // 3. 완료된 상자 기록 저장
        CompletedBox completedBox = new CompletedBox();
        completedBox.BoxIndex = gameData.CurrentBoxIndex;
        completedBox.UsedBundles = new List<GemBundle>(gameData.SelectedBundles);
        gameData.CompletedBoxes.Add(completedBox);

        if (PlayerPrefs.GetInt("TutorialPracticeMode", 0) == 1) 
        {
            int nextBoxIdx = gameData.CurrentBoxIndex + 1;

            if (nextBoxIdx == 1) {                
                HandleTutorialStep();
            }
            else if (nextBoxIdx == 3) { 
                StartCoroutine(ReturnToTutorialAfterDelay());
                return; 
            }
        }

        // 4. 상자 인덱스 증가
        gameData.CurrentBoxIndex++;

        // 5. 선택 관련 데이터 초기화
        foreach (var bundle in gameData.SelectedBundles)
        {
            if (selectedBundleOriginalPrefabs.ContainsKey(bundle))
            {
                selectedBundleOriginalPrefabs.Remove(bundle);
            }
        }
        gameData.SelectedBundles.Clear();

        // 6. 연속 성공 카운트 및 대사 처리
        consecutiveSuccessCount++;
        if (CapyDialogue != null && CapyDialogueText != null)
        {
            if (consecutiveSuccessCount >= 1)
            {
                CapyDialogue.ShowDialogue(CapyDialogueText, DialogueType.BoxCompleted);
                CapyDialogue.RestartDefault(CapyDialogueText, 3.5f);
            }
            else
            {
                CapyDialogue.ShowDialogue(CapyDialogueText, DialogueType.Default);
                CapyDialogue.RestartDefault(CapyDialogueText, 3.5f);
            }
        }

        // 7. 게임오버 체크 (보석이 말랐는지 확인)
        if (CheckGameOver())
        {
            HandleGameOver("특정 보석이 0개가 되어 더 이상 진행할 수 없습니다카피!");
            return;
        }

        // 8. [시각적 연출] 상자 교체 애니메이션 실행
        // 데이터는 이미 위에서 변했으므로, 애니메이션이 끝난 후 UI를 갱신합니다.
        if (UIManager != null)
        {
            UIManager.AnimateBoxChange(() =>
            {
                // === 이 안의 코드는 애니메이션(0.3~0.5초)이 끝난 후 실행됩니다 ===

                // A. 레벨 클리어 체크
                // (인덱스가 증가했으므로 전체 개수와 비교)
                if (gameData.CurrentBoxIndex >= gameData.Boxes.Count)
                {
                    HandleLevelClear();
                    return; 
                }

                // B. UI 갱신 (새로운 상자 정보로 표시)
                // 선택 패널 비우기
                UIManager.SelectionPanel.UpdateUI(gameData.SelectedBundles);
                
                // 상단 상자 정보 갱신 (다음 상자 요구량 표시)
                Box nextBox = GetCurrentBox();
                if (nextBox != null)
                {
                UIManager.UpdateBoxUI(
                    gameData.CurrentBoxIndex, 
                    0, 
                    nextBox.RequiredAmount,
                    gameData.Boxes.Count // ← 추가
                );
            }

            // 하단 아이템 개수 등 갱신
            UpdateAllItemUI();
        });
    }
    else
    {
        // 만약 UIManager가 없거나 연결 안 됐을 경우를 대비한 안전장치 (즉시 갱신)
        RefreshUI();
        if (gameData.CurrentBoxIndex >= gameData.Boxes.Count) HandleLevelClear();
    }
}
    private void HandleTutorialStep()
    {
        switch (tutorialStep)
        {
            case 0: 
                SetAllButtonsInteractable(false);
                UIManager.UndoButton.interactable = true;
                UIManager.StartWiggle(UIManager.UndoButton.gameObject);
                
                // ShowWarning 대신 ShowTutorialPopup 사용
                ShowTutorialPopup("잘하셨습니다카피!\n이제 왼쪽에 있는 아이템을 사용해 보세요.", () =>
                {
                    ShowTutorialPopup("상자되돌리기 버튼을 누르면 이전 상자로 돌아갑니다.\n눌러보세요!", null);
                    tutorialStep = 1;
                });
                
                break;
        }
    }
    public void OnUndoSuccess() 
    {
        if (IsTutorialScene && tutorialStep == 1)
        {
            // 첫 번째 팝업: "이전 상태로 되돌아갔습니다" (필요 시) 혹은 바로 지시 사항 전달
            // 확인 버튼을 눌러야만 tutorialStep이 변경되고 다음 단계로 넘어감
            ShowTutorialPopup("성공적으로 되돌렸습니다카피!\n이제 아무 보석이나 하나 클릭해 보세요.", () => {
                tutorialStep = 2;
                // 팝업이 닫힌 후에 실행될 로직을 콜백에 배치
                OnTutorialBundleClicked(); 
            });
        }
    }

    private void OnTutorialBundleClicked()
    {
        if (IsTutorialScene && tutorialStep == 2)
        {
            SetAllButtonsInteractable(false);
            UIManager.Undo1Button.interactable = true;
            UIManager.StartWiggle(UIManager.Undo1Button.gameObject);
            
            // 사용자가 내용을 읽고 확인을 눌러야 팝업이 닫힘
            
            ShowTutorialPopup("보석한개 되돌리기 버튼은 보석 하나만 되돌립니다.\n눌러보세요!", () => {
                tutorialStep = 3;
            });
            
            
        }
    }

    private IEnumerator ReturnToTutorialAfterDelay()
    {
        // 1. 상단 알림으로 안내 (기존 ShowTopNotification 활용)
        ShowTutorialPopup("실습을 모두 마쳤습니다!\n튜토리얼로 돌아갑니다카피.", null);        
        // 2. (선택 사항) 만약 전용 팝업창을 띄우고 싶다면 BaseConfirmationPopup 등을 활용 가능
        /*
        GameObject popupObj = PopupParentSetHelper.Instance.CreatePopup("Prefabs/BaseConfirmationPopup");
        BaseConfirmationPopup popup = popupObj.GetComponent<BaseConfirmationPopup>();
        popup.Setup("학습이 완료되었습니다.\n튜토리얼 씬으로 돌아갈까요?", 
            () => { SceneManager.LoadScene("TutorialScene"); }, // 확인 버튼 클릭 시
            null); 
        */
        ProgressData progressData = SaveManager.LoadData<ProgressData>("ProgressData");
        progressData.isTutorialSequenceFinished = true;
        SaveManager.Save(progressData, "ProgressData");
        Debug.Log($"[LevelModeManager] 튜토리얼 실습 완료 처리 {progressData.isTutorialSequenceFinished}");
        IsTutorialScene = false;
        // 3. 잠시 대기 (사용자가 메시지를 읽을 시간)
        yield return new WaitForSeconds(2.0f);
        
        // 4. 튜토리얼 상태 초기화 및 씬 전환
        PlayerPrefs.SetInt("TutorialPracticeMode", 0); // 실습 모드 종료
        PlayerPrefs.SetInt("LevelTutorialDone", 1);
        PlayerPrefs.Save();
        IsTutorialScene = false;
        
        // DOTween 등이 동작 중일 수 있으므로 정리 후 이동
        DOTween.KillAll();
        SceneManager.LoadScene("Tutorial"); 
    }


    // ========== 게임오버/클리어 체크 ==========
    private bool CheckGameOver()
    {
        if(gameData.CurrentBoxIndex < gameData.Boxes.Count)
        {
            foreach(var kvp in gameData.RemainingGems)
            {
                if(kvp.Value <= 0) return true;
            }
        }
        return false;
    }
    
    // ========== 게임오버 ==========
    private void HandleGameOver(string reason)
    {
        gameData.GameState = GameState.GameOver;
        StopCoroutine(timeCheckCoroutine);
        // ===== 추가: VideoPlayer + BGM 정지 =====
    StopBackgroundMedia();
    CapyDialogue.StopDialogue(CapyDialogueText);
    CapyDialogueBUbble.SetActive(false);
    
        // 1. 표시할 최종 메시지 결정 (기본값: reason)
        string finalMessage = reason;

        // CapyDialogue에서 'GemDepletedGameOver' 타입의 랜덤 대사를 가져옴
        if(CapyDialogue != null)
        {
            // 아까 만든 함수 호출
            string randomMsg = CapyDialogue.GetRandomMessage(DialogueType.GemDepletedGameOver);
            
            // 가져온 대사가 비어있지 않다면 최종 메시지로 채택
            if (!string.IsNullOrEmpty(randomMsg))
            {
                finalMessage = randomMsg;
            }
            

        }
        
        // 2. 게임오버 팝업 생성
        GameObject popupObj = PopupParentSetHelper.Instance.CreatePopup("Prefabs/GameOverPopup");
        Debug.Log("[LevelModeManager] 게임오버 팝업 생성");
        GameOverPopup popup = popupObj.GetComponent<GameOverPopup>();
        SoundManager.Instance.PlayFX(SoundType.GameOver);
        
        if(popup != null)
        {
            // 3. 팝업에도 위에서 결정한 finalMessage를 전달
            popup.Setup(
                finalMessage, 
                () => RestartCurrentLevel(), // 다시하기
                () => GoToMainHome()  // 메인으로
            );
        }
        else
        {
            // fallback
            Debug.LogError("[LevelModeManager] GameOverPopup을 찾을 수 없습니다!");
        }
    }

    public void RestartCurrentLevel()
    {
        Time.timeScale = 1f; // 시간 흐름 복구
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        Debug.Log("[LevelModeManager] 현재 레벨 재시작");  
    }

    
private void HandleLevelClear()
{
    gameData.GameState = GameState.Win;
    StopCoroutine(timeCheckCoroutine);
    
    StopBackgroundMedia();
    CapyDialogue.StopDialogue(CapyDialogueText);

    // 1. 별 개수 계산
    float clearTime = gameData.ElapsedTime;
    float maxTime = GetDynamicTimeLimit();
    int starCount = 1;
    if (clearTime <= maxTime * 0.6f) starCount = 3;
    else if (clearTime <= maxTime * 0.8f) starCount = 2;

    string clearMessage = GetClearMessage(clearTime);

    // 2. 데이터 로드
    ProgressData progressData = SaveManager.LoadData<ProgressData>("ProgressData");
    int currentLevel = gameData.CurrentLevelIndex;

    // ★ 수정 포인트: 기존 별 개수를 먼저 가져온 후 사과를 계산해야 합니다.
    int oldStars = progressData.GetStars(currentLevel); 
    
    // 3. 사과 지급 (데이터를 변경하기 전에 호출)
    if(AppleManager.Instance != null)
    {
        // AppleManager 내부에서 newStars와 oldStars를 비교해 차액을 지급합니다.
        AppleManager.Instance.AddApplesFromStars(oldStars, starCount);
    }

    // 4. 이제 별 개수를 갱신하고 저장합니다.
    progressData.SetStars(currentLevel, starCount);
    if (progressData.LastClearedLevel < currentLevel)
    {
        progressData.LastClearedLevel = currentLevel;
    }
    
    SaveManager.Save(progressData, "ProgressData");
    
    SoundManager.Instance.PlayFX(SoundType.GameClear);

    // 3. 레벨별 분기 처리 //수정필요 엔딩프리팹으로 연결해야함
    if (gameData.CurrentLevelIndex == 100)
    {
        int clearTimeMs = Mathf.RoundToInt(clearTime * 1000);

        if (!progressData.isLevel100Completed)
        {
            // 최초 클리어
            progressData.isLevel100Completed = true;
            progressData.BestTime = clearTimeMs;
            
            SaveManager.Save(progressData, "ProgressData");
            SaveManager.DeleteSave("GameData");
            TriggerEnding();
        }
        else
        {
            // 재클리어: 기록 경신 확인
            if (progressData.BestTime == 0 || clearTimeMs < progressData.BestTime)
            {
                progressData.BestTime = clearTimeMs;
                SaveManager.Save(progressData, "ProgressData");
            }
            
            // ✅ 레벨 4는 NextLevel 버튼 비활성화
            ShowLevelClearPopup(starCount, clearMessage, isLastLevel: true);
        }
    }
    else
    {
        // 레벨 1~3 클리어
        ShowLevelClearPopup(starCount, clearMessage, isLastLevel: false);
    }
}

    private void ShowTutorialPopup(string message, System.Action onConfirm)
    {
        if (PopupParentSetHelper.Instance == null) return;

        GameObject popupObj = PopupParentSetHelper.Instance.CreatePopup("Prefabs/BaseWarningPopup");
        BaseWarningPopup popup = popupObj.GetComponent<BaseWarningPopup>();
        if (popup != null)
        {
            // 사용자가 확인 버튼을 눌렀을 때만 onConfirm이 실행됨
            popup.Setup(message, onConfirm);
        }
    }

// ✅ 팝업 생성 함수 수정
private void ShowLevelClearPopup(int starCount, string message, bool isLastLevel)
{
    GameObject popupObj = PopupParentSetHelper.Instance.CreatePopup("Prefabs/LevelClearPopup");
    LevelClearPopup popup = popupObj.GetComponent<LevelClearPopup>();
    
    if (popup != null)
    {
        popup.Setup(
            starCount, 
            message,
                () => GoToNextLevel(),
                () => RestartLevel(),
                () => GoToMainHome()
        );
        
        // ✅ 레벨 4면 다음 레벨 버튼 비활성화
        if (isLastLevel && popup.NextLevelButton != null)
        {
            popup.NextLevelButton.interactable = false;
        }
    }
}

// ✅ 다음 레벨로 이동 함수 추가
public void GoToNextLevel()
{
    // 1. 시간 흐름 초기화
    Time.timeScale = 1f;

    // 2. 현재 레벨 번호 가져오기 (기본값 1)
    int currentLevel = PlayerPrefs.GetInt("SelectedLevel", 1);
    int nextLevel = currentLevel + 1;

        // ✅ 다음 챕터 LevelConfig 확인
        int nextChapter;
        if(nextLevel == 100)
        {
            nextChapter = 100;
        }
        else if(nextLevel >= 67)
        {
            nextChapter = 3;
        }
        else if(nextLevel >= 34)
        {
            nextChapter = 2;
        }
        else
        {
            nextChapter = 1;
        }
        
        LevelConfig nextConfig = Resources.Load<LevelConfig>($"LevelData/LevelConfig_{nextChapter}");

    if (nextConfig != null)
    {
        // 다음 레벨이 존재하면 정보 갱신 및 저장
        PlayerPrefs.SetInt("SelectedLevel", nextLevel);
        PlayerPrefs.Save();

        // 현재 게임 씬 다시 로드 (InitGame에서 새 SelectedLevel을 읽어옴)
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    else
    {
        // 더 이상 다음 레벨이 없으면 메인 홈으로 이동
        Debug.Log("모든 레벨을 클리어했습니다! 메인으로 돌아갑니다.");
        GoToMainHome();
    }
}

/// <summary>
/// 배경 Video Player와 BGM을 즉시 정지
/// </summary>
private void StopBackgroundMedia()
{
    // VideoPlayer 정지
    if(backgroundVideoPlayer != null && backgroundVideoPlayer.isPlaying)
    {
        backgroundVideoPlayer.Stop();
        Debug.Log("[LevelModeManager] VideoPlayer 정지");
    }
    
    // BGM 정지
    if(SoundManager.Instance != null)
    {
        SoundManager.Instance.StopBGM();
    }
}

  private string GetClearMessage(float clearTime)
    {
        float maxTime = GetDynamicTimeLimit();
        
        // 2. 비율 기준 계산
        // - 절반 (50%)
        float fastCutoff = maxTime * 0.5f; 
        // - 3분의 2 (약 66%) - '3/2'는 오타로 보고 '2/3' 지점으로 설정했습니다.
        float normalCutoff = maxTime * (2f / 3f); 

        // 3. 메시지 분기 처리
        if(clearTime <= fastCutoff)
        {
            // 제한시간의 절반보다 빨리 깸 (매우 빠름)
            return $"대단하다카피! 소요시간: {clearTime:F1}초\n제한시간의 절반도 안 썼어카피!";
        }
        else if(clearTime <= normalCutoff)
        {
            // 제한시간의 2/3 안쪽으로 깸 (적당함)
            return $"잘했다카피! 소요시간: {clearTime:F1}초\n다음 레벨도 화이팅카피!";
        }
        else
        {
            // 제한시간이 거의 다 되어서 깸 (느림)
            return $"클리어카피! 소요시간: {clearTime:F1}초\n 조금 느리지만.. 괜찮다카피~";
        }
    }

    private void TriggerEnding()
    {
        if(EndingPrefab == null)
        {
            Debug.LogError("[LevelModeManager] EndingPrefab이 할당되지 않았습니다!");
            SceneManager.LoadScene("MainHome");
            return;
        }
        
        GameObject endingObj = Instantiate(EndingPrefab, EndingPopupTransfrom);
        EndingManager endingManager = endingObj.GetComponent<EndingManager>();
        
        if(endingManager != null)
        {
            endingManager.OnEndingCompleted += OnEndingFinished;
        }
    }
    
    private void OnEndingFinished()
    {
        PlayerPrefs.SetInt("ShowRankingOnStart", 1);
        SceneManager.LoadScene("MainHome");
    }
    
    // ========== 타임오버 ==========
    private IEnumerator CheckTimeOver()
    {
        float timeLimit = GetDynamicTimeLimit();
        bool lowTimeWarningShown = false;

        // gameData.ElapsedTime을 0으로 시작하거나 유지
        while(true)
        {
            if(gameData.GameState == GameState.Playing)
            {
                // 매 프레임 흐른 시간을 누적 (timeScale이 0이면 0이 더해짐)
                gameData.ElapsedTime += Time.deltaTime; 
                
                float remaining = timeLimit - gameData.ElapsedTime;
                
                // UI 업데이트
                if (UIManager.TimerSlider != null)
                    UIManager.TimerSlider.value = Mathf.Clamp01(remaining / timeLimit);

                // 경고 로직
                if(!lowTimeWarningShown && remaining <= 30f && remaining > 0f)
                {
                    if(CapyDialogue != null && CapyDialogueText != null)
                        CapyDialogue.ShowDialogue(CapyDialogueText, DialogueType.TimeLowWarning);
                    lowTimeWarningShown = true;
                   
                }

                // 타임오버
                if(remaining <= 0)
                {
                    HandleTimeOver();
                    yield break;
                }
            }
            yield return null; // 다음 프레임까지 대기
        }
    }

    // 특정 UI 강조 기능 (예: 화살표나 반짝이는 효과)
    public void HighlightUI(string objectName)
    {
        GameObject target = GameObject.Find(objectName);
        if (target != null)
        {
            // 대상 오브젝트를 제외한 배경을 어둡게 하거나 화살표 표시 (DOTween 활용)
            target.transform.DOScale(1.2f, 0.5f).SetLoops(-1, LoopType.Yoyo);
        }
    }
    
 // ✅ 타임오버 처리 (시간추가 버튼 포함)
    private void HandleTimeOver()
{
        Debug.Log("[HandleTimeOver] 타임오버 발생!");
    
    gameData.GameState = GameState.TimeOver;
        isWaitingForTimeAdd = true;
     
        // 음악/영상 일시정지 (시간추가 가능성)
        if(backgroundVideoPlayer != null && backgroundVideoPlayer.isPlaying)
        {
            backgroundVideoPlayer.Pause();
        }
        if(SoundManager.Instance != null)
        {
            SoundManager.Instance.PauseBGM();
        }
        
        CapyDialogue.StopDialogue(CapyDialogueText);
    
    string randomMsg = CapyDialogue.GetRandomMessage(DialogueType.TimeOverGameOver);
   
        GameObject popupObj = PopupParentSetHelper.Instance.CreatePopup("Prefabs/GameOverPopup");
        GameOverPopup popup = popupObj.GetComponent<GameOverPopup>();
        
        SoundManager.Instance.PlayFX(SoundType.GameOver);
        
        if(popup != null)
    {
            // ✅ 시간추가 콜백 전달
            popup.Setup(
                randomMsg,
                () => RestartLevel(),
                () => GoToMainHome(),
                () => OnTimeAddRequested() // ✅ 시간추가
            );
        }
    }
     // ========== 씬 전환 ==========
    public void RestartLevel()
    {
        if(SoundManager.Instance != null)
        {
            SoundManager.Instance.StopAllBGM();
        }
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    public void GoToMainHome()
    {
        if(SoundManager.Instance != null)
        {
            SoundManager.Instance.StopAllBGM();
        }
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainHome");
    }

    
    // ✅ 시간추가 요청
    private void OnTimeAddRequested()
    {
        if(AppleManager.Instance == null)
    {
            Debug.LogError("[LevelModeManager] AppleManager가 없습니다!");
        return;
    }
   
        AppleManager.Instance.TryPurchaseTimeAdd(
            onSuccess: () => {
                ExecuteTimeAdd();
            },
            onNoApples: () => {
                ShowTimeAddAdPopup();
            }
        );

    }
     // ✅ 광고 시청 팝업
    private void ShowTimeAddAdPopup()
    {
        GameObject popupObj = PopupParentSetHelper.Instance.CreatePopup("Prefabs/BaseConfirmationPopup");
        BaseConfirmationPopup popup = popupObj.GetComponent<BaseConfirmationPopup>();
        
        popup.Setup(
            "사과가 부족합니다.\n광고를 시청하여 시간을 추가하시겠습니까?",
            () => {
                AdManager.Instance.ShowRewardedAd((success) => {
                    if(success)
                    {
                        AppleManager.Instance.AddApplesFromAd();
                        ExecuteTimeAdd();
                    }
                });
            },
            null
        );
    }
    


    // ✅ 시간추가 실행
    private void ExecuteTimeAdd()
    {
        Time.timeScale = 1f;
        float addTime = CurrentLevelConfig.TimeAddAmount;
        float timeLimit = GetDynamicTimeLimit();

        if(timeCheckCoroutine != null)
        {
            StopCoroutine(timeCheckCoroutine);
        }
        
        // 1. 데이터 수정: ElapsedTime을 감소시켜 남은 시간을 늘림
        // (예: 60초 제한에 60초 다 썼을 때 10초 추가하면 ElapsedTime을 50초로 만듦)
        gameData.ElapsedTime = Mathf.Max(0, gameData.ElapsedTime - addTime);
        
        // 2. 게임 상태 복구
        gameData.GameState = GameState.Playing;
        isWaitingForTimeAdd = false;
        
        // 3. UI 즉시 갱신 (슬라이더가 즉시 늘어나는 것을 보여줌)
        if (UIManager.TimerSlider != null)
        {
            float remaining = timeLimit - gameData.ElapsedTime;
            UIManager.TimerSlider.value = Mathf.Clamp01(remaining / timeLimit);
        }

        // 4. 음악/영상 재개
        if(backgroundVideoPlayer != null)
        {
            backgroundVideoPlayer.Play();
        }
        if(SoundManager.Instance != null)
        {
            SoundManager.Instance.ResumeBGM();
        }
        
        // 5. 타이머 코루틴 관리
        // 이미 CheckTimeOver가 돌고 있다면 중복 생성하지 않도록 처리
        timeCheckCoroutine = StartCoroutine(CheckTimeOver());
        
        // 6. 대사창 복구
        if(CapyDialogue != null && CapyDialogueText != null)
        {
            CapyDialogue.ShowDialogue(CapyDialogueText, DialogueType.Default);
        }
        
        ShowTopNotification($"시간 +{addTime}초 추가되었습니다카피!");
        Debug.Log($"[LevelModeManager] 시간추가 완료! 현재 경과시간: {gameData.ElapsedTime} / 제한시간: {timeLimit}");
    }
    

    
    // ========== Undo/Refresh/Hint ==========
    public void ProcessUndo()
    {
        if(gameData.CompletedBoxes.Count == 0)
        {
            ShowWarning("되돌릴 상자가 없습니다카피!");
            return;
        }
        
        gameData.UndoCount++;  // ✅ 횟수는 무조건 증가 (원복 X)
        
        
        if(gameData.UndoCount > CurrentLevelConfig.MaxUndoCount)
        {
            // [수정됨] 확인 팝업 먼저 표시
            ShowAdConfirmationPopup(() =>
            {
                // Yes 클릭 시에만 광고 호출
                AdManager.Instance.ShowRewardedAd((success) =>
                {
                    if(success)  StartCoroutine(ExecuteUndoWithCancle());
                    // ✅ 실패해도 Count 원복 안 함 (누적 카운터이므로)
                });
            }, 
            null); // ✅ No 버튼도 Count 원복 안 함
        }
        else
        {
            StartCoroutine(ExecuteUndoWithCancle());
        }
        if (tutorialStep == 1) 
        {
            ApplyUndoTutorialRule();
        }
    }
    private void ApplyUndoTutorialRule()
    {
        if (UIManager != null)
        {
            // 1. Undo 버튼 비활성화
            if (UIManager.UndoButton != null)
            {
                UIManager.StopWigggleInTutorial(UIManager.UndoButton.gameObject); 
                UIManager.UndoButton.interactable = false;
                // 만약 아예 안 보이게 하고 싶다면 gameObject.SetActive(false);
            }

            // 2. Complete 버튼 활성화
            if (UIManager.CompleteButton != null)
            {
                UIManager.CompleteButton.interactable = true;
                // 만약 숨겨져 있었다면 gameObject.SetActive(true);
            }
        }
        OnUndoSuccess();
    }

    private IEnumerator ExecuteUndoWithCancle()
        {
            // 2. 선택 강제 비우기 (애니메이션 포함)
        if(gameData.SelectedBundles.Count > 0)
        {
            CancelSelection();
            
            // CancelSelection의 애니메이션 시간 대기
            // (DOTween 0.3초 축소 + 0.2초 팝업 = 약 0.5초)
            yield return new WaitForSeconds(0.6f);
        }
        ExecuteUndo();
    }   


    
    private void ExecuteUndo()
    {
        CompletedBox lastBox = gameData.CompletedBoxes[gameData.CompletedBoxes.Count - 1];
        gameData.CompletedBoxes.RemoveAt(gameData.CompletedBoxes.Count - 1);
        
        foreach(var bundle in lastBox.UsedBundles)
        {
            gameData.RemainingGems[bundle.GemType] += bundle.GemCount;
            gameData.BundlePool.Insert(0, bundle);
        
        if (GemCountStatusPanel != null)
        {
            GemCountStatusPanel.UpdateGemCount(bundle.GemType, gameData.RemainingGems[bundle.GemType], RemainingBoxes);
        }

        }
        
        gameData.CurrentBoxIndex--;
        gameData.SelectedBundles.Clear();
        UpdateAllItemUI();
        
        // 연속 성공 카운트 리셋
        consecutiveSuccessCount = 0;
        
        ExtractDisplayBundles();
        RefreshUI();
        
        ShowTopNotification("이전 상태로 되돌아갔습니다카피!");
    }
    
    public void ProcessRefresh()
{
    gameData.RefreshCount++;  // ✅ 횟수는 무조건 증가
        
    if(gameData.RefreshCount > CurrentLevelConfig.MaxRefreshCount)
    {
        ShowAdConfirmationPopup(() =>
        {
            AdManager.Instance.ShowRewardedAd((success) =>
            {
                if(success) ExecuteRefresh();
                // ✅ Count 원복 없음
            });
        },
        null); // ✅ Count 원복 없음
    }
    else
    {
        ExecuteRefresh();
    }
    if(tutorialStep == 4)
    {
        ApplyRefreshTutorialRule();
    }
}
    private void ApplyRefreshTutorialRule()
    {
        if (UIManager != null)
        {
            if (UIManager.RefreshButton != null)
            {
                UIManager.StopWigggleInTutorial(UIManager.RefreshButton.gameObject); 
                UIManager.RefreshButton.interactable = false;
            }

            if (UIManager.CancelSelectButton != null)
            {
                UIManager.CancelSelectButton.interactable = true;
                UIManager.StartWiggle(UIManager.CancelSelectButton.gameObject);
            }
        }
        
        ShowTutorialPopup("버리기 버튼을 눌러 선택한 보석을 비워보세요!", () => {
            tutorialStep = 5;
        });
        
    }


    
    private void ExecuteRefresh()
    {
        foreach(var bundle in gameData.CurrentDisplayBundles)
        {
            if(!gameData.BundlePool.Contains(bundle))
            {
                gameData.BundlePool.Add(bundle);
            }
        }
        
        System.Random rng = new System.Random();
        gameData.BundlePool = gameData.BundlePool.OrderBy(x => rng.Next()).ToList();
        UpdateAllItemUI();
        
        ExtractDisplayBundles();
        RefreshUI();
        
        ShowTopNotification("카드가 재배열되었습니다카피!");
    }
   

    public void Process1Undo()
    {
        // 1. 현재 선택된 보석이 하나도 없다면 되돌릴 수 없음
        if (gameData.SelectedBundles == null || gameData.SelectedBundles.Count == 0)
        {
            ShowWarning("되돌릴 보석이 없습니다카피!");
            return;
        }

        // 2. 횟수 제한 체크 및 광고 로직
        // 무료 횟수를 다 썼다면 광고 확인 팝업을 띄움
        if (gameData.Undo1Count >= CurrentLevelConfig.MaxUndo1Count)
        {
            ShowAdConfirmationPopup(() =>
            {
                AdManager.Instance.ShowRewardedAd((success) =>
                {
                    if (success) Execute1Undo(); // 광고 성공 시 실행
                });
            }, null);
        }
        else
        {
            gameData.Undo1Count++; // 무료 사용 횟수 증가
            Execute1Undo();
        }
        if(tutorialStep == 3)
        {
            ApplyUndo1TutorialRule();
        }
    }
    private void ApplyUndo1TutorialRule()
    {
        if (UIManager != null)
        {
            if (UIManager.Undo1Button != null)
            {
                UIManager.StopWigggleInTutorial(UIManager.Undo1Button.gameObject); 
                UIManager.Undo1Button.interactable = false;
            }

            if (UIManager.RefreshButton != null)
            {
                UIManager.RefreshButton.interactable = true;
                UIManager.StartWiggle(UIManager.RefreshButton.gameObject);
            }
        }
        
        // 확인을 누른 후에 step을 4로 변경하여 로직 겹침 방지
        ShowTutorialPopup("새로고침 버튼을 눌러보세요!", () => {
            tutorialStep = 4;
        });
        
    }

    public void Execute1Undo()
    {
        // 1. 안전 장치: 선택된 보석이 없는 경우
        if (gameData.SelectedBundles == null || gameData.SelectedBundles.Count == 0)
        {
            ShowWarning("되돌릴 보석이 없습니다카피!");
            return;
        }

        // 2. 가장 마지막에 추가된 보석 묶음 데이터 가져오기
        int lastIndex = gameData.SelectedBundles.Count - 1;
        GemBundle lastBundle = gameData.SelectedBundles[lastIndex];

        // 3. 데이터 복구
        gameData.SelectedBundles.RemoveAt(lastIndex); // 선택 리스트에서 제거
        gameData.RemainingGems[lastBundle.GemType] += lastBundle.GemCount; // 보석 총량 복구 (GemCountPanel용)

        // 4. 그리드 위치(Index) 정보 확인 및 복구
        if (selectedBundleOriginalIndices.TryGetValue(lastBundle, out int originalGridIndex))
        {
            // A. 상자(선택창) UI에서 이미지 제거
            if (UIManager != null && UIManager.SelectionPanel != null)
            {
                UIManager.SelectionPanel.RemoveLastGem();
            }

            // B. 현재 그리드 칸에 있는 데이터(보통 빈 칸/Placeholder일 것임)를 다시 Pool로 반환
            GemBundle currentOccupant = gameData.CurrentDisplayBundles[originalGridIndex];
            if (currentOccupant != null && !gameData.BundlePool.Contains(currentOccupant))
            {
                gameData.BundlePool.Add(currentOccupant);
            }

            // C. 그리드 데이터 리스트에 원래 보석 데이터 복구
            gameData.CurrentDisplayBundles[originalGridIndex] = lastBundle;

            // D. [핵심] GridManager를 통해 실제 UI 프리팹을 다시 보이게 함
            if (GridManager != null)
            {
                // isRestoring: true를 전달하여 투명도와 인터랙션을 즉시 복구함
                GridManager.ReplaceBundleAtIndex(
                    originalGridIndex,
                    lastBundle,
                    OnBundleClicked,
                    isRestoring: true 
                );
            }

            // 사용한 인덱스 정보 삭제 (중복 방지)
            selectedBundleOriginalIndices.Remove(lastBundle);
        }
        else
        {
            // 만약 인덱스 정보를 잃어버렸다면 전체 그리드를 다시 그림 (강제 동기화)
            Debug.LogWarning($"[Execute1Undo] {lastBundle.BundleID}의 인덱스 정보를 찾지 못해 그리드를 전체 갱신합니다.");
            ExtractDisplayBundles();
        }

        // 5. 기타 UI 및 아이템 카운트 갱신
        UpdateAllItemUI();
        RefreshUI();
        
        // GemCountPanel(위험도/보석개수) 숫자 업데이트
        if (GemCountStatusPanel != null)
        {
            GemCountStatusPanel.UpdateGemCount(
                lastBundle.GemType, 
                gameData.RemainingGems[lastBundle.GemType], 
                RemainingBoxes
            );
        }

        ShowTopNotification("마지막 보석 선택을 취소했습니다카피!");
    }


// public void ProcessHint()
// {
//     // 게임당 1회 제한
//     if(gameData.HintCount >= CurrentLevelConfig.MaxHintCount)
//     {
//         ShowAdConfirmationPopup(() =>
//         {
//             AdManager.Instance.ShowRewardedAd((success) =>
//             {
//                 if(success)
//                 {
                    
//                    StartCoroutine(ExecuteHintWithLoading());
//                 }
//             });
//         },
//         null);
//     }
// }

// ✅ 아이템 구매 팝업 표시
private void ShowItemPurchasePopup(string itemName, Action onPurchaseSuccess)
    {
    GameObject popupObj = PopupParentSetHelper.Instance.CreatePopup("Prefabs/ItemPurchasePopup");
    ItemPurchasePopup popup = popupObj.GetComponent<ItemPurchasePopup>();
       
    if(popup != null)
    {
        popup.Setup(itemName, onPurchaseSuccess);
    }
}


// // ✅ 새 메서드: 로딩 UI 포함 힌트 실행
// // ✅ ExecuteHintWithLoading() 수정 - HintManager 사용
// private IEnumerator ExecuteHintWithLoading()
// {
//     // 1. 로딩 UI 표시
//     if(HintLoadingUI != null)
//     {
//         HintLoadingUI.SetActive(true);
//     }
    
//     // 2. 선택 강제 비우기
//     if(gameData.SelectedBundles.Count > 0)
//     {
//         CancelSelection();
//         yield return new WaitForSeconds(0.6f);
//     }
    
//     // 3. 한 프레임 대기 (백트래킹 시간 확보)
//     yield return null;
    
//     // 4. ✅ HintManager에 위임
//     List<GemBundle> hintBundles = hintManager.FindHintCombination(
//         GetCurrentBox(),
//         gameData.BundlePool,
//         gameData.CurrentDisplayBundles,
//         gameData.Boxes.Count - gameData.CurrentBoxIndex,
//         CurrentLevelConfig.GemTypeCount
//     );
    
//     // 5. 로딩 UI 숨김
//     if(HintLoadingUI != null)
//     {
//         HintLoadingUI.SetActive(false);
//     }
    
//     // 6. 결과 처리
//     if(hintBundles != null && hintBundles.Count > 0)
//     {
//         gameData.HintCount++;
//         GridManager.ShakeBundles(hintBundles);
//         ShowTopNotification("힌트를 확인하세요카피!");
//     }
//     else
//     {
//         // 모든 전략 실패 → 이미 글렀음
//         if(CapyDialogue != null && CapyDialogueText != null)
//         {
//             CapyDialogue.ShowDialogue(CapyDialogueText, DialogueType.AlreadyFailed);
//         }
//     }
    
//     UpdateAllItemUI();
// }


// Assets/Scripts/Manager/LevelModeManager.cs

// private void ExecuteHint()
// {
//     // 1단계: 글렀는지 빠른 판정
//     if(!CheckIfSolvable())
//     {
//         if(CapyDialogue != null && CapyDialogueText != null)
//         {
//             CapyDialogue.ShowDialogue(CapyDialogueText, DialogueType.AlreadyFailed);
//         }
//         return;
//     }
    
//     // 2~3단계: 힌트 조합 찾기
//     List<GemBundle> hintBundles = FindHintCombination();
    
//     if(hintBundles != null && hintBundles.Count > 0)
//     {
//         gameData.HintCount++;
//         // 힌트 표시 (흔들림)
//         GridManager.ShakeBundles(hintBundles);
        
//         ShowTopNotification("힌트를 확인하세요카피!");
            
//     }
//     else
//     {
//         // 조합 실패 (현재 화면에서 불가능)
//         ShowWarning("현재 화면에서 조합을 찾을 수 없습니다카피! 새로고침을 추천합니다카피!");
//     }
    
//     UpdateAllItemUI();
// }

// ========== 1단계: 빠른 글렀는지 판정 ==========
private bool CheckIfSolvable()
{
    int remainingBoxes = gameData.Boxes.Count - gameData.CurrentBoxIndex;
    
    // 각 색깔별로 체크
    for(int i = 0; i < CurrentLevelConfig.GemTypeCount; i++)
    {
        GemType type = (GemType)i;
        // ✅ 수정: null 필터링
        int totalBundles = gameData.BundlePool
            .Count(b => b != null && b.GemType == type);
        
        
        
        // 번들 개수 < 남은 상자 개수 → 불가능
        if(totalBundles < remainingBoxes)
        {
            Debug.Log($"[Hint] {type} 색 번들 부족: {totalBundles}개 < {remainingBoxes}상자");
            return false;
        }
    }
    
    return true;
}

// ========== 2~3단계: 힌트 조합 찾기 ==========
private List<GemBundle> FindHintCombination()
{
    Box currentBox = GetCurrentBox();
    int requiredAmount = currentBox.RequiredAmount;
    int remainingBoxes = gameData.Boxes.Count - gameData.CurrentBoxIndex;
    
    // 2단계: 선택 가능 풀 생성
    var pools = BuildSelectablePools(remainingBoxes, requiredAmount);
    
    if(pools == null)
    {
        Debug.Log("[Hint] 선택 가능 풀 생성 실패");
        return null;
    }
    
    // 작업용 복사본 생성 (원본 보존)
    var workingPools = CreateWorkingPools(pools);
    
    // 3단계: 조합 생성
    List<GemBundle> selectedBundles = new List<GemBundle>();
    
    // 3-1. 각 색 최소 1개씩 (작은 것부터)
    int minSelectedTotal = 0;
    
    for(int i = 0; i < CurrentLevelConfig.GemTypeCount; i++)
    {
        GemType type = (GemType)i;
        
        if(!workingPools.ContainsKey(type) || workingPools[type].AvailableBundles.Count == 0)
        {
            Debug.Log($"[Hint] {type} 색 선택 가능 번들 없음");
            return null;
        }
        
        var smallest = workingPools[type].AvailableBundles
            .OrderBy(b => b.GemCount)
            .FirstOrDefault();
        
        
        
        selectedBundles.Add(smallest);
        minSelectedTotal += smallest.GemCount;
        
        workingPools[type].AvailableBundles.Remove(smallest);
        workingPools[type].RemainingSelectCount--;
    }
    
    // 총량 초과 체크
    if(minSelectedTotal > requiredAmount)
    {
        Debug.Log($"[Hint] 최소 선택으로 총량 초과: {minSelectedTotal} > {requiredAmount}");
        return null;
    }
    
    // 총량 만족 체크
    if(minSelectedTotal == requiredAmount)
    {
        Debug.Log("[Hint] 각 색 1개씩으로 정확히 맞음!");
        return selectedBundles;
    }
    
    // 3-2. 부족하면 추가 선택
    int currentTotal = minSelectedTotal;
   
    HashSet<GemType> triedColors = new HashSet<GemType>();
    
    int loopCount = 0; // 안전장치
    int maxLoops = 100;
    
    while(currentTotal < requiredAmount)
    {
        loopCount++;
        if(loopCount > maxLoops)
        {
            Debug.LogError("[Hint] 무한 루프 감지!");
            return null;
        }
         int remaining = requiredAmount - currentTotal;

        // 1단계 선택 가능한 색 찾기
        var availableColors = workingPools
        .Where(p => p.Value.RemainingSelectCount > 0 
                 && p.Value.AvailableBundles.Count > 0
                 && !triedColors.Contains(p.Key))
        .ToList();
    
        
        if(availableColors.Count == 0)
        {
            // 모든 색 다 시도했는데 못 찾음
            Debug.Log("[Hint] 모든 색 시도했으나 조합 실패");
            return null;
        }

        // ✅ 2단계: 번들 개수가 비슷한지 확인
    int maxBundleCount = availableColors.Max(p => p.Value.AvailableBundles.Count);
    var topColors = availableColors
        .Where(p => p.Value.AvailableBundles.Count == maxBundleCount)
        .ToList();
    
    GemBundle selected = null;
    GemType selectedColor = GemType.Red;

        // ✅ 3단계: 번들 개수가 같은 색이 여러 개면 → 전체 통합 검색
    if(topColors.Count > 1)
    {
        Debug.Log($"[Hint] {topColors.Count}개 색의 번들 개수 동일 ({maxBundleCount}개) → 통합 검색");
        
        // 모든 후보 색의 번들을 하나로 합침
        List<(GemBundle bundle, GemType color)> allCandidates = new List<(GemBundle, GemType)>();
        
        foreach(var colorInfo in topColors)
        {
            GemType color = colorInfo.Key;
            foreach(var bundle in workingPools[color].AvailableBundles)
            {
                allCandidates.Add((bundle, color));
            }
        }
        
        // ① 정확히 남은 양을 채우는 번들 찾기
        var exactMatch = allCandidates
            .FirstOrDefault(item => item.bundle.GemCount == remaining);
        
        if(exactMatch.bundle != null)
        {
            selected = exactMatch.bundle;
            selectedColor = exactMatch.color;
            Debug.Log($"[Hint] 정확히 맞는 번들 발견: {selectedColor} {selected.GemCount}개");
        }
        else
        {
            // ② 없으면 큰 번들부터 (남은 양 이하)
            var largestFit = allCandidates
                .Where(item => item.bundle.GemCount <= remaining)
                .OrderByDescending(item => item.bundle.GemCount)
                .FirstOrDefault();
            
            if(largestFit.bundle != null)
            {
                selected = largestFit.bundle;
                selectedColor = largestFit.color;
                Debug.Log($"[Hint] 큰 번들 선택 (통합): {selectedColor} {selected.GemCount}개");
            }
        }
    }
    else
    {
        // ✅ 4단계: 번들 개수가 확실히 많은 색 하나만 있으면 → 기존 로직
        selectedColor = topColors.First().Key;
        
        // ① 정확히 맞는 번들
        selected = workingPools[selectedColor].AvailableBundles
            .FirstOrDefault(b => b.GemCount == remaining);
        
        if(selected == null)
        {
            // ② 큰 번들부터
            selected = workingPools[selectedColor].AvailableBundles
                .Where(b => b.GemCount <= remaining)
                .OrderByDescending(b => b.GemCount)
                .FirstOrDefault();
        }
        
        if(selected != null)
        {
            Debug.Log($"[Hint] {selectedColor} 색 우세 → {selected.GemCount}개 선택");
        }
    }
    
    // ✅ 5단계: 선택 실패 시 해당 색 제외
    if(selected == null)
    {
        if(topColors.Count > 1)
        {
            // 통합 검색 실패 → 모든 후보 색 제외
            foreach(var colorInfo in topColors)
            {
                triedColors.Add(colorInfo.Key);
            }
            Debug.Log($"[Hint] 통합 검색 실패 → {topColors.Count}개 색 제외");
        }
        else
        {
            // 단일 색 실패
            triedColors.Add(topColors.First().Key);
            Debug.Log($"[Hint] {topColors.First().Key} 색 조건 만족 번들 없음");
        }
        continue;
    }
    
    // ✅ 6단계: 선택 성공 → 업데이트
    selectedBundles.Add(selected);
    currentTotal += selected.GemCount;
    
    workingPools[selectedColor].AvailableBundles.Remove(selected);
    workingPools[selectedColor].RemainingSelectCount--;
    
    triedColors.Clear();
    
    Debug.Log($"[Hint] {selectedColor} 색에서 {selected.GemCount}개 번들 선택, 현재 총량: {currentTotal}/{requiredAmount}");
}

Debug.Log($"[Hint] 조합 완성! 총 {selectedBundles.Count}개 번들 선택");
return selectedBundles;
}

// ========== 선택 가능 풀 생성 ==========
private Dictionary<GemType, PoolInfo> BuildSelectablePools(int remainingBoxes, int requiredAmount)
{
    var pools = new Dictionary<GemType, PoolInfo>();
    
    // maxBundleGemCount 계산
    int maxBundleGemCount = requiredAmount - (CurrentLevelConfig.GemTypeCount - 1);
    
    Debug.Log($"[Hint] maxBundleGemCount: {maxBundleGemCount} (요구량 {requiredAmount} - 색 {CurrentLevelConfig.GemTypeCount - 1})");
    
    for(int i = 0; i < CurrentLevelConfig.GemTypeCount; i++)
    {
        GemType type = (GemType)i;
        
        // 해당 색의 모든 번들 (BundlePool + CurrentDisplayBundles)
         List<GemBundle> allBundles = gameData.BundlePool
            .Where(b => b != null && b.GemType == type) // ← null 체크!
            .ToList();
        
        
        int totalBundles = allBundles.Count;
        
        // 여유분 계산
        int surplus = totalBundles - remainingBoxes;
        
        if(surplus < 0)
        {
            Debug.LogError($"[Hint] {type} 색 번들 부족: {totalBundles}개 < {remainingBoxes}상자");
            return null;
        }
        
        // 작은 번들만 필터링
        List<GemBundle> smallBundles = allBundles
            .Where(b => b.GemCount <= maxBundleGemCount)
            .ToList();
        
        // 화면에 표시 중인 번들만 선택 가능
        // ✅ 수정: null 필터링 (이미 위에서 했지만 명확히)
        List<GemBundle> availableBundles = gameData.CurrentDisplayBundles
            .Where(b => b != null && b.GemType == type && b.GemCount <= maxBundleGemCount)
            .ToList();
        
        if(availableBundles.Count == 0)
        {
            Debug.LogWarning($"[Hint] {type} 색의 선택 가능 번들이 화면에 없음");
            // 빈 리스트로 설정 (나중에 체크됨)
        }
        
        pools[type] = new PoolInfo
        {
            AvailableBundles = availableBundles,
            MaxSelectCount = surplus + 1
        };
        
        Debug.Log($"[Hint] {type} 색: 총 {totalBundles}개, 화면 {availableBundles.Count}개, MaxSelect {surplus + 1}");
    }
    
    return pools;
}

// ========== 작업용 풀 복사 ==========
private Dictionary<GemType, WorkingPoolInfo> CreateWorkingPools(Dictionary<GemType, PoolInfo> originalPools)
{
    var workingPools = new Dictionary<GemType, WorkingPoolInfo>();
    
    foreach(var kvp in originalPools)
    {
        workingPools[kvp.Key] = new WorkingPoolInfo
        {
            AvailableBundles = new List<GemBundle>(kvp.Value.AvailableBundles),
            RemainingSelectCount = kvp.Value.MaxSelectCount
        };
    }
    
    return workingPools;
}

// ========== 헬퍼 클래스 ==========
private class PoolInfo
{
    public List<GemBundle> AvailableBundles;
    public int MaxSelectCount;
}

private class WorkingPoolInfo
{
    public List<GemBundle> AvailableBundles;
    public int RemainingSelectCount;
}

private void ShowAdConfirmationPopup(Action onYes, Action onNo)
{
    if(PopupParentSetHelper.Instance == null)
    {
        Debug.LogError("[LevelModeManager] PopupParentSetHelper가 없습니다!");
        return;
    }
    
    GameObject popupObj = PopupParentSetHelper.Instance.CreatePopup("Prefabs/BaseConfirmationPopup");
    
    if(popupObj == null)
    {
        Debug.LogError("[LevelModeManager] BaseConfirmationPopup 생성 실패!");
        return;
    }
    
    BaseConfirmationPopup popup = popupObj.GetComponent<BaseConfirmationPopup>();
    
    if(popup == null)
    {
        Debug.LogError("[LevelModeManager] BaseConfirmationPopup 컴포넌트를 찾을 수 없습니다!");
        return;
    }
    
    popup.Setup(
        "사용량을 초과하였습니다. 광고를 시청하시겠습니까?\n(광고 시청 시 기능을 한 번 사용할 수 있습니다)",
        onYes,
        onNo
    );
}
    




private void UpdateAllItemUI()
{
  
  
    // Undo/Refresh는 기존 방식
    int undoLeft = Mathf.Max(0, CurrentLevelConfig.MaxUndoCount - gameData.UndoCount);
    int undo1Left = Mathf.Max(0, CurrentLevelConfig.MaxUndo1Count - gameData.Undo1Count);
    int refreshLeft = Mathf.Max(0, CurrentLevelConfig.MaxRefreshCount - gameData.RefreshCount);
    
     // ✅ 수정: 최대 횟수도 함께 전달
    UIManager.UpdateHintAndItemUI(
        refreshLeft,
        undo1Left,
        undoLeft
    );

}



    
    // ========== 일시정지 ==========
   public void TogglePause()
{
    if(gameData.GameState == GameState.Playing)
    {
        gameData.GameState = GameState.Paused;
        Time.timeScale = 0f;
        UIManager.PausePopupPanel.SetActive(true);
        
        // ===== 추가: VideoPlayer + BGM 일시정지 =====
        if(backgroundVideoPlayer != null && backgroundVideoPlayer.isPlaying)
        {
            backgroundVideoPlayer.Pause();
        }
        
        if(SoundManager.Instance != null)
        {
            SoundManager.Instance.PauseBGM();
        }
            CapyDialogue.StopDialogue(CapyDialogueText);
    }
}
    public void Resume()
    {
        gameData.GameState = GameState.Playing;
        Time.timeScale = 1f;
        
        if (UIManager.PausePopupPanel != null)
        {
            UIManager.PausePopupPanel.transform.DOKill();
            UIManager.PausePopupPanel.SetActive(false);
        }
        
        // ===== 추가: VideoPlayer + BGM 재개 =====
        if(backgroundVideoPlayer != null && !backgroundVideoPlayer.isPlaying)
        {
            backgroundVideoPlayer.Play();
        }
        
        if(SoundManager.Instance != null)
        {
            SoundManager.Instance.ResumeBGM();
        }
            CapyDialogue.ShowDialogue(CapyDialogueText, DialogueType.Default);
    }
    
    // ========== 유틸리티 ==========
    private Box GetCurrentBox()
    {
        // 안전장치: 인덱스가 0보다 작거나 리스트 개수보다 크면 안됨
        if (gameData.CurrentBoxIndex < 0 || gameData.CurrentBoxIndex >= gameData.Boxes.Count)
        {
            Debug.LogWarning($"[GetCurrentBox] 인덱스 범위 초과! Index: {gameData.CurrentBoxIndex}, Total Boxes: {gameData.Boxes.Count}");
            
            // 모든 박스를 다 채운 경우 마지막 박스를 반환하거나 null 처리
            if (gameData.Boxes.Count > 0)
                return gameData.Boxes[gameData.Boxes.Count - 1]; 
            
            return null;
        }
        return gameData.Boxes[gameData.CurrentBoxIndex];
    }
    
    private int CalculateSelectedTotal()
    {
        int total = 0;
        foreach(var bundle in gameData.SelectedBundles)
        {
            total += bundle.GemCount;
        }
        return total;
    }
    
   private void RefreshUI()
{
   UIManager.UpdateBoxUI(
        gameData.CurrentBoxIndex,
        CalculateSelectedTotal(),
        GetCurrentBox().RequiredAmount,
        gameData.Boxes.Count // ← 추가
    );

    // ✅ 추가: 현재 레벨 번호 UI 업데이트
    // gameData.CurrentLevelIndex는 InitGame에서 PlayerPrefs 정보를 통해 저장됩니다.
    if (UIManager != null)
    {
        UIManager.UpdateLevelDisplay(gameData.CurrentLevelIndex);
    }

    // ✅ UpdateAllItemUI() 호출로 통일
    UpdateAllItemUI();
}
    private void ShowWarning(string message)
    {
        if(CapyDialogue != null && CapyDialogueText != null)
        {
            if (message == null)
            {
                CapyDialogue.ShowDialogue(CapyDialogueText, DialogueType.Warning);
                CapyDialogue.RestartDefault(CapyDialogueText, 3.5f);
            }
            else
            {
                CapyDialogue.ShowDialogue(CapyDialogueText, message, false);
                CapyDialogue.RestartDefault(CapyDialogueText, 3.5f);
            }
    
        }
        
        Debug.LogWarning($"[LevelModeManager] {message}");
    }
    
    // ===== CapyDialogue 연결: 경고 메시지 =====
    private void ShowDifficientWarning(string message)
    {
        if(CapyDialogue != null && CapyDialogueText != null)
        {
            if (message == null)
            {
                CapyDialogue.ShowDialogue(CapyDialogueText, DialogueType.DifficientWarning);
                CapyDialogue.RestartDefault(CapyDialogueText, 3.5f);
            }
            else
            {
                CapyDialogue.ShowDialogue(CapyDialogueText, message, false);
                CapyDialogue.RestartDefault(CapyDialogueText, 3.5f);
            }
    
        }
        
        Debug.LogWarning($"[LevelModeManager] {message}");
    }
    private void ShowUnmatchGemWarning(string message)
    {
        if(CapyDialogue != null && CapyDialogueText != null)
        {
            if (message == null)
            {
                CapyDialogue.ShowDialogue(CapyDialogueText, DialogueType.UnmatchGemWarning);
                CapyDialogue.RestartDefault(CapyDialogueText, 3.5f);
            }
            else
            {
                CapyDialogue.ShowDialogue(CapyDialogueText, message, false);
                CapyDialogue.RestartDefault(CapyDialogueText, 3.5f);
            }
    
        }
        
        Debug.LogWarning($"[LevelModeManager] {message}");
    }
    
    private void FlashRedScreen()
    {
        if(FlashOverlay != null)
        {
            StartCoroutine(FlashRedCoroutine());
        }
    }
    
    private IEnumerator FlashRedCoroutine()
    {
       
        FlashOverlay.gameObject.SetActive(true);
        
        yield return new WaitForSeconds(0.2f);
        
        FlashOverlay.gameObject.SetActive(false);
    }
    
   private void ShowTopNotification(string message)
{
    if(NotificationPanel == null || NotificationText == null)
    {
        Debug.LogWarning("[LevelModeManager] NotificationPanel이 할당되지 않았습니다!");
        return;
    }
    
    StopCoroutine(nameof(NotificationCoroutine));
    StartCoroutine(NotificationCoroutine(message));
}

private IEnumerator NotificationCoroutine(string message)
{
    NotificationText.text = message;
    
    // CanvasGroup 가져오기/생성
    CanvasGroup canvasGroup = NotificationPanel.GetComponent<CanvasGroup>();
    if(canvasGroup == null)
    {
        canvasGroup = NotificationPanel.AddComponent<CanvasGroup>();
    }
    
    NotificationPanel.SetActive(true);
    
    // 페이드 인
    canvasGroup.alpha = 0f;
    yield return canvasGroup.DOFade(1f, 0.3f).SetEase(Ease.OutQuad).WaitForCompletion();
    
    // 유지
    yield return new WaitForSeconds(NotificationDuration + 0.5f);
    
    // 페이드 아웃
    yield return canvasGroup.DOFade(0f, 0.3f).SetEase(Ease.InQuad).WaitForCompletion();
    
    NotificationPanel.SetActive(false);
}
}
