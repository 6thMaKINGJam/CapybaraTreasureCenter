using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.Video;
using DG.Tweening;

public class ChallengeManager : MonoBehaviour
{
    public static ChallengeManager Instance;

    [Header("Managers & Config")]
    public ChunkGenerator ChunkGenerator;
    public BundleGridManager GridManager;
    public GameUIManager UIManager;

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

    [Header("카피바라 대사 시스템")]
    public CapyDialogue CapyDialogue;
    public TextMeshProUGUI CapyDialogueText;

    [Header("UI 매니저 참조")]
    public GemCountPanelManager GemCountStatusPanel;

    [Header("배경 Video Player")]
    public VideoPlayer backgroundVideoPlayer;
    [Header("Undo1 횟수")]
    public int Undo1Current = 3;
    [Header("알림")]
    public GameObject NotificationPanel; // Inspector 할당
    public TextMeshProUGUI NotificationText; // Panel 내부 텍스트
    public float NotificationDuration = 2f; // 표시 시간
    [Header("선택된 조건 표시 UI")]
    public TextMeshProUGUI ActiveRequirementText; // 현재 화면에 떠 있는 조건 텍스트
    [Header("Requirement Settings")]
    // 무료 조건 재선택 횟수
    public int MaxFreeRequirementReselect = 2; 
    private int currentReselectCount = 0;
    // UI에 남은 횟수를 표시하기 위한 텍스트
    public TextMeshProUGUI ReselectCountText;
    [Header("Requirement Range Settings")]
    // 오른쪽 항에 숫자가 들어갈 때의 범위
    public int MinRequirementValue = 2;
    public int MaxRequirementValue = 5;
    [Header("Challenge Reward Settings")]
    public float SuccessRewardTime = 10f; // 성공 시 보상 시간

    [Header("Initial Gem Settings")]
    public int InitialGemCountPerType = 10; // 타입별 초기 보석 개수
    private float currentRemainingTime; // 챌린지 전용 남은 시간 계산용
    private int RemainingBoxes => gameData.Boxes.Count - gameData.CurrentBoxIndex;

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
        gameData.RemainingGems = new Dictionary<GemType, int>();
        for (int i = 0; i < 5; i++)
        {
            gameData.RemainingGems[(GemType)i] = InitialGemCountPerType;
        }
        GenerateInitialBundlesFromGems();
        if (GameUIManager.Instance.GemCountStatusPanel != null)
        {
            GameUIManager.Instance.GemCountStatusPanel.InitLevelGemStatus(gameData.RemainingGems, 5);
        }

        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.UpdateBoxUI(
                gameData.CurrentBoxIndex, 
                0, 
                gameData.Boxes[gameData.CurrentBoxIndex].RequiredAmount, 
                gameData.Boxes.Count
            );
            
        }
        if (UIManager == null) return;

        // 비우기 버튼 연결 추가
        if (UIManager.CancelSelectButton != null)
        {
            UIManager.CancelSelectButton.onClick.RemoveAllListeners();
            UIManager.CancelSelectButton.onClick.AddListener(() => CancelSelection());
        }
        if (GemCountStatusPanel != null)
        {
            // 보석 개수 전체 다시 그리기
            GemCountStatusPanel.InitLevelGemStatus(gameData.RemainingGems, 5);
        }

        // 3. 화면에 보석 그리드 표시
        ExtractDisplayBundles();
        currentReselectCount = 0;
        UpdateAllItemUI();

        // 4. 첫 조건 선택 시작
        ShowRequirementSelection();
        
        StartCoroutine(ChallengeTimerRoutine());
    }

    // [추가] 초기 보석 데이터를 바탕으로 그리드용 번들 생성
    private void GenerateInitialBundlesFromGems()
    {
        gameData.BundlePool.Clear();
        gameData.CurrentDisplayBundles.Clear();

        // 현재 가진 보석 총량을 바탕으로 랜덤한 크기(1~4)의 번들을 생성하여 풀에 채움
        foreach (GemType type in Enum.GetValues(typeof(GemType)))
        {
            int total = gameData.RemainingGems[type];
            int allocated = 0;
            while (allocated < total)
            {
                int size = UnityEngine.Random.Range(1, Mathf.Min(5, total - allocated + 1));
                GemBundle newBundle = new GemBundle {
                    BundleID = Guid.NewGuid().ToString(),
                    GemType = type,
                    GemCount = size
                };
                gameData.BundlePool.Add(newBundle);
                allocated += size;
            }
        }
        
        // 번들 풀 섞기
        gameData.BundlePool = gameData.BundlePool.OrderBy(x => UnityEngine.Random.value).ToList();
        
        // 상위 12개를 그리드에 표시
        int displayCount = Mathf.Min(12, gameData.BundlePool.Count);
        for (int i = 0; i < displayCount; i++)
        {
            gameData.CurrentDisplayBundles.Add(gameData.BundlePool[i]);
        }
    }

    // 비우기(선택 취소) 기능 구현
    public void CancelSelection()
    {
        if (gameData.SelectedBundles.Count == 0) return;

        // 선택된 보석들을 하나씩 되돌림 (Undo1 로직 활용)
        // 역순으로 처리하여 그리드 위치를 정확히 복구합니다.
        for (int i = gameData.SelectedBundles.Count - 1; i >= 0; i--)
        {
            GemBundle bundle = gameData.SelectedBundles[i];
            gameData.RemainingGems[bundle.GemType] += bundle.GemCount;

            if (selectedBundleOriginalIndices.TryGetValue(bundle, out int gridIndex))
            {
                gameData.CurrentDisplayBundles[gridIndex] = bundle;
                GridManager.ReplaceBundleAtIndex(gridIndex, bundle, OnBundleClicked, true);
            }
        }

        // 데이터 초기화
        gameData.SelectedBundles.Clear();
        selectedBundleOriginalIndices.Clear();

        // UI 일괄 갱신
        UIManager.SelectionPanel.UpdateUI(gameData.SelectedBundles);
        UIManager.UpdateBoxUI(gameData.CurrentBoxIndex, 0, GetCurrentBox().RequiredAmount, gameData.Boxes.Count);
        
        if (GemCountStatusPanel != null)
        {
            // 보석 개수 전체 다시 그리기
            GemCountStatusPanel.InitLevelGemStatus(gameData.RemainingGems, 5);
        }
    }
    public void ExtractDisplayBundles()
    {
        gameData.CurrentDisplayBundles.Clear();
        
        // 풀에서 최대 12개를 가져와 표시 목록에 추가
        int count = Mathf.Min(12, gameData.BundlePool.Count);
        for(int i = 0; i < count; i++)
        {
            gameData.CurrentDisplayBundles.Add(gameData.BundlePool[i]);
        }
        
        // UI 매니저를 통해 그리드 갱신 (중요!)
        if (GameUIManager.Instance != null && GameUIManager.Instance.GridManager != null)
        {
            // OnBundleClicked는 ChallengeManager 내부에 구현된 클릭 이벤트를 연결합니다.
            GameUIManager.Instance.GridManager.RefreshGrid(gameData.CurrentDisplayBundles, OnBundleClicked);
        }
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
        for(int i = 0; i < 5; i++)
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
    // 보석 클릭 시 처리
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
        Time.timeScale = 0f;
        // 선택 중에는 시간 정지 (필요 시)
        RequirementPopupObject.SetActive(true);
        
        // 기존 카드 제거
        foreach (Transform child in RequirementParentPanel) Destroy(child.gameObject);

        // 카드 3개 생성
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
        Box targetBox = GetCurrentBox();
        
        // 1. 왼쪽 항 보석 타입 결정 (0~4)
        req.LeftType = (GemType)UnityEngine.Random.Range(0, 5);
        
        // 2. 연산자 결정
        req.Op = (ComparisonOperator)UnityEngine.Random.Range(0, 3);
        
        // 3. 오른쪽 항 결정 (보석 vs 숫자)
        // 보석 종류가 하나씩 들어가는 것을 방지하기 위해 타입 설정 로직 변경
        req.IsValueComparison = UnityEngine.Random.value > 0.5f;

        if (req.IsValueComparison)
        {
            // [수정] 오른쪽 항이 숫자인 경우 인스펙터 설정 범위 사용 (2~5 등)
            req.RightValue = UnityEngine.Random.Range(MinRequirementValue, MaxRequirementValue + 1);
        }
        else
        {
            // [수정] 오른쪽 항이 보석인 경우, 왼쪽 항과 무조건 다른 타입이 나오도록 처리
            List<GemType> otherTypes = new List<GemType>();
            for (int i = 0; i < 5; i++)
            {
                if ((GemType)i != req.LeftType)
                {
                    otherTypes.Add((GemType)i);
                }
            }
            req.RightType = otherTypes[UnityEngine.Random.Range(0, otherTypes.Count)];
        }

        // 시간 보상 설정
        req.RewardTime = SuccessRewardTime;
        // [추가] 보석 보상을 여러 종류로 랜덤 배분
        if (targetBox != null)
        {
            int totalToGive = targetBox.RequiredAmount;
            // 1~3가지 종류의 보석으로 배분
            int typeCount = UnityEngine.Random.Range(1, 4);
            req.RewardGemTypes = new GemType[typeCount];
            req.RewardGemCounts = new int[typeCount];

            int allocated = 0;
            List<GemType> availableTypes = Enum.GetValues(typeof(GemType)).Cast<GemType>().ToList();
            
            for (int i = 0; i < typeCount; i++)
            {
                // 랜덤 타입 선택
                int typeIdx = UnityEngine.Random.Range(0, availableTypes.Count);
                req.RewardGemTypes[i] = availableTypes[typeIdx];
                availableTypes.RemoveAt(typeIdx);

                // 마지막 루프면 남은 거 다 몰아줌
                if (i == typeCount - 1)
                {
                    req.RewardGemCounts[i] = totalToGive - allocated;
                }
                else
                {
                    int maxPiece = (totalToGive - allocated) - (typeCount - 1 - i);
                    int piece = UnityEngine.Random.Range(1, maxPiece + 1);
                    req.RewardGemCounts[i] = piece;
                    allocated += piece;
                }
            }
        }
        return req;
    }

    private void OnRequirementSelected(ChallengeRequirement selectedReq)
    {
        currentActiveRequirement = selectedReq;
        RequirementPopupObject.SetActive(false);
        Time.timeScale = 1f; // 게임 재개

        // 선택한 조건을 화면 UI에 업데이트
        if (ActiveRequirementText != null)
        {
            ActiveRequirementText.text = $"현재 조건: {selectedReq.GetDescription()}";
        }
        
        ShowTopNotification("새로운 조건이 적용되었습니다카피!");
    }

    public void OnClickComplete()
    {
        if (currentActiveRequirement == null) return;

        Box currentBox = GetCurrentBox();
        int selectedTotal = CalculateSelectedTotal();

        // 1. 상자 수량 확인
        if (selectedTotal != currentBox.RequiredAmount)
        {
            VibrationManager.Instance.Vibrate(VibrationPattern.Warning);
            ShowTopNotification("보석 수량이 맞지 않습니다카피!");
            return;
        }

        // 2. 조건(Requirement) 검사
        if (currentActiveRequirement.Validate(gameData.SelectedBundles))
        {
            // 성공: 시간 보상 및 다음 단계
            currentRemainingTime += currentActiveRequirement.RewardTime;
            ProcessBoxSuccess();
        }
        else
        {
            // 실패: 진동 및 알림
            VibrationManager.Instance.Vibrate(VibrationPattern.Warning);
            ShowTopNotification("조건에 맞지 않습니다카피!");
            CancelSelection(); 
        }
    }
    // 조건 검사 로직 예시
    private bool CheckRequirementMet()
    {
        if (currentActiveRequirement == null) return true; // 조건이 없으면 통과

        // 현재 상자에 담긴 보석들의 색상별 개수 합산
        Dictionary<GemType, int> counts = new Dictionary<GemType, int>();
        foreach (var bundle in gameData.SelectedBundles)
        {
            if (!counts.ContainsKey(bundle.GemType)) counts[bundle.GemType] = 0;
            counts[bundle.GemType] += bundle.GemCount;
        }

        int leftVal = counts.ContainsKey(currentActiveRequirement.LeftType) ? counts[currentActiveRequirement.LeftType] : 0;
        int rightVal = 0;

        if (currentActiveRequirement.IsValueComparison)
        {
            rightVal = currentActiveRequirement.RightValue;
        }
        else
        {
            rightVal = counts.ContainsKey(currentActiveRequirement.RightType) ? counts[currentActiveRequirement.RightType] : 0;
        }

        // 부등호 판별
        switch (currentActiveRequirement.Op)
        {
            case ComparisonOperator.Equal: return leftVal == rightVal;
            case ComparisonOperator.LessThan: return leftVal < rightVal;
            case ComparisonOperator.GreaterThan: return leftVal > rightVal;
            default: return false;
        }
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
        
        Debug.LogWarning($"[GameManager] {message}");
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
    int undoLeft = Mathf.Max(0, 3 - gameData.UndoCount);
    int undo1Left = Mathf.Max(0, Undo1Current - gameData.Undo1Count);
    int hintLeft = Mathf.Max(0, 3 - gameData.HintCount);
    
     // ✅ 수정: 최대 횟수도 함께 전달
    UIManager.UpdateHintAndItemUI(
        hintLeft, 0,
        undo1Left, Undo1Current,
        undoLeft, 0
    );

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
        if (gameData.Undo1Count >= Undo1Current)
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
    }

    public void Execute1Undo()
    {
        // 1. 가장 마지막에 추가된 보석 묶음 데이터 가져오기
        int lastIndex = gameData.SelectedBundles.Count - 1;
        GemBundle lastBundle = gameData.SelectedBundles[lastIndex];

        // 2. 데이터 복구
        gameData.SelectedBundles.RemoveAt(lastIndex); // 선택 리스트에서 제거
        gameData.RemainingGems[lastBundle.GemType] += lastBundle.GemCount; // 보석 총량 복구

        if (selectedBundleOriginalIndices.TryGetValue(lastBundle, out int originalGridIndex))
        {
            // 4. 상자 안의 가장 최근 보석 이미지 제거
            if (UIManager != null && UIManager.SelectionPanel != null)
            {
                UIManager.SelectionPanel.RemoveLastGem();
            }

            // 5. 그리드 데이터 복구 (나머지 보석 유지)
            gameData.CurrentDisplayBundles[originalGridIndex] = lastBundle;

            // 6. 해당 칸만 시각적으로 복구 (isRestoring: true 사용)
            if (GridManager != null)
            {
                GridManager.ReplaceBundleAtIndex(
                    originalGridIndex,
                    lastBundle,
                    OnBundleClicked,
                    isRestoring: true
                );
            }

            // 사용한 인덱스 정보 삭제
            selectedBundleOriginalIndices.Remove(lastBundle);
        }

        // B. 되돌리기 버튼의 남은 숫자 줄이기
        UpdateAllItemUI(); // 아이템 카운트 텍스트 갱신 (UndoCount 반영)
        
        // D. 상자 진행도 텍스트(예: 3/5 -> 2/5) 및 기타 UI 갱신
        RefreshUI();
        
        // 위험도 패널(GemCountPanel) 숫자 업데이트
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

    private void ShowTopNotification(string message)
    {
        if(NotificationPanel == null || NotificationText == null)
        {
            Debug.LogWarning("[GameManager] NotificationPanel이 할당되지 않았습니다!");
            return;
        }
        
        StopCoroutine(nameof(NotificationCoroutine));
        StartCoroutine(NotificationCoroutine(message));
    }
    private void RefreshUI()
    {
    UIManager.UpdateBoxUI(
            gameData.CurrentBoxIndex,
            CalculateSelectedTotal(),
            GetCurrentBox().RequiredAmount,
            gameData.Boxes.Count // ← 추가
        );

        // ✅ UpdateAllItemUI() 호출로 통일
        UpdateAllItemUI();
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
        yield return new WaitForSeconds(NotificationDuration);
        
        // 페이드 아웃
        yield return canvasGroup.DOFade(0f, 0.3f).SetEase(Ease.InQuad).WaitForCompletion();
        
        NotificationPanel.SetActive(false);
    }

    // [수정] 상자 완료 시 보상 로직
    private void ProcessBoxSuccess()
    {
            // 1. 시간 보상 (설정된 10초 등 반영)
        currentRemainingTime += currentActiveRequirement.RewardTime;

        // 2. 보석 보상: 현재 완료한 상자의 요구량(RequiredAmount)만큼 보충
        int totalGemsToReplenish = GetCurrentBox().RequiredAmount; 
        int currentCount = 0;

        // 총 개수를 채울 때까지 랜덤하게 번들을 생성합니다.
        if (currentActiveRequirement.RewardGemCounts != null)
        {
            for (int i = 0; i < currentActiveRequirement.RewardGemCounts.Length; i++)
            {
                GemType type = currentActiveRequirement.RewardGemTypes[i];
                int count = currentActiveRequirement.RewardGemCounts[i];
                
                gameData.RemainingGems[type] += count;

                // 번들 풀에 추가
                int allocated = 0;
                while (allocated < count)
                {
                    int size = UnityEngine.Random.Range(1, Mathf.Min(5, count - allocated + 1));
                    gameData.BundlePool.Add(new GemBundle {
                        BundleID = Guid.NewGuid().ToString(),
                        GemType = type,
                        GemCount = size
                    });
                    allocated += size;
                }
            }
        }

        // 3. UI 갱신
        if (GemCountStatusPanel != null)
        {
            GemCountStatusPanel.InitLevelGemStatus(gameData.RemainingGems, 5);
        }

        gameData.CurrentBoxIndex++;
        
        // [수정] 애니메이션 종료 후 다음 상자 정보를 명시적으로 업데이트
        UIManager.AnimateBoxChange(() => {
            gameData.SelectedBundles.Clear();
            selectedBundleOriginalIndices.Clear();
            UIManager.SelectionPanel.UpdateUI(gameData.SelectedBundles);
            
            // 다음 상자 정보 UI 즉시 반영
            Box nextBox = GetCurrentBox();
            if (nextBox != null)
            {
                UIManager.UpdateBoxUI(
                    gameData.CurrentBoxIndex, 
                    0, 
                    nextBox.RequiredAmount, 
                    gameData.Boxes.Count
                );
            }

            if (GemCountStatusPanel != null)
                GemCountStatusPanel.InitLevelGemStatus(gameData.RemainingGems, 5);

            gameData.BundlePool = gameData.BundlePool.OrderBy(x => UnityEngine.Random.value).ToList();
            ExtractDisplayBundles();
            ShowRequirementSelection();
        });
    }

    private void HandleTimeOver()
    {
        gameData.GameState = GameState.TimeOver;
        
        // 1. 게임 시간 정지 (UI 버튼은 눌려야 하므로 Canvas가 Screen Space - Overlay인지 확인 필요)
        Time.timeScale = 0f; 

        StopBackgroundMedia();
        CapyDialogue.StopDialogue(CapyDialogueText);
        
        string randomMsg = CapyDialogue.GetRandomMessage(DialogueType.TimeOverGameOver);

        if(PopupParentSetHelper.Instance == null) return;
        
        // 2. 팝업 생성
        GameObject popupObj = PopupParentSetHelper.Instance.CreatePopup("Prefabs/GameOverPopup");
        if(popupObj == null) return;

        // 팝업이 다른 UI에 가려지지 않도록 최상단으로 올림
        popupObj.transform.SetAsLastSibling();

        GameOverPopup popup = popupObj.GetComponent<GameOverPopup>();
        if(popup == null) return;

        SoundManager.Instance.PlayFX(SoundType.GameOver);

        // 3. 콜백 설정 (다시 시작할 때 반드시 TimeScale을 1로 복구해야 함)
        popup.Setup(
            randomMsg,
            () => {
                Time.timeScale = 1f; // 시간 복구
                Core.SceneLoader.Instance.RestartCurrentLevel();
            },
            () => {
                Time.timeScale = 1f; // 시간 복구
                Core.SceneLoader.Instance.GoToMainHome();
            }
        );
    }

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

    // [추가] 조건 재선택 버튼 클릭 시 호출될 함수
    public void OnClickReselectRequirement()
    {
        // 1. 무료 횟수 체크
        if (currentReselectCount < MaxFreeRequirementReselect)
        {
            currentReselectCount++;
            ExecuteReselect();
        }
        else
        {
            // 2. 무료 횟수 초과 시 광고 팝업
            ShowAdConfirmationPopup(() =>
            {
                AdManager.Instance.ShowRewardedAd((success) =>
                {
                    if (success) ExecuteReselect();
                });
            }, null);
        }
    }

    // [추가] 실제 재선택 실행 로직
    private void ExecuteReselect()
    {
        UpdateReselectUI();
        ShowRequirementSelection(); // 기존에 만든 팝업 띄우기 함수 호출
        ShowTopNotification("조건을 다시 선택합니다카피!");
    }

    // [추가] UI 업데이트 로직
    private void UpdateReselectUI()
    {
        if (ReselectCountText != null)
        {
            int leftCount = Mathf.Max(0, MaxFreeRequirementReselect - currentReselectCount);
            ReselectCountText.text = $"{leftCount}";
        }
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
    public int[] RewardGemCounts;
    public GemType[] RewardGemTypes;

    public string GetDescription()
    {
        string opStr = Op == ComparisonOperator.Equal ? "==" : (Op == ComparisonOperator.LessThan ? "<" : ">");
        string rightStr = IsValueComparison ? RightValue.ToString() : RightType.ToString();
        return $"{LeftType} {opStr} {rightStr}";
    }
    public string GetRewardGemDescription()
    {
        if (RewardGemCounts == null || RewardGemTypes == null || RewardGemCounts.Length == 0) 
            return "No Gems";

        List<string> rewards = new List<string>();
        for (int i = 0; i < RewardGemCounts.Length; i++)
        {
            // 예: Red+2
            rewards.Add($"{RewardGemTypes[i].ToString().Substring(0, 1)}+{RewardGemCounts[i]}");
        }
        return string.Join(", ", rewards);
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