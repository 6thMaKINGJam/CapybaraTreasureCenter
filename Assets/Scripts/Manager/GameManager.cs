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



    
    void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            // Firebase 초기화 로직
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
    
    // ===== 추가: 보석 개수 차감 및 UI 업데이트 =====
int beforeCount = gameData.RemainingGems[bundle.GemType]; // 차감 전
// gameData.RemainingGems[bundle.GemType] -= bundle.GemCount; // 차감 실행
int afterCount = gameData.RemainingGems[bundle.GemType];  // 차감 후

Debug.Log($"[데이터 체크] 타입: {bundle.GemType} | 빼기 전: {beforeCount} | 뺄 개수: {bundle.GemCount} | 뺀 후: {afterCount}");
    
    
    
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
        // 취소해서 다시 돌려주기
        gameData.RemainingGems[bundle.GemType] += bundle.GemCount;
        
        // 원래 인덱스 가져오기
        if(!selectedBundleOriginalIndices.ContainsKey(bundle))
        {
            Debug.LogError($"[OnBundleClicked] {bundle.BundleID}의 원래 인덱스를 찾을 수 없습니다!");
            return;
        }
        
        int originalIndex = selectedBundleOriginalIndices[bundle];
        selectedBundleOriginalIndices.Remove(bundle);
        
        // BundlePool에 다시 추가
        if(!gameData.BundlePool.Contains(bundle))
        {
            gameData.BundlePool.Add(bundle);
        }
        
        // 현재 그 자리의 번들
        GemBundle currentBundle = gameData.CurrentDisplayBundles[originalIndex];
        
        // 현재 번들이 새로 생성된 거라면 BundlePool에 반환
        if(currentBundle != null && currentBundle != bundle)
        {
            if(!gameData.BundlePool.Contains(currentBundle))
            {
                gameData.BundlePool.Add(currentBundle);
            }
        }
        
        // CurrentDisplayBundles 복원
        gameData.CurrentDisplayBundles[originalIndex] = bundle;
        
        // Grid에서 복원 (SiblingIndex 유지)
        GridManager.ReplaceBundleAtIndex(
            originalIndex,
            bundle,
            OnBundleClicked,
            isRestoring: true
        );
    }
    // ===== 선택 =====
    else
    {
        // 보석 개수 부족 시 선택 방지
        if(gameData.RemainingGems[bundle.GemType] < bundle.GemCount)
            {
                ShowWarning($"{bundle.GemType} 보석이 부족하다카피!");
                FlashRedScreen();

                 return;
            }
        
        gameData.SelectedBundles.Add(bundle);
        
        // 원래 인덱스 저장
        selectedBundleOriginalIndices[bundle] = gridIndex;
        
        // BundlePool에서 제거
        gameData.BundlePool.Remove(bundle);
        
        // 새 번들 결정
        GemBundle newBundle = GetRandomFromRemainingPool();
        
        // CurrentDisplayBundles 갱신
        gameData.CurrentDisplayBundles[gridIndex] = newBundle;
        
        // ===== 추가: 보석 개수 차감 및 UI 업데이트 =====
    gameData.RemainingGems[bundle.GemType] -= bundle.GemCount;
    
    if (GemCountStatusPanel != null)
    {
        GemCountStatusPanel.UpdateGemCount(bundle.GemType, gameData.RemainingGems[bundle.GemType]);
    }
    
    
        // Grid 애니메이션 교체 (SiblingIndex 유지)
        GridManager.ReplaceBundleAtIndex(
            gridIndex,
            newBundle,
            OnBundleClicked,
            isRestoring: false
        );
    }
    
    // UI 업데이트
    UIManager.SelectionPanel.UpdateUI(gameData.SelectedBundles);
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
    List<GemBundle> availableBundles = new List<GemBundle>(gameData.BundlePool);
    
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
    {
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
    
   private void ProcessBoxCompletion()
{
    foreach(var bundle in gameData.SelectedBundles)
    {
        // gameData.RemainingGems[bundle.GemType] -= bundle.GemCount;
        
        // 이미 선택 시 제거했으므로 Contains 체크
        if(gameData.BundlePool.Contains(bundle))
        {
            gameData.BundlePool.Remove(bundle);
        }
        
        if(gameData.CurrentDisplayBundles.Contains(bundle))
        {
            gameData.CurrentDisplayBundles.Remove(bundle);
        }
    }
    
    CompletedBox completedBox = new CompletedBox();
    completedBox.BoxIndex = gameData.CurrentBoxIndex;
    completedBox.UsedBundles = new List<GemBundle>(gameData.SelectedBundles);
    gameData.CompletedBoxes.Add(completedBox);
    
    gameData.CurrentBoxIndex++;
    
    // selectedBundleOriginalPrefabs 정리
    foreach(var bundle in gameData.SelectedBundles)
    {
        if(selectedBundleOriginalPrefabs.ContainsKey(bundle))
        {
            selectedBundleOriginalPrefabs.Remove(bundle);
        }
    }
    
    gameData.SelectedBundles.Clear();
    
    consecutiveSuccessCount++;

    if(CapyDialogue != null && CapyDialogueText != null)
    {
        if(consecutiveSuccessCount >= 3)
        {
            CapyDialogue.ShowDialogue(CapyDialogueText, DialogueType.ConsecutiveSuccess);
      CapyDialogue.RestartDefault(CapyDialogueText, 2.5f);
        }
        else
        {
            CapyDialogue.ShowDialogue(CapyDialogueText, DialogueType.Default);
        }
    }
    
    if(CheckGameOver())
    {
        HandleGameOver("특정 보석이 0개가 되어 더 이상 진행할 수 없습니다카피!");
        return;
    }
    
    if(gameData.CurrentBoxIndex >= gameData.Boxes.Count)
    {
        HandleLevelClear();
        return;
    }
    
   
    // 👈 인덱스가 증가한 직후, 리스트 크기와 비교해서 클리어인지 먼저 확인!
    if (gameData.CurrentBoxIndex >= gameData.Boxes.Count)
    {
        HandleLevelClear();
        return; // 클리어 시 함수 종료 (이후 UI 갱신 등 방지)
    }
    
    // ExtractDisplayBundles() 호출 안 함!
    RefreshUI();
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

        // 레벨 해금 정보 갱신 (공통)
        if (progressData.LastClearedLevel < gameData.CurrentLevelIndex)
        {
            progressData.LastClearedLevel = gameData.CurrentLevelIndex;
            Debug.Log($"[Clear] 다음 레벨 해금: {progressData.LastClearedLevel + 1}");
        }

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
                
                SaveManager.Save(progressData, "ProgressData"); // 👈 여기서 확실히 저장
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
                
                // 재클리어 시에는 엔딩 없이 메인으로 가거나 선택 (여기선 팝업 예시)
                ShowLevelClearPopup(starCount, clearMessage);
            }
        }
        else
        {
            // 레벨 1~3 클리어: 반드시 저장 후 팝업
            SaveManager.Save(progressData, "ProgressData"); // 👈 메인 해금을 위해 필수!
            ShowLevelClearPopup(starCount, clearMessage);
        }
    }
// ========== VideoPlayer + BGM 제어 헬퍼 ==========

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
// 팝업 생성 로직을 별도 함수로 빼면 중복 코드가 줄어듭니다.
private void ShowLevelClearPopup(int starCount, string message)
{
    GameObject popupObj = PopupParentSetHelper.Instance.CreatePopup("Prefabs/LevelClearPopup");
    LevelClearPopup popup = popupObj.GetComponent<LevelClearPopup>();
    if (popup != null)
    {
        popup.Setup(
            starCount, 
            message,
            () => GoToNextLevel(), // 👈 RestartLevel 대신 다음 레벨 이동 함수 연결 권장
            () => GoToMainHome()
        );
    }
}
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
            return $"대단하다카피! 소요시간: {clearTime:F1}초\n(제한시간의 절반도 안 썼어카피!)";
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
                    CapyDialogue.RestartDefault(CapyDialogueText, 2.5f);
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
    
    if(gameData.UndoCount > 2)
    {
        // [수정됨] 확인 팝업 먼저 표시
        ShowAdConfirmationPopup(() =>
        {
            // Yes 클릭 시에만 광고 호출
            AdManager.Instance.ShowRewardedAd((success) =>
            {
                if(success) ExecuteUndo();
                // ✅ 실패해도 Count 원복 안 함 (누적 카운터이므로)
            });
        }, 
        null); // ✅ No 버튼도 Count 원복 안 함
    }
    else
    {
        ExecuteUndo();
    }
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
    
    if(gameData.RefreshCount > 2)
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
    
    public void ProcessHint()
{
    string today = System.DateTime.Now.ToString("yyyy-MM-dd");
    string lastHintDate = PlayerPrefs.GetString("LastHintDate", "");
    
    if(lastHintDate == today)
    {
        // [수정됨] 확인 팝업 먼저 표시
        ShowAdConfirmationPopup(() =>
        {
            AdManager.Instance.ShowRewardedAd((success) =>
            {
                if(success) ExecuteHint();
                // ✅ 날짜는 광고 성공 시에만 갱신 (아래에서 처리)
            });
        },
        null);
    }
    else
    {
        ExecuteHint();
        PlayerPrefs.SetString("LastHintDate", today); // 무료 사용 시 날짜 갱신
    }
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
    
    private void ExecuteHint()
    {
        Box currentBox = GetCurrentBox();
        List<GemBundle> hintBundles = FindHintCombination(currentBox);
        
        if(hintBundles == null || hintBundles.Count == 0)
        {
            ShowWarning("현재 화면에서 조합을 찾을 수 없습니다카피! 새로고침이 필요카피");
            return;
        }
        UpdateAllItemUI();
        
    }

    private void UpdateAllItemUI()
{
    UIManager.UpdateItemUI(
        PlayerPrefs.GetInt("HintUsedToday", 0),
        gameData.RefreshCount,
        gameData.UndoCount
    );
}
    
    private List<GemBundle> FindHintCombination(Box targetBox)
    {
        List<GemBundle> result = new List<GemBundle>();
        Dictionary<GemType, int> needed = new Dictionary<GemType, int>();
        
        for(int i = 0; i < CurrentLevelConfig.GemTypeCount; i++)
        {
            needed[(GemType)i] = 1;
        }
        
        int totalNeeded = targetBox.RequiredAmount;
        int totalGathered = CurrentLevelConfig.GemTypeCount;
        
        // 1단계: 각 종류 1개씩
        foreach(var bundle in gameData.CurrentDisplayBundles)
        {
            if(needed[bundle.GemType] > 0)
            {
                result.Add(bundle);
                needed[bundle.GemType] = 0;
            }
        }
        
        // 2단계: 남은 개수 채우기
        foreach(var bundle in gameData.CurrentDisplayBundles)
        {
            if(result.Contains(bundle)) continue;
            
            if(totalGathered + bundle.GemCount <= totalNeeded)
            {
                result.Add(bundle);
                totalGathered += bundle.GemCount;
                
                if(totalGathered == totalNeeded) break;
            }
        }
        
        return result;
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

        // 아이템 남은 횟수 UI 업데이트
        UIManager.UpdateItemCounts(
            PlayerPrefs.GetInt("HintUsedToday", 0), 
            gameData.RefreshCount,
            gameData.UndoCount
        );
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
        // 빨간색 반투명으로 설정
        FlashOverlay.color = new Color(1f, 0f, 0f, 0.5f);
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
        }
        
        Debug.Log($"[GameManager] Notification: {message}");
    }
}
