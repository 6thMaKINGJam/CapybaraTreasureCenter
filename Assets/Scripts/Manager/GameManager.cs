using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.UI;

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
    
    [Header("카피바라 대사 시스템")]
    public CapyDialogue CapyDialogue;
    public TextMeshProUGUI CapyDialogueText; // 대사 표시할 UI Text
    
    [Header("효과")]
    public Image FlashOverlay; // 빨간 화면 깜박임용 Image (전체 화면 크기)
    
    // 게임 데이터
    private GameData gameData;
    private ChunkData chunkData;
    
    // 시간 관련
    private float levelStartTime;
    private Coroutine timeCheckCoroutine;
    
    // 연속 성공 카운트
    private int consecutiveSuccessCount = 0;
    
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
        InitGame();
    }
    
    // ========== 초기화 ==========
    public void InitGame()
    {
        Time.timeScale = 1f;
        
        // 선택된 레벨 로드
        int selectedLevel = PlayerPrefs.GetInt("SelectedLevel", 1);
        LoadLevelConfig(selectedLevel);
        
        // 이어하기 확인
        if(SaveManager.HasSaveData("GameData"))
        {
            LoadGameData();
        }
        else
        {
            SetupNewGame();
        }
        
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
        CurrentLevelConfig = Resources.Load<LevelConfig>($"Levels/Level_{levelIndex:00}");
        if(CurrentLevelConfig == null)
        {
            Debug.LogError($"[GameManager] 레벨 {levelIndex} 설정 파일을 찾을 수 없습니다!");
        }
    }
    
    private void LoadGameData()
    {
        gameData = SaveManager.LoadData<GameData>("GameData");
        Debug.Log("[GameManager] 이어하기 데이터 로드 완료");
        
        chunkData = ChunkGenerator.GenerateAllChunks(CurrentLevelConfig);
        
        foreach(var completedBox in gameData.CompletedBoxes)
        {
            foreach(var usedBundle in completedBox.UsedBundles)
            {
                gameData.BundlePool.RemoveAll(b => b.BundleID == usedBundle.BundleID);
            }
        }
        
        ExtractDisplayBundles();
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
        
        gameData.Boxes = new List<Box>(chunkData.AllBoxes);
        gameData.BundlePool = new List<GemBundle>(chunkData.MergedBundlePool);
        gameData.RemainingGems = new Dictionary<GemType, int>(chunkData.TotalRemainingGems);
        
        ExtractDisplayBundles();
        
        Debug.Log($"[GameManager] 새 게임 시작. 레벨: {gameData.CurrentLevelIndex}, 상자: {gameData.Boxes.Count}개");
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
    
    // ========== 묶음 선택/취소 ==========
    private void OnBundleClicked(GemBundlePrefab clickedPrefab)
    {
        GemBundle bundle = clickedPrefab.GetData();
        
        if(gameData.SelectedBundles.Contains(bundle))
        {
            gameData.SelectedBundles.Remove(bundle);
            clickedPrefab.SetSelected(false);
        }
        else
        {
            gameData.SelectedBundles.Add(bundle);
            clickedPrefab.SetSelected(true);
        }
        
        UIManager.SelectionPanel.UpdateUI(gameData.SelectedBundles);
        UIManager.UpdateBoxUI(
            gameData.CurrentBoxIndex,
            CalculateSelectedTotal(),
            GetCurrentBox().RequiredAmount
        );
    }
    
    public void CancelSelection()
    {
        gameData.SelectedBundles.Clear();
        GridManager.ClearAllSelections();
        
        UIManager.SelectionPanel.UpdateUI(gameData.SelectedBundles);
        UIManager.UpdateBoxUI(
            gameData.CurrentBoxIndex,
            0,
            GetCurrentBox().RequiredAmount
        );
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
        // 보석 차감
        foreach(var bundle in gameData.SelectedBundles)
        {
            gameData.RemainingGems[bundle.GemType] -= bundle.GemCount;
            gameData.BundlePool.Remove(bundle);
            gameData.CurrentDisplayBundles.Remove(bundle);
        }
        
        // 완료 기록
        CompletedBox completedBox = new CompletedBox();
        completedBox.BoxIndex = gameData.CurrentBoxIndex;
        completedBox.UsedBundles = new List<GemBundle>(gameData.SelectedBundles);
        gameData.CompletedBoxes.Add(completedBox);
        
        // 상자 진행
        gameData.CurrentBoxIndex++;
        gameData.SelectedBundles.Clear();
        
        // ===== [수정됨] CapyDialogue 연결: 상자 완료 =====
        consecutiveSuccessCount++; // 성공 횟수 증가

        if(CapyDialogue != null && CapyDialogueText != null)
        {
            // 3번 이상이면 계속 ConsecutiveSuccess 유지 (리셋 안 함)
            if(consecutiveSuccessCount >= 3)
            {
                CapyDialogue.ShowDialogue(CapyDialogueText, DialogueType.ConsecutiveSuccess);
            }
            else
            {
                // 1번, 2번 성공일 때는 그냥 디폴트 루트 
                CapyDialogue.ShowDialogue(CapyDialogueText, DialogueType.Default);
            }
        }
        
        // 게임오버 체크
        if(CheckGameOver())
        {
            HandleGameOver("특정 보석이 0개가 되어 더 이상 진행할 수 없습니다카피!");
            return;
        }
        
        // 레벨 클리어 체크
        if(gameData.CurrentBoxIndex >= gameData.Boxes.Count)
        {
            HandleLevelClear();
            return;
        }
        
        // 저장
        SaveManager.Save(gameData, "GameData");
        
        // 화면 갱신
        ExtractDisplayBundles();
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
        GameObject popupObj = Instantiate(Resources.Load<GameObject>("Prefabs/UI/GameOverPopup"));
        GameOverPopup popup = popupObj.GetComponent<GameOverPopup>();
        
        if(popup != null)
        {
            // 3. 팝업에도 위에서 결정한 finalMessage를 전달
            popup.Setup(
                finalMessage, 
                () => RestartLevel(), // 다시하기
                () => ExecuteUndo(),  // 되돌리기
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
        
        // 1. 클리어 시간 및 별 계산
        float clearTime = Time.time - levelStartTime + gameData.ElapsedTime;
        float maxTime = CurrentLevelConfig.TimeLimit;
        
        int starCount = 1; // 기본 1개
        if (clearTime <= maxTime * 0.5f) starCount = 3;      // 50% 이하 시간: 별 3개
        else if (clearTime <= maxTime * (2f/3f)) starCount = 2; // 66% 이하 시간: 별 2개
        
        // 2. 메시지 생성
        string clearMessage = GetClearMessage(clearTime);


        // ProgressData 업데이트
        ProgressData progressData = SaveManager.LoadData<ProgressData>("ProgressData");
        
        if(progressData.LastClearedLevel < gameData.CurrentLevelIndex)
        {
            progressData.LastClearedLevel = gameData.CurrentLevelIndex;
        }
        
        
        // 레벨 4 클리어 시
        if(gameData.CurrentLevelIndex == 4 && !progressData.isLevel4Completed)
        {
            int clearTimeMs = Mathf.RoundToInt(clearTime * 1000);
           
            progressData.isLevel4Completed = true;
            if(progressData.BestTime == 0 || clearTimeMs < progressData.BestTime)
            {
                progressData.BestTime = clearTimeMs;
            }
            
            SaveManager.Save(progressData, "ProgressData");
            SaveManager.DeleteSave("GameData");
            
            
                TriggerEnding();
                return;
          
        }
        else
        {
            // 레벨 1~3 클리어
           GameObject popupObj = Instantiate(Resources.Load<GameObject>("Prefabs/UI/LevelClearPopup"));
        LevelClearPopup popup = popupObj.GetComponent<LevelClearPopup>();
        
        if (popup != null)
        {
            popup.Setup(
                starCount, 
                clearMessage,
               () => RestartLevel(), // 첫 번째 버튼(Retry)에 재시작 기능 연결!
                () => GoToMainHome()  // 두 번째 버튼(Home)에 홈 이동 연결
                );
        }
        else
        {
            Debug.LogError("[GameManager] LevelClearPopup을 찾을 수 없습니다! 경로를 확인하세요.");
        }
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
        
        GameObject endingObj = Instantiate(EndingPrefab);
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
        
        while(true)
        {
            if(gameData.GameState == GameState.Playing)
            {
                float elapsed = Time.time - levelStartTime + gameData.ElapsedTime;
                float remaining = timeLimit - elapsed;
                
                UIManager.TimerSlider.value = remaining / timeLimit;
                
                // ===== CapyDialogue 연결: 시간 부족 경고 =====
                if(!lowTimeWarningShown && remaining <= 30f && remaining > 0f)
                {
                    if(CapyDialogue != null && CapyDialogueText != null)
                    {
                        CapyDialogue.ShowDialogue(CapyDialogueText, DialogueType.TimeLowWarning);
                    }
                    lowTimeWarningShown = true;
                }
                
                if(remaining <= 0)
                {
                    HandleTimeOver();
                    yield break;
                }
            }
            
            yield return null;
        }
    }
    
    private void HandleTimeOver()
    {
        gameData.GameState = GameState.TimeOver;
        
        
        string randomMsg = CapyDialogue.GetRandomMessage(DialogueType.TimeOverGameOver);
        
        GameObject popupObj = Instantiate(Resources.Load<GameObject>("Prefabs/UI/BaseConfirmationPopup"));
        BaseConfirmationPopup popup = popupObj.GetComponent<BaseConfirmationPopup>();
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
        
        gameData.UndoCount++;
        
        if(gameData.UndoCount > 2)
        {
            AdManager.Instance.ShowRewardedAd((success) =>
            {
                if(success) ExecuteUndo();
            });
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
        }
        
        gameData.CurrentBoxIndex--;
        gameData.SelectedBundles.Clear();
        
        // 연속 성공 카운트 리셋
        consecutiveSuccessCount = 0;
        
        SaveManager.Save(gameData, "GameData");
        
        ExtractDisplayBundles();
        RefreshUI();
        
        ShowTopNotification("이전 상태로 되돌아갔습니다카피!");
    }
    
    public void ProcessRefresh()
    {
        gameData.RefreshCount++;
        
        if(gameData.RefreshCount > 2)
        {
            AdManager.Instance.ShowRewardedAd((success) =>
            {
                if(success) ExecuteRefresh();
            });
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
            AdManager.Instance.ShowRewardedAd((success) =>
            {
                if(success) ExecuteHint();
            });
        }
        else
        {
            ExecuteHint();
            PlayerPrefs.SetString("LastHintDate", today);
        }
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
        
        GridManager.HighlightBundles(hintBundles, 3f);
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
            gameData.ElapsedTime += Time.time - levelStartTime;
            Time.timeScale = 0f;
            UIManager.PausePopupPanel.SetActive(true);
        }
    }
    
    public void Resume()
    {
        gameData.GameState = GameState.Playing;
        levelStartTime = Time.time;
        Time.timeScale = 1f;
        UIManager.PausePopupPanel.SetActive(false);
    }
    
    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SaveManager.DeleteSave("GameData");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    public void GoToMainHome()
    {
        Time.timeScale = 1f;
        SaveManager.DeleteSave("GameData");
        SceneManager.LoadScene("MainHome");
    }
    
    // ========== 유틸리티 ==========
    private Box GetCurrentBox()
    {
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
        UIManager.UpdateTotalGemUI(gameData.RemainingGems);
        UIManager.UpdateBoxUI(
            gameData.CurrentBoxIndex,
            CalculateSelectedTotal(),
            GetCurrentBox().RequiredAmount
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
            }
            else
            {
                CapyDialogue.ShowDialogue(CapyDialogueText, message, false);
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