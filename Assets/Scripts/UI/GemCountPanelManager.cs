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
        UpdateDangerUI(0); // 초기 업데이트
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
        if (remainingBoxes < 4)
        {
            if(DangerSlider != null) DangerSlider.gameObject.SetActive(false);
            SetGemItemsVisibility(true);
            return;
        }

        if(DangerSlider != null) DangerSlider.gameObject.SetActive(true);
        SetGemItemsVisibility(false);

        int minGemCount = currentGems.Values.Count > 0 ? currentGems.Values.Min() : 0;
        
        // 1. 비율 계산 (0.0 ~ 1.0)
        // 보석이 상자보다 많으면 1.0 이상이 나옵니다.
        float ratio = (remainingBoxes > 0) ? (float)minGemCount / remainingBoxes : 1f;

        float sliderValue = 0f;
        Color statusColor = Color.green;

        // 2. 위험도 판정 (기획에 맞춰 조절하세요)
        if (ratio < 0.5f) // 보석이 상자의 절반도 안 될 때 (위험)
        {
            statusColor = Color.red;
            sliderValue = Mathf.Lerp(0f, 0.33f, ratio / 0.5f); 
        }
        else if (ratio < 1.0f) // 보석이 상자보다 약간 적을 때 (경고)
        {
            statusColor = Color.yellow;
            sliderValue = Mathf.Lerp(0.33f, 0.66f, (ratio - 0.5f) / 0.5f);
        }
        else // 보석이 충분할 때 (안전)
        {
            statusColor = Color.green;
            sliderValue = Mathf.Lerp(0.66f, 1f, Mathf.Min(ratio - 1f, 1f));
        }

        // 3. UI 적용
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