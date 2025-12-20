using System;
using UnityEngine;
using UnityEngine.UI;

public class GemBundlePrefab : MonoBehaviour
{
    [Header("UI 요소")]
    public Image GemIcon; // 보석 이미지 아이콘
    public Outline Outline; // 선택 시 외곽선
    
    // 데이터
    private GemBundle bundleData;
    
    // 클릭 이벤트
    public event Action<GemBundlePrefab> OnClickBundle;

    // ========== 데이터 설정 ==========
    public void SetData(GemBundle data)
    {
        bundleData = data;
        
        // 스프라이트 로드 (Resources/GemSprites/{GemType}{Count}.png)
        string spriteName = $"{data.GemType}{data.GemCount}";
        Sprite loadedSprite = Resources.Load<Sprite>($"GemSprites/{spriteName}");
        
        if(loadedSprite != null)
        {
            GemIcon.sprite = loadedSprite;
        }
        else
        {
            Debug.LogWarning($"[GemBundlePrefab] 스프라이트를 찾을 수 없습니다: {spriteName}");
        }
    }

    // ========== 데이터 반환 ==========
    public GemBundle GetData()
    {
        return bundleData;
    }

    // ========== 선택 상태 설정 (외곽선 표시) ==========
    public void SetSelected(bool isSelected)
    {
        if(Outline != null)
        {
            Outline.enabled = isSelected;
        }
    }

    // ========== 클릭 이벤트 ==========
    public void OnClick()
    {
        OnClickBundle?.Invoke(this);
    }
}