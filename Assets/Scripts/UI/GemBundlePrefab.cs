// GemBundlePrefab.cs 수정
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
    private Button button;
    
    public event Action<GemBundlePrefab> OnClickBundle;
    
    // ========== 초기화 ==========
    void Awake()
    {
        button = GetComponent<Button>();
        if(button != null)
        {
            button.onClick.AddListener(OnClick);
        }
        else
        {
            Debug.LogError("[GemBundlePrefab] Button 컴포넌트를 찾을 수 없습니다!");
        }
    }
    
    void OnDestroy()
    {
        // 메모리 누수 방지
        if(button != null)
        {
            button.onClick.RemoveListener(OnClick);
        }
    }
    
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
    
    // Button onClick에서 호출됨
    private void OnClick()
    {
        OnClickBundle?.Invoke(this);
    }
}