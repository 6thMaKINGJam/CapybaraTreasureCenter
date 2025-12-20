using UnityEngine;
using UnityEngine.UI;
using System;

public class GemBundlePrefab : MonoBehaviour
{
    public Image GemIcon;
    public Outline Outline;
    
    [Header("보석 스프라이트 매핑")]
    [Tooltip("Red 1개, Red 2개, Red 3개, Red 4개, Blue 1개, ... 순서")]
    public Sprite[] GemSprites; // 5종 x 4개 = 20개 스프라이트
    
    private GemBundle bundleData;
    public event Action<GemBundlePrefab> OnClickBundle;
    
    public void SetData(GemBundle data)
    {
        bundleData = data;
        
        // ===== 계산식으로 인덱스 구하기 =====
        // GemType: Red(0), Blue(1), Green(2), Yellow(3), Purple(4)
        // Count: 1~4
        // Index = GemType * 4 + (Count - 1)
        int index = (int)data.GemType * 4 + (data.GemCount - 1);
        
        if(index >= 0 && index < GemSprites.Length && GemSprites[index] != null)
        {
            GemIcon.sprite = GemSprites[index];
        }
        else
        {
            Debug.LogError($"[GemBundlePrefab] 스프라이트 인덱스 오류: {data.GemType} x{data.GemCount} (index: {index})");
        }
    }
    
    public GemBundle GetData() => bundleData;
    
    public void SetSelected(bool isSelected)
    {
        if(Outline != null) Outline.enabled = isSelected;
    }
    
    public void OnClick()
    {
        OnClickBundle?.Invoke(this);
    }
}