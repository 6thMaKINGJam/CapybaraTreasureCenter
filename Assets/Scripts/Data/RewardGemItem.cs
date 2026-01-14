using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 보상 보석 UI 아이템 (+ [보석이미지] 숫자)
/// </summary>
public class RewardGemItem : MonoBehaviour
{
    [Header("UI 요소")]

    public Image GemIcon; // 보석 이미지
    public TextMeshProUGUI CountText; // 개수

    /// <summary>
    /// 보상 보석 데이터 설정
    /// </summary>
    public void Setup(GemType gemType, int count, GemSpriteDatabase database)
    {
        if (database == null)
        {
            Debug.LogError("[RewardGemItem] SpriteDatabase가 null입니다!");
            return;
        }

      

        // 보석 이미지 설정 (1개짜리 스프라이트 사용)
        if (GemIcon != null)
        {
            Sprite sprite = database.GetSprite(gemType, 1);
            if (sprite != null)
            {
                GemIcon.sprite = sprite;
            }
            else
            {
                Debug.LogError($"[RewardGemItem] {gemType} 타입의 1개 스프라이트를 찾을 수 없습니다!");
            }
        }

        // 개수 설정
        if (CountText != null)
        {
            CountText.text = count.ToString();
        }
    }
}