using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class ChallengeManager : MonoBehaviour
{
    public static ChallengeManager Instance;

    [Header("Managers & Config")]
    public ChunkGenerator ChunkGenerator;
    // 챌린지 모드는 별도의 LevelConfig 에셋 없이 코드 내에서 기본값을 정의합니다 (소프트 코딩)
    public int totalChallengeBoxes = 1000;
    public float startingTimeLimit = 20f;
    public int challengeMaxRequired = 12;

    [Header("Requirement UI")]
    public GameObject RequirementCardPrefab;
    public Transform RequirementParentPanel;
    public GameObject RequirementPopupObject;

    [Header("Game Data")]
    private GameData gameData;
    private ChallengeRequirement currentActiveRequirement;
    private List<GemBundle> currentSelectedBundles = new List<GemBundle>();
    private Dictionary<GemBundle, int> selectedBundleOriginalIndices = new Dictionary<GemBundle, int>();
    
    private float currentRemainingTime; // 챌린지 전용 남은 시간 계산용

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        InitChallengeMode();
    }

    private void InitChallengeMode()
    {
        Time.timeScale = 1f;
        gameData = new GameData();
        gameData.GameState = GameState.Playing;
        
        // 초기 제한 시간 설정
        currentRemainingTime = startingTimeLimit;

        // 1. 맵 생성 데이터 준비 (임시 LevelConfig 생성)
        LevelConfig tempConfig = ScriptableObject.CreateInstance<LevelConfig>();
        tempConfig.BoxCount = totalChallengeBoxes;
        tempConfig.MaxRequiredPerBox = challengeMaxRequired;
        tempConfig.GemTypeCount = 5; // 챌린지는 모든 색상 사용

        var chunkData = ChunkGenerator.GenerateAllChunks(tempConfig);
        gameData.Boxes = new List<Box>(chunkData.AllBoxes);
        gameData.BundlePool = new List<GemBundle>(chunkData.MergedBundlePool);
        gameData.RemainingGems = new Dictionary<GemType, int>(chunkData.TotalRemainingGems);

        // 2. 보석 개수 UI 상시 노출 설정
        // GemCountPanelManager에서 챌린지 모드 플래그를 확인하여 DangerSlider를 끄고 아이템을 항상 켭니다
        if (GameUIManager.Instance.GemCountStatusPanel != null)
        {
            GameUIManager.Instance.GemCountStatusPanel.InitLevelGemStatus(gameData.RemainingGems, 5);
        }

        // 3. 첫 조건 선택 시작
        ShowRequirementSelection();
        
        // 타이머 코루틴 시작
        StartCoroutine(ChallengeTimerRoutine());
    }

    private IEnumerator ChallengeTimerRoutine()
    {
        while (gameData.GameState == GameState.Playing)
        {
            currentRemainingTime -= Time.deltaTime;
            
            // UI 타이머 슬라이더 업데이트 (기존 UIManager 활용)
            if (GameUIManager.Instance.TimerSlider != null)
            {
                // 챌린지 모드는 최대 시간이 가변적이므로 슬라이더 연출은 별도 조정 권장
                GameUIManager.Instance.TimerSlider.value = Mathf.Max(0, currentRemainingTime / startingTimeLimit);
            }

            if (currentRemainingTime <= 0)
            {
                HandleTimeOver();
                yield break;
            }
            yield return null;
        }
    }

    // [요구사항 3, 4] 조건 카드 생성
    public void ShowRequirementSelection()
    {
        // 팝업 열기 전 게임 일시정지 (선택 중 시간 흐름 방지 시)
        // Time.timeScale = 0f; 
        RequirementPopupObject.SetActive(true);
        
        foreach (Transform child in RequirementParentPanel) Destroy(child.gameObject);

        for (int i = 0; i < 3; i++)
        {
            var req = GenerateRandomRequirement();
            GameObject cardObj = Instantiate(RequirementCardPrefab, RequirementParentPanel);
            cardObj.GetComponent<RequirementCard>().Setup(req, OnRequirementSelected);
        }
    }

    private ChallengeRequirement GenerateRandomRequirement()
    {
        ChallengeRequirement req = new ChallengeRequirement();
        req.LeftType = (GemType)UnityEngine.Random.Range(0, 5);
        req.Op = (ComparisonOperator)UnityEngine.Random.Range(0, 3);
        
        req.IsValueComparison = UnityEngine.Random.value > 0.5f;
        if (req.IsValueComparison)
            req.RightValue = UnityEngine.Random.Range(1, 6); // 숫자 1~5 [요구사항 4]
        else
            req.RightType = (GemType)UnityEngine.Random.Range(0, 5);

        // 시간 보상 설정 (성공 시 5초 추가 등)
        req.RewardTime = 5f;
        return req;
    }

    private void OnRequirementSelected(ChallengeRequirement selectedReq)
    {
        currentActiveRequirement = selectedReq;
        RequirementPopupObject.SetActive(false);
        Time.timeScale = 1f; // 선택 완료 후 재개
    }

    public void OnClickComplete()
    {
        if (currentActiveRequirement == null) return;

        bool isSuccess = currentActiveRequirement.Validate(currentSelectedBundles);

        if (isSuccess)
        {
            // 시간 보상 추가 [요구사항 1]
            currentRemainingTime += currentActiveRequirement.RewardTime;
            
            // 상자 교체 및 다음 조건 선택
            ProcessBoxSuccess();
        }
        else
        {
            VibrationManager.Instance.Vibrate(VibrationPattern.Warning);
            // 실패 시 처리 (보석 초기화 등)
        }
    }

    private void ProcessBoxSuccess()
    {
        currentSelectedBundles.Clear();
        // 묶음 리필 및 그리드 갱신 로직...
        ShowRequirementSelection();
    }

    private void HandleTimeOver()
    {
        gameData.GameState = GameState.TimeOver;
        // 게임오버 팝업 출력
    }
}


[System.Serializable]
public enum ComparisonOperator { Equal, LessThan, GreaterThan }

[System.Serializable]
public class ChallengeRequirement
{
    public GemType LeftType;
    public ComparisonOperator Op;
    public bool IsValueComparison;
    public GemType RightType;
    public int RightValue;

    public float RewardTime;
    public int RewardGemCount;

    public string GetDescription()
    {
        string opStr = Op == ComparisonOperator.Equal ? "==" : (Op == ComparisonOperator.LessThan ? "<" : ">");
        string rightStr = IsValueComparison ? RightValue.ToString() : RightType.ToString();
        return $"{LeftType} {opStr} {rightStr}";
    }

    public bool Validate(List<GemBundle> selected)
    {
        // 람다 식을 사용하기 위해 파일 최상단에 using System.Linq; 가 있어야 합니다.
        int leftCount = 0;
        foreach(var b in selected) if(b.GemType == LeftType) leftCount += b.GemCount;

        int rightCompareValue = 0;
        if (IsValueComparison)
        {
            rightCompareValue = RightValue;
        }
        else
        {
            foreach(var b in selected) if(b.GemType == RightType) rightCompareValue += b.GemCount;
        }

        switch (Op)
        {
            case ComparisonOperator.Equal: return leftCount == rightCompareValue;
            case ComparisonOperator.LessThan: return leftCount < rightCompareValue;
            case ComparisonOperator.GreaterThan: return leftCount > rightCompareValue;
            default: return false;
        }
    }
}