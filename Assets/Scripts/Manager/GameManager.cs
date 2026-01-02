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

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    
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
    private ChunkData chunkData;
      [Header("힌트 로딩 UI")]
    public GameObject HintLoadingUI;  // ✅ 추가
    // 시간 관련
    private float levelStartTime;
    private Coroutine timeCheckCoroutine;
    
    // 연속 성공 카운트
    private int consecutiveSuccessCount = 0;
    private int lastCountedSecond = -1; // 중복 호출 방지용
  
private Dictionary<GemBundle, GemBundlePrefab> selectedBundleOriginalPrefabs 
    = new Dictionary<GemBundle, GemBundlePrefab>();
private Dictionary<GemBundle, int> selectedBundleOriginalIndices 
    = new Dictionary<GemBundle, int>(); // Bundle → 원래 Grid 인덱스

 private HintManager hintManager;
    

    
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
    }

    void Update()
    {
        // gameData가 생성된 상태이고 게임 상태가 Playing일 때만 작동
        if (gameData != null && gameData.GameState == GameState.Playing)
        {
            // 현재 남은 시간 계산 (제한시간 - 경과시간)
            float remainingTime = CurrentLevelConfig.TimeLimit - gameData.ElapsedTime;

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
    // ========== 초기화 ==========
    public void InitGame()
    {
        Time.timeScale = 1f;
        
        // TODO : SelectedLevelPanel에서 넘겨준 레벨 받아오기
        int selectedLevel = PlayerPrefs.GetInt("SelectedLevel", 1);
        LoadLevelConfig(selectedLevel);
        
        
        SetupNewGame();
        
        
        // 시간 체크 시작
        levelStartTime = Time.time;
        timeCheckCoroutine = StartCoroutine(CheckTimeOver());
        
        // UI 초기화
        RefreshUI();
        
        // ===== CapyDialogue 연결: 게임 시작 =====
        if(CapyDialogue != null && CapyDialogueText != null)
        {
            CapyDialogue.ShowDialogue(CapyDialogueText, DialogueType.Default);
        }
    }
    
    private void LoadLevelConfig(int levelIndex)
    {
        CurrentLevelConfig = Resources.Load<LevelConfig>($"LevelData/Level_{levelIndex}");
        if(CurrentLevelConfig == null)
        {
            Debug.LogError($"[GameManager] 레벨 {levelIndex} 설정 파일을 찾을 수 없습니다!");
        }
    }
    
  
    
    private void SetupNewGame()
{
    gameData = new GameData();
    gameData.CurrentLevelIndex = PlayerPrefs.GetInt("SelectedLevel", 1);
    gameData.CurrentBoxIndex = 0;
    gameData.GameState = GameState.Playing;
    gameData.StartTime = Time.time;
    gameData.ElapsedTime = 0f;
    gameData.UndoCount = 0;
    gameData.RefreshCount = 0;
    gameData.HintCount = 0;
    
    chunkData = ChunkGenerator.GenerateAllChunks(CurrentLevelConfig);

Debug.Log(GemCountStatusPanel);
    // ===== 수정: GemCountPanelManager 초기화 =====
    if(GemCountStatusPanel != null)
    {
        Debug.Log("[GameManager] GemCountStatusPanel 초기화 시작");
        GemCountStatusPanel.InitLevelGemStatus(
            chunkData.TotalRemainingGems, 
            CurrentLevelConfig.GemTypeCount
        );
    }
    
    gameData.Boxes = new List<Box>(chunkData.AllBoxes);
    gameData.BundlePool = new List<GemBundle>(chunkData.MergedBundlePool);
    gameData.RemainingGems = new Dictionary<GemType, int>(chunkData.TotalRemainingGems);
    
    ExtractDisplayBundles();
    
    Debug.Log($"[GameManager] 새 게임 시작. 레벨: {gameData.CurrentLevelIndex}");
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
            GetCurrentBox().RequiredAmount
        );
        
        if (GemCountStatusPanel != null)
        {
            GemCountStatusPanel.UpdateGemCount(bundle.GemType, gameData.RemainingGems[bundle.GemType]);
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
        
        GemBundle newBundle = GetRandomFromRemainingPool();
        gameData.CurrentDisplayBundles[gridIndex] = newBundle;
        
        gameData.RemainingGems[bundle.GemType] -= bundle.GemCount;
        
        if (GemCountStatusPanel != null)
        {
            GemCountStatusPanel.UpdateGemCount(bundle.GemType, gameData.RemainingGems[bundle.GemType]);
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
    yield return new WaitForSeconds(0.5f);
    
    // 선택 패널 업데이트
    UIManager.SelectionPanel.UpdateUI(gameData.SelectedBundles);
    
    // 상자 진행도 업데이트
    UIManager.UpdateBoxUI(
        gameData.CurrentBoxIndex,
        CalculateSelectedTotal(),
        GetCurrentBox().RequiredAmount
    );
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
        UIManager.UpdateBoxUI(gameData.CurrentBoxIndex, 0, GetCurrentBox().RequiredAmount);
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
            GemCountStatusPanel.UpdateGemCount(bundle.GemType, gameData.RemainingGems[bundle.GemType]);
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
    UIManager.UpdateBoxUI(gameData.CurrentBoxIndex, 0, GetCurrentBox().RequiredAmount);
    GridManager.ClearAllSelections();
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
            HandleFailure(); // 실패 처리 함수 호출
            return;
        }
        
        // 2. 종류 검증 실패 (모든 종류 1개 이상)
        if(!ValidateGemTypes())
        {
            HandleFailure(); // 실패 처리 함수 호출
            return;
        }
        
        // 3. 처리 (성공)
        ProcessBoxCompletion();
    }

    // [추가됨] 실패 시 공통 처리 로직
    private void HandleFailure()
    {
        // 연속 성공 카운트 초기화
        consecutiveSuccessCount = 0; 

        // ===== CapyDialogue 연결: 검증 실패 =====
        // 경고 메시지를 띄우지만, 내부적으로 연속 성공은 깨짐
        ShowWarning(null); 
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
    
 // GameManager.cs 내부

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
            CapyDialogue.RestartDefault(CapyDialogueText, 2.5f);
        }
        else
        {
            CapyDialogue.ShowDialogue(CapyDialogueText, DialogueType.Default);
            CapyDialogue.RestartDefault(CapyDialogueText, 2.5f);
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
                UIManager.UpdateBoxUI(gameData.CurrentBoxIndex, 0, nextBox.RequiredAmount);
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
        GameOverPopup popup = popupObj.GetComponent<GameOverPopup>();
        SoundManager.Instance.PlayFX(SoundType.GameOver);
        
        if(popup != null)
        {
            // 3. 팝업에도 위에서 결정한 finalMessage를 전달
            popup.Setup(
                finalMessage, 
                () => RestartLevel(), // 다시하기
                () => GoToMainHome()  // 메인으로
            );
        }
        else
        {
            // fallback
            Debug.LogError("[GameManager] GameOverPopup을 찾을 수 없습니다!");
        }
    }
private void HandleLevelClear()
{
    gameData.GameState = GameState.Win;
    StopCoroutine(timeCheckCoroutine);
    
    // ===== 추가: VideoPlayer + BGM 정지 =====
    StopBackgroundMedia();
    CapyDialogue.StopDialogue(CapyDialogueText);

    // 1. 시간 및 별 계산
    float clearTime = Time.time - levelStartTime + gameData.ElapsedTime;
    float maxTime = CurrentLevelConfig.TimeLimit;
    int starCount = 1;
    if (clearTime <= maxTime * 0.5f) starCount = 3;
    else if (clearTime <= maxTime * 0.66f) starCount = 2;

    string clearMessage = GetClearMessage(clearTime);

    // 2. 데이터 로드 및 업데이트
    ProgressData progressData = SaveManager.LoadData<ProgressData>("ProgressData");

    // ✅ 별 개수 갱신 (기존 기록보다 좋으면 업데이트)
    int currentLevel = gameData.CurrentLevelIndex;
     progressData.SetStars(currentLevel, starCount);

    // 레벨 해금
    if (progressData.LastClearedLevel < currentLevel)
    {
        progressData.LastClearedLevel = currentLevel;
    }
    
    SaveManager.Save(progressData, "ProgressData");
    
    SoundManager.Instance.PlayFX(SoundType.GameClear);

    // 3. 레벨별 분기 처리
    if (gameData.CurrentLevelIndex == 4)
    {
        int clearTimeMs = Mathf.RoundToInt(clearTime * 1000);

        if (!progressData.isLevel4Completed)
        {
            // 최초 클리어
            progressData.isLevel4Completed = true;
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
            () => GoToNextLevel(), // ✅ 다음 레벨
            () => RestartLevel(),  // ✅ 다시하기
            () => GoToMainHome()   // ✅ 메인홈
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

    // 3. 다음 레벨 설정 파일이 Resources 폴더에 있는지 확인
    // 파일 경로 예시: Resources/LevelData/Level_2.asset
    LevelConfig nextConfig = Resources.Load<LevelConfig>($"LevelData/Level_{nextLevel}");

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
        Debug.Log("[GameManager] VideoPlayer 정지");
    }
    
    // BGM 정지
    if(SoundManager.Instance != null)
    {
        SoundManager.Instance.StopBGM();
    }
}

  private string GetClearMessage(float clearTime)
    {
        // 1. 현재 레벨의 총 제한시간 가져오기
        float maxTime = CurrentLevelConfig.TimeLimit;
        
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
            Debug.LogError("[GameManager] EndingPrefab이 할당되지 않았습니다!");
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
        float timeLimit = CurrentLevelConfig.TimeLimit;
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
    
    private void HandleTimeOver()
{
    Debug.Log("[HandleTimeOver] 1. 시작");
    
    gameData.GameState = GameState.TimeOver;
    Debug.Log("[HandleTimeOver] 2. GameState 변경 완료");
     
    // ===== 추가: VideoPlayer + BGM 정지 =====
    StopBackgroundMedia();
        CapyDialogue.StopDialogue(CapyDialogueText);
    
    string randomMsg = CapyDialogue.GetRandomMessage(DialogueType.TimeOverGameOver);
   
    // PopupParentSetHelper 사용하는 경우
    if(PopupParentSetHelper.Instance == null)
    {
        return;
    }
    
    GameObject popupObj = PopupParentSetHelper.Instance.CreatePopup("Prefabs/GameOverPopup");
    
    if(popupObj == null)
    {
        Debug.LogError("[HandleTimeOver] popupObj 생성 실패!");
        return;
    }
   
    GameOverPopup popup = popupObj.GetComponent<GameOverPopup>();
    
    if(popup == null)
    {
        Debug.LogError("[HandleTimeOver] BaseConfirmationPopup 컴포넌트를 찾을 수 없습니다!");
        return;
    }
     SoundManager.Instance.PlayFX(SoundType.GameOver);
    popup.Setup(
        randomMsg,
        () => RestartLevel(),
        () => GoToMainHome()
    );
    
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
            GemCountStatusPanel.UpdateGemCount(bundle.GemType, gameData.RemainingGems[bundle.GemType]);
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
    // Assets/Scripts/Manager/GameManager.cs

public void ProcessHint()
{
    // 게임당 1회 제한
    if(gameData.HintCount >= CurrentLevelConfig.MaxHintCount)
    {
        ShowAdConfirmationPopup(() =>
        {
            AdManager.Instance.ShowRewardedAd((success) =>
            {
                if(success)
                {
                    
                   StartCoroutine(ExecuteHintWithLoading());
                }
            });
        },
        null);
    }
    else
    {
       
       StartCoroutine(ExecuteHintWithLoading());
    }
}


// ✅ 새 메서드: 로딩 UI 포함 힌트 실행
// ✅ ExecuteHintWithLoading() 수정 - HintManager 사용
private IEnumerator ExecuteHintWithLoading()
{
    // 1. 로딩 UI 표시
    if(HintLoadingUI != null)
    {
        HintLoadingUI.SetActive(true);
    }
    
    // 2. 선택 강제 비우기
    if(gameData.SelectedBundles.Count > 0)
    {
        CancelSelection();
        yield return new WaitForSeconds(0.6f);
    }
    
    // 3. 한 프레임 대기 (백트래킹 시간 확보)
    yield return null;
    
    // 4. ✅ HintManager에 위임
    List<GemBundle> hintBundles = hintManager.FindHintCombination(
        GetCurrentBox(),
        gameData.BundlePool,
        gameData.CurrentDisplayBundles,
        gameData.Boxes.Count - gameData.CurrentBoxIndex,
        CurrentLevelConfig.GemTypeCount
    );
    
    // 5. 로딩 UI 숨김
    if(HintLoadingUI != null)
    {
        HintLoadingUI.SetActive(false);
    }
    
    // 6. 결과 처리
    if(hintBundles != null && hintBundles.Count > 0)
    {
        gameData.HintCount++;
        GridManager.ShakeBundles(hintBundles);
        ShowTopNotification("힌트를 확인하세요카피!");
    }
    else
    {
        // 모든 전략 실패 → 이미 글렀음
        if(CapyDialogue != null && CapyDialogueText != null)
        {
            CapyDialogue.ShowDialogue(CapyDialogueText, DialogueType.AlreadyFailed);
        }
    }
    
    UpdateAllItemUI();
}


// Assets/Scripts/Manager/GameManager.cs

private void ExecuteHint()
{
    // 1단계: 글렀는지 빠른 판정
    if(!CheckIfSolvable())
    {
        if(CapyDialogue != null && CapyDialogueText != null)
        {
            CapyDialogue.ShowDialogue(CapyDialogueText, DialogueType.AlreadyFailed);
        }
        return;
    }
    
    // 2~3단계: 힌트 조합 찾기
    List<GemBundle> hintBundles = FindHintCombination();
    
    if(hintBundles != null && hintBundles.Count > 0)
    {
        gameData.HintCount++;
        // 힌트 표시 (흔들림)
        GridManager.ShakeBundles(hintBundles);
        
        ShowTopNotification("힌트를 확인하세요카피!");
            
    }
    else
    {
        // 조합 실패 (현재 화면에서 불가능)
        ShowWarning("현재 화면에서 조합을 찾을 수 없습니다카피! 새로고침을 추천합니다카피!");
    }
    
    UpdateAllItemUI();
}

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
        Debug.LogError("[GameManager] PopupParentSetHelper가 없습니다!");
        return;
    }
    
    GameObject popupObj = PopupParentSetHelper.Instance.CreatePopup("Prefabs/BaseConfirmationPopup");
    
    if(popupObj == null)
    {
        Debug.LogError("[GameManager] BaseConfirmationPopup 생성 실패!");
        return;
    }
    
    BaseConfirmationPopup popup = popupObj.GetComponent<BaseConfirmationPopup>();
    
    if(popup == null)
    {
        Debug.LogError("[GameManager] BaseConfirmationPopup 컴포넌트를 찾을 수 없습니다!");
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
    int refreshLeft = Mathf.Max(0, CurrentLevelConfig.MaxRefreshCount - gameData.RefreshCount);
    int hintLeft = Mathf.Max(0, CurrentLevelConfig.MaxHintCount - gameData.HintCount);
    
     // ✅ 수정: 최대 횟수도 함께 전달
    UIManager.UpdateHintAndItemUI(
        hintLeft, CurrentLevelConfig.MaxHintCount,
        refreshLeft, CurrentLevelConfig.MaxRefreshCount,
        undoLeft, CurrentLevelConfig.MaxUndoCount
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
    
    public void RestartLevel()
    {
        Time.timeScale = 1f; // 시간 흐름 복구

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainHome()
    {
        Time.timeScale = 1f; // 시간 흐름 복구
 
        SceneManager.LoadScene("MainHome");
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
        GetCurrentBox().RequiredAmount
    );

    // ✅ UpdateAllItemUI() 호출로 통일
    UpdateAllItemUI();
}

    
    // ===== CapyDialogue 연결: 경고 메시지 =====
    private void ShowWarning(string message)
    {
        if(CapyDialogue != null && CapyDialogueText != null)
        {
            if (message == null)
            {
                CapyDialogue.ShowDialogue(CapyDialogueText, DialogueType.Warning);
                CapyDialogue.RestartDefault(CapyDialogueText, 2.5f);
            }
            else
            {
                CapyDialogue.ShowDialogue(CapyDialogueText, message, false);
                CapyDialogue.RestartDefault(CapyDialogueText, 2.5f);
            }
    
        }
        
        Debug.LogWarning($"[GameManager] {message}");
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
        // TODO: 상단 알림창 구현
        // 임시로 CapyDialogue 활용
        if(CapyDialogue != null && CapyDialogueText != null)
        {
            CapyDialogue.ShowDialogue(CapyDialogueText, message, false);
                  CapyDialogue.RestartDefault(CapyDialogueText, 2.5f);
        }
        
        Debug.Log($"[GameManager] Notification: {message}");
    }
}
