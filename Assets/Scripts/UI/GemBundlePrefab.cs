using UnityEngine;
using UnityEngine.UI;
using System;

public class GemBundlePrefab : MonoBehaviour
{
    public Image GemIcon;
    public Outline Outline;
    
    [Header("스프라이트 데이터베이스")]
    [Tooltip("Resources/GemSpriteDatabase를 할당하세요")]
    public GemSpriteDatabase SpriteDatabase;
    
    private GemBundle bundleData;
    public event Action<GemBundlePrefab> OnClickBundle;
    
    public void SetData(GemBundle data)
    {
        bundleData = data;
        
        if(SpriteDatabase == null)
        {
            Debug.LogError("[GemBundlePrefab] SpriteDatabase가 할당되지 않았습니다!");
            return;
        }
        
        Sprite sprite = SpriteDatabase.GetSprite(data.GemType, data.GemCount);
        if(sprite != null)
        {
            GemIcon.sprite = sprite;
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