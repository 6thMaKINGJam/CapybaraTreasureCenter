using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine.SceneManagement;
using System.Diagnostics;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("게임 상태")]
    public GameState CurrentState; 
    private float LevelStartTime; 
    private float CurrentLevelLimitTime = 300f;  

    [Header("레벨 세팅")]
    public int CurrentLevelIndex = 0; // 현재 레벨 인덱스
    public int CurrentBoxIndex = 0;   // 현재 상자 인덱스
    public int TargetCapacity = 100;  // 현재 상자의 목표 용량
    public int CurrentCapacity = 0;   // 현재 상자에 담긴 양

    [Header("참조")]
    public ChunkGenerator chunkGenerator;
    public GameObject EndingPrefab;

    [Header("데이터 리스트")]
    public List<GameObject> RemainingGems = new List<GameObject>();
    public List<GameObject> CurrentDisplayBundles = new List<GameObject>();
    private List<GameObject> SelectedInCurrentBox = new List<GameObject>();
    private Stack<List<GameObject>> CompletedBoxesHistory = new Stack<List<GameObject>>();

    [Header("아이템 카운트")]
    public int UndoCount = 0;
    public int RefreshCount = 0;
    public int HintCount = 0;

    [Header("팝업 프리팹")]
    public GameObject ConfirmationPopupPrefab;
    public GameObject WarningPopupPrefab;

    public void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        InitGame();
    }

    // 1. A: GameManager 초기화
    public void InitGame()
    {
        CurrentState = GameState.Ready;
        Debug.Log($"게임 준비.");

        CurrentLevelIndex = PlayerPrefs.GetInt("SelectedLevel", 1);

        if (SaveManager.HasSaveData("GameState"))
        {
            LoadGameState();
        }
        else
        {
            SetupNewGame();
        }

        LevelStartTime = Time.time;
        CurrentState = GameState.Playing;
        StartCoroutine(CheckTimeOverLimit());
    }

    private void SetupNewGame()
    {
        CurrentBoxIndex = 0;
        if(chunkGenerator != null)
            chunkGenerator.GenerateChunk(null, CurrentLevelIndex);

        SetupLevelData();
    }

    // 2. B: 청크 로드 및 묶음 표시
    public void SetupLevelData()
    {
        RemainingGems = GameObject.FindGameObjectsWithTag("Gem").ToList();
        Debug.Log($"청크 로드 완료. 보석 {RemainingGems.Count}개 감지.");
        ExtractNextBundles();
    }

    public void ExtractNextBundles()
    {
        CurrentDisplayBundles = RemainingGems.Take(12).ToList();
        Debug.Log($"하단 그리드용 보석 12개 추출 완료.");
    }

    // 3. C: 선택 취소
    public void CancelSelection()
    {
        SelectedInCurrentBox.Clear();
        CurrentCapacity = 0;
        Debug.Log($"선택 취소. 상자를 비움.");
    }

    // 4. D: 완료 버튼 로직
    public void OnClickComplete()
    {
        if(CurrentCapacity != TargetCapacity)
        {
            Debug.LogWarning("실패: 상자 용량 초과 또는 미달.");
            HandleFailureFeedback();
            return;
        }

        CompletedBoxesHistory.Push(new List<GameObject>(SelectedInCurrentBox));

        foreach (var gem in SelectedInCurrentBox)
        {
            RemainingGems.Remove(gem);
        }

        SelectedInCurrentBox.Clear();
        CurrentBoxIndex++;

        // ! SaveManager.Save("GameState", this);
        Debug.Log($"{CurrentBoxIndex}번째 상자 완료.");

        CheckLevelProgress();
    }

    private void HandleFailureFeedback()
    {
        // 피드백 로직 (진동, 반짝임 등)
        Debug.Log("용량 불일치 피드백 실행");
    }

    public void CheckLevelProgress()
    {
        if(RemainingGems.Count <= 0)
        {
            HandleLevelClear();
        }
        else
        {
            CurrentCapacity = 0;
            ExtractNextBundles();
        }
    }

    // 5. E: 레벨 완료 / 게임 오버 처리
    public void HandleLevelClear()
    {
        CurrentState = GameState.Win;
        float timeSpent = Time.time - LevelStartTime;

        if(CurrentLevelIndex == 4)
        {
            TriggerEnding();
        }
        else
        {
            GameObject go = Instantiate(WarningPopupPrefab); // Instatiate -> Instantiate 오타 수정
            BaseWarningPopup popup = go.GetComponent<BaseWarningPopup>();
            popup.Setup($"클리어! 소요시간: {timeSpent:F1}초", () =>
            {
                SaveManager.DeleteSave("GameState");
                CurrentLevelIndex++;
                PlayerPrefs.SetInt("SelectedLevel", CurrentLevelIndex); // PlayersPrefs -> PlayerPrefs 수정
                InitGame();
            });
        }
    }

    private void TriggerEnding()
    {
        if(EndingPrefab == null) return;
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

    public void HandleGameOver()
    {
        CurrentState = GameState.GameOver;
        GameObject go = Instantiate(ConfirmationPopupPrefab);
        BaseConfirmationPopup popup = go.GetComponent<BaseConfirmationPopup>();
        popup.Setup("상자가 가득 찼습니다! 다시 시도?", 
            () => RestartLevel(), 
            () => GoToMain());
    }

    public void HandleTimeOver()
    {
        CurrentState = GameState.TimeOver; // TimeOVer -> TimeOver 오타 수정
        GameObject go = Instantiate(ConfirmationPopupPrefab);
        BaseConfirmationPopup popup = go.GetComponent<BaseConfirmationPopup>();
        popup.Setup("시간이 종료되었습니다! 다시 시도?", 
            () => RestartLevel(), 
            () => GoToMain());
    }

    // 일시 정지 함수
    public void TogglePause()
    {
        if(CurrentState == GameState.Playing)
        {
            CurrentState = GameState.Paused;
            // DeltaTime 기반 로직 정지
            Time.timeScale = 0f;
            Debug.Log($"게임 일시정지");
        }

        else if(CurrentState == GameState.Paused)
        {
            CurrentState = LoadGameState.Playing;
            Time.timeScale = 1f; // 게임 재개
            Debug.Log($"게임 재개");
        }
    }

    // 일시정지 시 시간 멈추도록 하는 코루틴
    private float RemainingTime;
    
    private IEnumerator CheckTimeOverLimit()
    {
        RemainingTime = CurrentLevelLimitTime;

        while(RemainingTime > 0)
        {
            // Playing 상태일 때만 시간이 흐르게 함
            if(CurrentState == GameState.Playing)
            {
                RemainingTime -= Time.deltaTime;
            }

            if(RemainingTime <= 0)
            {
                HandleTimeOver();
                yield break;
            }

            yield return null;
        }
    }

    private void RestartLevel()
    {
        SaveManager.DeleteSave("GameState");
        InitGame();
    }

    private void GoToMain()
    {
        SaveManager.DeleteSave("GameState");
        SceneManager.LoadScene("MainHome");
    }

    public void ProcessUndo()
    {
        if(CompletedBoxesHistory.Count <= 0) return;

        List<GameObject> LastBoxGems = CompletedBoxesHistory.Pop();
        RemainingGems.InsertRange(0, LastBoxGems);
        CurrentBoxIndex--;
        CurrentCapacity = 0;
        UndoCount++;
        ExtractNextBundles();
    }

    public void ProcessRefresh()
    {
        System.Random Rng = new System.Random();
        RemainingGems = RemainingGems.OrderBy( a => Rng.Next()).ToList();
        RefreshCount++;
        ExtractNextBundles();
    }

    public void ProcessHint()
    {
        List<GameObject> FoundCombo = new List<GameObject>();
        int TempSum = 0;

        foreach (var gem in CurrentDisplayBundles)
        {
            if(gem == null) continue;
            
            Gem GemScript = gem.GetComponent<Gem>();
            if(GemScript != null)
            {
                int val = GemScript.Value;
                if(TempSum + val <= TargetCapacity)
                {
                    TempSum += val;
                    FoundCombo.Add(gem);
                }
            }
        }
        StartCoroutine(HighlightGems(FoundCombo));
        HintCount++;
    }

    private IEnumerator HighlightGems(List<GameObject> gems)
    {
        foreach(var g in gems) if(g != null) g.transform.localScale *= 1.2f;
        yield return new WaitForSeconds(3f);
        foreach (var g in gems) if(g != null) g.transform.localScale /= 1.2f;
    }

    private void LoadGameState()
    {
        Debug.Log("이어하기 데이터 로드 완료.");
    }
}