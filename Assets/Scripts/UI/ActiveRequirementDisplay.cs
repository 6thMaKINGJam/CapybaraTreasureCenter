using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 현재 선택된 챌린지 조건을 화면에 표시하는 UI
/// </summary>
public class ActiveRequirementDisplay : MonoBehaviour
{
    [Header("UI 요소")]

    public Image LeftGemIcon;
    public TextMeshProUGUI OperatorText;
    public Image RightGemIcon; // 오른쪽이 보석일 때
    public TextMeshProUGUI RightValueText; // 오른쪽이 숫자일 때
    
    [Header("데이터베이스")]
    public GemSpriteDatabase SpriteDatabase; // Inspector에서 할당

    /// <summary>
    /// 조건 업데이트
    /// </summary>
    public void UpdateRequirement(ChallengeRequirement req)
    {
        if (req == null)
        {
            // 조건이 없으면 전체 패널 비활성화
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        if (SpriteDatabase == null)
        {
            Debug.LogError("[ActiveRequirementDisplay] SpriteDatabase가 할당되지 않았습니다!");
            return;
        }

        // 1. 왼쪽 보석 이미지
        Sprite leftSprite = SpriteDatabase.GetSprite(req.LeftType, 1);
        if (leftSprite != null && LeftGemIcon != null)
        {
            LeftGemIcon.sprite = leftSprite;
            LeftGemIcon.enabled = true;
        }

        // 2. 연산자
        if (OperatorText != null)
        {
            OperatorText.text = GetOperatorSymbol(req.Op);
        }

        // 3. 오른쪽 항 (보석 vs 숫자)
        if (req.IsValueComparison)
        {
            // 숫자 비교
            if (RightValueText != null)
            {
                RightValueText.text = req.RightValue.ToString();
                RightValueText.gameObject.SetActive(true);
            }
            if (RightGemIcon != null)
            {
                RightGemIcon.gameObject.SetActive(false);
            }
        }
        else
        {
            // 보석 비교
            Sprite rightSprite = SpriteDatabase.GetSprite(req.RightType, 1);
            if (rightSprite != null && RightGemIcon != null)
            {
                RightGemIcon.sprite = rightSprite;
                RightGemIcon.gameObject.SetActive(true);
            }
            if (RightValueText != null)
            {
                RightValueText.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 조건 숨기기
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private string GetOperatorSymbol(ComparisonOperator op)
    {
        switch (op)
        {
            case ComparisonOperator.Equal: return "=";
            case ComparisonOperator.LessThan: return "<";
            case ComparisonOperator.GreaterThan: return ">";
            default: return "?";
        }
    }
}