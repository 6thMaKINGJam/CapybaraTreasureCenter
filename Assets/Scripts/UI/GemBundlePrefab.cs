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
    private CanvasGroup canvasGroup;
    
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
        
        canvasGroup = GetComponent<CanvasGroup>();
    }
    
    void OnDestroy()
    {
        // 메모리 누수 방지
        if(button != null)
        {
            button.onClick.RemoveListener(OnClick);
        }
    }
    
    // ★ 수정: null 체크 추가
    public void SetData(GemBundle data)
    {
        bundleData = data;
        
        // ★ Placeholder인 경우 (data == null)
        if(data == null)
        {
            // 아이콘 숨기기 또는 투명하게
            if(GemIcon != null)
            {
                GemIcon.enabled = false; // 또는 sprite = null
            }
            return;
        }
        
        // ★ 일반 번들인 경우
        if(SpriteDatabase == null)
        {
            Debug.LogError("[GemBundlePrefab] SpriteDatabase가 할당되지 않았습니다!");
            return;
        }
        
        Sprite sprite = SpriteDatabase.GetSprite(data.GemType, data.GemCount);
        
        if(sprite != null && GemIcon != null)
        {
            GemIcon.sprite = sprite;
            GemIcon.enabled = true; // 다시 활성화
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
        // ★ 추가: Placeholder 클릭 방지
        if(bundleData == null) return;
        
        OnClickBundle?.Invoke(this);
    }
}