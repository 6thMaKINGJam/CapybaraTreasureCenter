using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class GemCountPanelManager : MonoBehaviour
{
    [Header("위험도 UI 설정")]
    public Slider DangerSlider; // 타이머 아래에 새로 추가할 슬라이더
    //public Image SliderFillImage; // 슬라이더의 색상을 바꿀 Fill 이미지//
    
    [Header("기존 아이템 설정")]
    public GameObject GemCountItemPrefab;
    public GemSpriteDatabase SpriteDatabase;

    [Header("슬라이더 노출 설정")]
    [Tooltip("해당 챕터(LevelConfig)의 마지막 레벨부터 몇 번째 레벨까지 슬라이더를 보여줄지 설정합니다.")]
    public int sliderExposureRange = 5;

    private Dictionary<GemType, GemCountItem> gemItemDict = new Dictionary<GemType, GemCountItem>();
    private Dictionary<GemType, int> currentGems = new Dictionary<GemType, int>();
    private int totalBoxesInLevel;
    private int gemTypeCount;

    // 초기화 시 상자 총 개수 정보를 받도록 수정
    public void InitLevelGemStatus(Dictionary<GemType, int> totalGems, int gemTypeCount)
    {
        this.gemTypeCount = gemTypeCount;
        currentGems = new Dictionary<GemType, int>(totalGems);
        
        // UI 생성 (처음엔 숨겨져 있을 수 있음)
        CreateGemItems();
        ResetUIState();
    }

    // UI 상태를 위험도 슬라이더 모드로 강제 리셋하는 메서드
    private void ResetUIState()
    {
        bool isChallenge = (ChallengeModeManager.Instance != null);

        if (isChallenge)
        {
            if (DangerSlider != null) DangerSlider.gameObject.SetActive(false);
            SetGemItemsVisibility(true);
        }
        else
        {
            // 레벨 모드에서는 시작 시 무조건 슬라이더를 켜고 상세 항목을 숨깁니다.
            if (DangerSlider != null) DangerSlider.gameObject.SetActive(true);
            SetGemItemsVisibility(false);
            
            // 슬라이더 값도 안전(초록) 상태로 초기화
            if (DangerSlider != null) DangerSlider.value = 1f;
           // if (SliderFillImage != null) SliderFillImage.color = Color.green;
        }
    }

    private void CreateGemItems()
    {
        ClearAllItems();
        foreach (var kvp in currentGems)
        {
            GameObject itemObj = Instantiate(GemCountItemPrefab, transform);
            GemCountItem item = itemObj.GetComponent<GemCountItem>();
            item.SetData(kvp.Key, kvp.Value, SpriteDatabase);
            gemItemDict[kvp.Key] = item;
        }
    }

    public void UpdateGemCount(GemType type, int newCount, int remainingBoxes)
    {
        currentGems[type] = newCount;
        
        // 기존 개별 아이템 업데이트
        if (gemItemDict.ContainsKey(type))
            gemItemDict[type].UpdateCount(newCount);

        UpdateDangerUI(remainingBoxes);
    }

    private void UpdateDangerUI(int remainingBoxes)
    {
        bool isChallenge = (ChallengeModeManager.Instance != null);

        if (isChallenge)
        {
            if (DangerSlider != null) DangerSlider.gameObject.SetActive(false);
            SetGemItemsVisibility(true);
            return;
        }

        // 1. 현재 레벨 정보 및 설정 가져오기
        int selectedLevel = PlayerPrefs.GetInt("SelectedLevel", 1);
        var gm = LevelModeManager.Instance;

        if (gm == null || gm.CurrentLevelConfig == null) return;

        // 2. 현재 챕터의 최대 레벨 계산 (예: 1챕터는 33, 2챕터는 66 등)
        // LevelConfig의 구조에 따라 다르지만, 일반적으로 다음 챕터 시작 전까지가 최대 레벨입니다.
        int chapterMaxLevel = GetChapterMaxLevel(gm.CurrentLevelConfig.ChapterNumber);
        
        // 3. 슬라이더 노출 범위 체크 (뒤에서 N번째 레벨부터 마지막 레벨까지)
        // 예: Max가 33이고 Range가 5라면, 29, 30, 31, 32, 33레벨에서 true
        bool isInSliderRange = selectedLevel > (chapterMaxLevel - sliderExposureRange) && selectedLevel <= chapterMaxLevel;

        // 4. UI 표시 분기 (범위 안이면서 상자가 4개 이상 남았을 때 슬라이더 표시)
        if (isInSliderRange && remainingBoxes >= 4)
        {
            if (DangerSlider != null && !DangerSlider.gameObject.activeSelf) 
                DangerSlider.gameObject.SetActive(true);
            
            SetGemItemsVisibility(false);

            // 위험도 계산 로직 (기존 유지)
            if (currentGems == null || currentGems.Count == 0) return;
            int minGemCount = currentGems.Values.Min();
            float ratio = (remainingBoxes > 0) ? (float)minGemCount / remainingBoxes : 1f;

            float sliderValue = 0f;
            Color statusColor = Color.green;

            if (ratio < 0.5f) { statusColor = Color.red; sliderValue = Mathf.Lerp(0f, 0.33f, ratio / 0.5f); }
            else if (ratio < 1.0f) { statusColor = Color.yellow; sliderValue = Mathf.Lerp(0.33f, 0.66f, (ratio - 0.5f) / 0.5f); }
            else { statusColor = Color.green; sliderValue = Mathf.Lerp(0.66f, 1f, Mathf.Min(ratio - 1f, 1f)); }

            if (DangerSlider != null) DangerSlider.value = sliderValue;
            //if (SliderFillImage != null) SliderFillImage.color = statusColor;
        }
        else
        {
            // 그 외의 경우 상세 항목 표시
            if (DangerSlider != null && DangerSlider.gameObject.activeSelf) 
                DangerSlider.gameObject.SetActive(false);
            
            SetGemItemsVisibility(true);
        }
    }

    private void SetGemItemsVisibility(bool visible)
    {
        foreach (var item in gemItemDict.Values)
        {
            item.gameObject.SetActive(visible);
        }
    }

    private void ClearAllItems()
    {
        gemItemDict.Clear();
        foreach (Transform child in transform) Destroy(child.gameObject);
    }

    private int GetChapterMaxLevel(int chapterNumber)
    {
        switch (chapterNumber)
        {
            case 1: return 11;
            case 2: return 22;
            case 3: return 33;
            case 34: return 34;
            default: return 33;
        }
    }
}