using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Coffee.UIEffects;
/// <summary>
/// 보석 종류별 남은 개수를 표시하는 UI 아이템
/// GemCountPanelManager에서 사용
/// </summary>
public class GemCountItem : MonoBehaviour
{
    [Header("UI 요소")]
    public Image GemIcon;
    public TextMeshProUGUI CountText;
    
    [Header("스프라이트 데이터베이스")]
    public GemSpriteDatabase SpriteDatabase;
    
    private GemType gemType;
    private Color satisfiedColor = new Color(0x0A / 255f, 0xFF / 255f, 0x00 / 255f); // #0AFF00
    private Color normalColor = Color.white; // #FFFFFF
    
    
    /// <summary>
    /// 보석 종류와 초기 개수 설정
    /// </summary>
    public void SetData(GemType type, int count, GemSpriteDatabase database)
    {
        gemType = type;
        SpriteDatabase = database;
        
        // 보석 1개짜리 스프라이트로 종류만 표시
        if (SpriteDatabase != null && GemIcon != null)
        {
            Sprite sprite = SpriteDatabase.GetSprite(type, 1);
            if (sprite != null)
            {
                GemIcon.sprite = sprite;
            }
            else
            {
                Debug.LogError($"[GemCountItem] {type} 타입의 1개 스프라이트를 찾을 수 없습니다!");
            }
        }
        
        UpdateCount(count);
    }
    
    /// <summary>
    /// 개수만 업데이트 (보석 선택/취소/되돌리기 시 호출)
    /// </summary>
    public void UpdateCount(int count)
    {
        if (CountText == null)
        {
            Debug.LogError("[GemCountItem] CountText가 할당되지 않았습니다!");
            return;
        }
        
        CountText.text = count.ToString();
        
        
    }

    
    public GemType GetGemType() => gemType;
}