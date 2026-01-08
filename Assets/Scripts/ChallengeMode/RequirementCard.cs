using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Linq;

public class RequirementCard : MonoBehaviour
{
    public TextMeshProUGUI RequirementText;
    public TextMeshProUGUI RewardTimeText;
    public TextMeshProUGUI RewardGemText;
    public Button SelectButton;

    private ChallengeRequirement data;
    private Action<ChallengeRequirement> onSelect;

    public void Setup(ChallengeRequirement req, Action<ChallengeRequirement> callback)
    {
        data = req;
        onSelect = callback;

        RequirementText.text = req.GetDescription();
        RewardTimeText.text = $"+{req.RewardTime}s";
        RewardGemText.text = $"+{req.RewardGemCount}";

        SelectButton.onClick.RemoveAllListeners();
        SelectButton.onClick.AddListener(() => onSelect?.Invoke(data));
    }
}