using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class GemCountPanelManager : MonoBehaviour
{
    [Header("위험도 UI 설정")]
    public Slider DangerSlider; // 타이머 아래에 새로 추가할 슬라이더
    public Image SliderFillImage; // 슬라이더의 색상을 바꿀 Fill 이미지
    
    [Header("기존 아이템 설정")]
    public GameObject GemCountItemPrefab;
    public GemSpriteDatabase SpriteDatabase;

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
        bool isChallenge = (ChallengeManager.Instance != null);

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
            if (SliderFillImage != null) SliderFillImage.color = Color.green;
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
        // 1. 챌린지 모드 체크: ChallengeManager 인스턴스가 존재하면 챌린지 모드로 간주
        // (또는 이전에 제안드린 대로 bool 플래그를 Init 시점에 저장해두고 쓰는 것이 더 성능에 좋습니다)
        bool isChallenge = (ChallengeManager.Instance != null);

        if (isChallenge)
        {
            if (!this.gameObject.activeSelf) 
                this.gameObject.SetActive(true);

            // 챌린지 모드: 슬라이더 비활성화, 보석 아이템 항상 표시
            if (DangerSlider != null && DangerSlider.gameObject.activeSelf) 
                DangerSlider.gameObject.SetActive(false);
                
            SetGemItemsVisibility(true);
            return;
        }

        // 2. 레벨 모드: 남은 상자 개수에 따른 분기 로직
        if (remainingBoxes < 4)
        {
            // 상자가 얼마 안 남았을 때: 슬라이더 끄고 보석 개수 상세 표시
            if (DangerSlider != null && DangerSlider.gameObject.activeSelf) 
                DangerSlider.gameObject.SetActive(false);
                
            SetGemItemsVisibility(true);
            return;
        }

        // 상자가 많이 남았을 때: 슬라이더 켜고 보석 개수 상세 숨김
        if (DangerSlider != null && !DangerSlider.gameObject.activeSelf) 
            DangerSlider.gameObject.SetActive(true);
            
        SetGemItemsVisibility(false);

        // 3. 위험도 슬라이더 값 계산 (레벨 모드 전용)
        if (currentGems == null || currentGems.Count == 0) return;

        int minGemCount = currentGems.Values.Min();
        float ratio = (remainingBoxes > 0) ? (float)minGemCount / remainingBoxes : 1f;

        float sliderValue = 0f;
        Color statusColor = Color.green;

        // 위험도 판정 로직
        if (ratio < 0.5f) // 위험 (빨강)
        {
            statusColor = Color.red;
            sliderValue = Mathf.Lerp(0f, 0.33f, ratio / 0.5f); 
        }
        else if (ratio < 1.0f) // 경고 (노랑)
        {
            statusColor = Color.yellow;
            sliderValue = Mathf.Lerp(0.33f, 0.66f, (ratio - 0.5f) / 0.5f);
        }
        else // 안전 (초록)
        {
            statusColor = Color.green;
            sliderValue = Mathf.Lerp(0.66f, 1f, Mathf.Min(ratio - 1f, 1f));
        }

        // UI 적용
        if (DangerSlider != null) DangerSlider.value = sliderValue;
        if (SliderFillImage != null) SliderFillImage.color = statusColor;
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
}