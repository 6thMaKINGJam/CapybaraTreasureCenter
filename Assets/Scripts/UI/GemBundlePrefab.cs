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
    
    /// <summary>
    /// 외부에서 호출 가능한 상태 변경 함수
    /// true: 투명한 빈 공간(Placeholder) 모드
    /// false: 정상 모드
    /// </summary>
    public void SetPlaceholderState(bool isPlaceholder)
    {
        // 만약의 경우를 대비해 null 체크
        if(canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

        if (isPlaceholder)
        {
            // 투명하고 터치 안 되게 (공간은 유지함)
            canvasGroup.alpha = 0f; 
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
        else
        {
            // 다시 보이게 복구
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
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