using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class RequirementCard : MonoBehaviour
{
    [Header("조건 표시 UI")]
    public Image LeftGemIcon;
    public TextMeshProUGUI OperatorText;
    public Image RightGemIcon; // 오른쪽이 보석일 때
    public TextMeshProUGUI RightValueText; // 오른쪽이 숫자일 때
    
    [Header("보상 표시 UI")]

    public Transform RewardGemPanel; // HorizontalLayoutGroup이 붙은 부모
    public GameObject RewardGemItemPrefab; // RewardGemItem 프리팹
    
    [Header("버튼")]
    public Button SelectButton;
    
    [Header("데이터베이스")]
    public GemSpriteDatabase SpriteDatabase; // Inspector에서 할당

    private ChallengeRequirement data;
    private Action<ChallengeRequirement> onSelect;

    public void Setup(ChallengeRequirement req, Action<ChallengeRequirement> callback)
    {
        data = req;
        onSelect = callback;

        // 조건 표시
        SetupCondition(req);
        
        // 보상 시간 표시
      
        
        // 보상 보석 표시
        SetupRewardGems(req);

        // 버튼 클릭 이벤트
        SelectButton.onClick.RemoveAllListeners();
        SelectButton.onClick.AddListener(() => onSelect?.Invoke(data));
    }

    private void SetupCondition(ChallengeRequirement req)
    {
        if (SpriteDatabase == null)
        {
            Debug.LogError("[RequirementCard] SpriteDatabase가 할당되지 않았습니다!");
            return;
        }

        // 1. 왼쪽 보석 이미지 설정
        Sprite leftSprite = SpriteDatabase.GetSprite(req.LeftType, 1);
        if (leftSprite != null && LeftGemIcon != null)
        {
            LeftGemIcon.sprite = leftSprite;
            LeftGemIcon.enabled = true;
        }

        // 2. 연산자 텍스트 설정
        if (OperatorText != null)
        {
            OperatorText.text = GetOperatorSymbol(req.Op);
        }

        // 3. 오른쪽 항 설정 (보석 vs 숫자)
        if (req.IsValueComparison)
        {
            // 숫자 비교 → RightValueText 활성화, RightGemIcon 비활성화
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
            // 보석 비교 → RightGemIcon 활성화, RightValueText 비활성화
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

    private void SetupRewardGems(ChallengeRequirement req)
    {
        // 기존 보상 아이템 제거
        foreach (Transform child in RewardGemPanel)
        {
            Destroy(child.gameObject);
        }

        if (req.RewardGemTypes == null || req.RewardGemCounts == null)
        {
            Debug.LogWarning("[RequirementCard] 보상 보석 데이터가 없습니다.");
            return;
        }

        // 보상 보석 아이템 동적 생성
        for (int i = 0; i < req.RewardGemTypes.Length; i++)
        {
            GameObject itemObj = Instantiate(RewardGemItemPrefab, RewardGemPanel);
            RewardGemItem item = itemObj.GetComponent<RewardGemItem>();
            
            if (item != null)
            {
                item.Setup(req.RewardGemTypes[i], req.RewardGemCounts[i], SpriteDatabase);
            }
            else
            {
                Debug.LogError("[RequirementCard] RewardGemItem 컴포넌트를 찾을 수 없습니다!");
            }
        }
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