using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine.SceneManagement;
using System.Diagnostics; // 씬 전환용


// A. 선택 레벨 로드/이어하기/새로시작/
// ChunkGenerator 호출/State 초기화/UI 업데이트
// B. 청크 생성→remainingGems 초기화→12개 추출→BundleGridManager 전달
// C. 선택 묶음 전부 지우고, 이전 상자 클리어 직후 상태로 복구
// D. 검증→차감→오버 체크→CompletedBox 추가→index++→저장→청크/레벨 완료 체크
// E. 클리어 시 ProgressData 업데이트/저장파일삭제/
// 팝업/메인, 오버 시 팝업(다시/되돌리기/메인)

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("게임 상태")]
    // 현재 게임 상태
    public GameState CurrentState; 
    // 시간 체크
    private float LevelStartTime; 
    // 제한 시간 (아래 시간은 수정 가능)
    private float CurrentLevelLimitTime = 300f;  

    [Header("레벨 세팅")]
    public int CurrentLevelIndex = 0; // 현재 레벨 인덱스
    public int CurrentBoxIndex = 0; // 현재 상자 인덱스
    public int TargetCapacity = 100; // 현재 상자의 목표 용량
    public int CurrentCapacity = 0; // 현재 상자에 담긴 양

    [Header("참조")]
    public ChunkGenerator chunkGenerator;
    public GameObject EndingPrefab;
    // ! public GameUIManager gameUIManager;
    // ! public SaveManager saveManager;

    [Header("데이터 리스트")]
    // 청크에서 추출한 전체 보석 리스트
    public List<GameObject> RemainingGems = new List<GameObject>();
    // 하단 그리드에 표시될 12개 묶음
    public List<GameObject> CurrentDisplayBundles = new List<GameObject>();
    // 현재 상자에 담기 위해 선택한 보석들 (취소 기능 시험용)
    private List<GameObject> SelectedInCurrentBox = new List<GameObject>();

    [Header("데이터 리스트 확장")]
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
        // 게임 시작 시 초기화 호출
        InitGame();
    }

    // 1. GameManager 초기화
    public void InitGame()
    {
        // 상태 초기화
        CurrentState = GameState.Ready;
        Debug.Log($"게임 준비.");

        // PlayerPrefs로 선택 레벨 로드
        CurrentLevelIndex = PlayerPrefs.GetInt("SelectedLevel", 1);

        // 이어하기 체크
        if (SaveManager.HasSaveData("GameState"))
        {
            LoadGameState(); // 이어하기 로직
        }
        else
        {
            SetupNewGame(); // 새로하기 로직
        }

        LevelStartTime = Time.time; // 시간 체크 시작
        CurrentState = GameState.Playing;

        // 타임 오버를 감시하는 코루틴 시작
        StartCoroutine(CheckTimeOverLimit());
    }

    private void SetupNewGame()
    {
        CurrentBoxIndex = 0;
        // 새로 시작 시 청크 생성 호출
        if(chunkGenerator != null)
            chunkGenerator.GenerateChunk(null, CurrentLevelIndex);

        SetupLevelData();
    }

    // 2. 청크 로드 및 묶음 표시
    public void SetupLevelData()
    {
        // Gem 태그를 가진 모든 오브젝트를 찾아 리스트화 (게임 전체 리스트)
        RemainingGems = GameObject.FindGameObjectsWithTag("Gem").ToList();
        Debug.Log($"청크 로드 완료. 보석 {RemainingGems.Count}개 감지.");
        
        ExtractNextBundles(); // UI에 보여줄 첫 12개 묶음 추

        // UI 업데이트
        // ! gameUIManager.UpdateAllUI();
    }

    public void ExtractNextBundles()
    {
        // RemainingGems의 앞에서부터 12개 추출
        CurrentDisplayBundles = RemainingGems.Take(12).ToList();

        // BundleGridManager가 완성되면 데이터 전달하여 UI 갱신
        Debug.Log($"하단 그리드용 보석 12개 추출 완료. BundleGridManager 전달 준비.");
    }

    // 3. 묶음 (담기) 취소
    public void CancelSelection()
    {
        // 현재 상자에 담았던 보석 리스트 비우기
        SelectedInCurrentBox.Clear();

        // 현재 상자 용량을 0으로 리셋
        CurrentCapacity = 0;

        // ! BundleGridManager 재구성 및 UI 업데이트 
        Debug.Log($"선택 취소. 상자를 비우고 이전 상태로 복구함.");
    }

    // 4. 완료 버튼 로직
    public void OnClickComplete()
    {
        Debug.Log($"유저가 완료 버튼을 누름. 상자 검증 시작.");

        // 오버 체크 (상자의 한도를 초과했는지 여부)
        if(CurrentCapacity != TargetCapacity)
        {
            Debug.LogWarning("실패: 상자 용량 초과.");
            HandleFailureFeedback(); // 게임 오버 로직으로 이동
            return; // 게임 오버 = 이후 로직 없이 종료
        }

        // 완료 직전 현재 선택된 보석들을 히스토리에 저장
        CompletedBoxesHistory.Push(new List<GameObject>(SelectedInCurrentBox));

        // 차감
        // 검증 통과 -> 현재 상자에 담았던 보석들을 게임 전체 리스트에서 제거
        foreach (var gem in SelectedInCurrentBox)
        {
            RemainingGems.Remove(gem);
        }

        // CompletedBox 추가 (데이터 관리)
        // ! CompletedBox = 리스트 따로 저장 / DB에 반영
        SelectedInCurrentBox.Clear();

        // index++ = 현재 상자 채우기에 성공 -> 다음 상자로 넘어감
        CurrentBoxIndex++;

        // 중간 저장
        // 상자 하나 완료될 때 데이터 저장 -> 강제 종료해도 이어하기 가능
        // ! SaveManager.Save("GameState", this);
        Debug.Log($"{CurrentBoxIndex}번째 상자 완료 및 데이터 저장.");

        // 청크/레벨 완료 체크 = 레벨 클리어인지 다음 상자를 줄지 여부
        CheckLevelProgress();
    }

    private void HandleFailureFeedback()
    {
        //‘capydialogue’ 타입 Warning 나오고 + 빨간색 반투명 화면 전부 덮는 크기로 반짝 한번 
        // + vibrationmanager.cs로 진동 한 번 
        // ! VibrationManager.Instance.Vibrate();
    }

    public void CheckLevelProgress()
    {
        // 레벨 완료 여부 체크 = RemainingGems 리스트가 비어있는지 여부
        if(RemainingGems.Count <= 0)
        {
            Debug.Log("모든 보석 처리 완료. 레벨 클리어.");
            HandleLevelClear(); // 레벨 완료 로직으로 이동
        }
        else
        {
            // 클리어 X -> 다음 상자 준비
            CurrentCapacity = 0; // 상자 용량 0으로 초기화
            ExtractNextBundles(); // 추출 로직 다시 실행 -> 그리드(12개) 채움
            Debug.Log($"다음 상자를 준비합니다. 남은 보석 수: {RemainingGems.Count}");
        }
    }

    // 5. 레벨 완료 / 게임 오버 / 타임오버 처리

    // 레벨 완료 (성공 판정)
    public void HandleLevelClear()
    {
        CurrentState = GameState.Win;
        float timeSpent = Time.time - LevelStartTime;
        Debug.Log("레벨 클리어.");

        if(CurrentLevelIndex == 4)
        {
            // ! ProgressData.Update(timeSpent);
            TriggerEnding();
        }
        else
        {
            GameObject go = Instatiate(WarningPopupPrefab);
            BaseWarningPopup popup = go.GetComponent<BaseWarningPopup>();
            popup.Setup($"클리어! 소요시간: {timeSpent:F1}초", () =>
            {
                SaveManager.DeleteSave("GameState");
                CurrentLevelIndex++;
                PlayersPrefs.SetInt("SelectedLevel", CurrentLevelIndex);
                InitGame();
            });
        }
        
        }
    


    // 엔딩
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
        // 랭킹창 오픈 예약해두고 메인으로 이동
        PlayerPrefs.SetInt("ShowRankingOnStart", 1);
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainHome");
    }

    // 게임 오버 (용량 초과로 실패 판정)
    public void HandleGameOver()
    {
        CurrentState = GameState.GameOver;
        Debug.Log($"용량 초과로 인한 게임 오버.");

        // 게임 오버 -> 다시하기/메인으로 중 선택
        GameObject go = Instantiate(ConfirmationPopupPrefab);
        BaseConfirmationPopup popup = go.GetComponent<BaseConfirmationPopup>();

        popup.Setup("상자가 가득 찼습니다! 다시 시도하시겠습니까?",
            YesCallback: () => RestartLevel(), // 현재 레벨 다시 시작
            NoCallback: () => GoToMain() // 메인 로비로 이동
        );
    }

    // 타임 오버 (시간 제한 종료로 실패 판정)
    public void HandleTimeOver()
    {
        // 게임 오버 -> 다시하기/메인으로 중 선택
        CurrentState = GameState.TimeOVer;
        GameObject go = Instantiate(ConfirmationPopupPrefab);
        BaseConfirmationPopup popup = go.GetComponent<BaseConfirmationPopup>();

        popup.Setup("시간이 종료되었습니다! 다시 시도하시겠습니까?",
            YesCallback: () => RestartLevel(), // 현재 레벨 다시 시작
            NoCallback: () => GoToMain() // 메인 로비로 이동
        );
    }

     // 타임 오버 감시 코루틴
    private IEnumerator CheckTimeOverLimit()
    {
        while (CurrentState == GameState.Playing)
        {
            if(Time.time - LevelStartTime >= CurrentLevelLimitTime)
            {
                HandleTimeOver();
                yield break;
            }
            yield return new WaitForSeconds(1f);
        }
    }

    // 보조 함수들
    private void RestartLevel()
    {
        // 레벨 처음부터 다시 시작
        Debug.Log($"레벨 다시 시작");
        SaveManager.DeleteSave("GameState");
        InitGame();
    }

    private void GoToMain()
    {
        // 메인 로비 씬으로 이동
        Debug.Log($"메인 메뉴로 이동");
        SaveManager.DeleteSave("GameState");
        SceneManager.LoadScene("MainHome");
    }

    // 되돌리기 (Undo)
    public void ProcessUndo()
    {
        if(CompletedBoxesHistory.Count <= 0)
        {
            Debug.Log($"복구할 기록이 없습니다.");
            return;
        }

        
        // 광고 체크
        if( UnodoCount >= 2)
        {
            Debug.Log("Undo 시 광고 실행");
            // ShowAd(UndoCallback);
        }

        // 복구 로직
        List<GameObject> LastBoxGems = CompletedBoxesHistory.Pop();
        // 보석들을 다시 남은 보석 리스트 맨 앞에 삽입
        RemainingGems.InsertRange(0, LastBoxGems);
        // 상자 인덱스 감소 및 현재 상태 리셋
        CurrentBoxIndex--;
        CurrentCapacity = 0;
        // UI 업데이트
        UndoCount++;
        ExtractNextBundles();
        Debug.Log($"Undo 완료. 보석 복구 및 인덱스 감소.");
    }

    // 새로고침 (Refresh)
    public void ProcessRefresh()
    {
        // 광고 체크
        if(RefreshCount >= 2)
        {
            Debug.Log($"Refresh. 광고 실행.");
        }

        // 현재 그리드의 보석들을 RemainingGems로 반납
        // 셔플 로직 (System.Linq)
        System.Random Rng = new System.Random();
        RemainingGems = RemainingGems.OrderBy( a => Rng.Next()).ToList();

        // 재추출하고 카운트 증가
        RefreshCount++;
        ExtractNextBundles();
        Debug.Log($"Refresh 완료. 보석 셔플 및 재배치");
    }

    // 힌트 (Hint)
    public void ProcessHint()
    {
        Debug.Log($"Hint 실행. 최적 조합 탐색");

        // 단순화 탐색 = 현재 그리드에 하나씩 더하며 조합 찾기
        List<GameObject> FoundCombo = new List<GameObject>();
        int TempSum = 0;

        foreach (var gem in CurrentDisplayBundles)
        {
            // 보석이 파괴되었을 경우
            if(gem == null) continue;
            
            Gem GemScript = gem.GetComponent<Gem>();
            if(GemScript != null)
            {
                // Gem 스크립트에서 Value 변수 가져옴
                int val = GemScript.Value;
                if(TempSum + val <= TargetCapacity)
                {
                    TempSum += val;
                    FoundCombo.Add(gem);
                }
            }
        }

        // 3초 동안 강조 연출 (코루틴)
        StartCoroutine(HighlightGems(FoundCombo));

        // 힌트 사용한 횟수 늘림
        HintCount++;
    }

    private IEnumerator HighlightGems(List<GameObject> gems)
    {
        // 강조 시작 (크기 키우기)
        foreach(var g in gems)
        {
            if(g != null) g.transform.localScale *= 1.2f;
        }

        yield return new WaitForSeconds(3f);

        // 강조 종료 (원래대로)
        foreach (var g in gems)
        {
            if(g != null) g.transform.localScale /= 1.2f;
        }
    }

    private void LoadGameState()
    {
        // SaveManager 로드 로직 구현 위치
        Debug.Log($"이어하기 데이터 로드 완료.");
    }
}
