using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 아이템 소진 시 사과 구매 팝업
/// </summary>
public class ItemPurchasePopup : MonoBehaviour
{
    [Header("UI 요소")]
    public TextMeshProUGUI MessageText;
    public TextMeshProUGUI AppleCountText; // 현재 보유 사과
    public Button PurchaseButton; // 사과로 구매
    public Button AdButton; // 광고로 구매
    public Button CancelButton;
    
    private Action onPurchaseSuccess;
    private string itemName;
    
    public void Setup(string itemName, Action purchaseSuccessCallback)
    {
        this.itemName = itemName;
        onPurchaseSuccess = purchaseSuccessCallback;
        
        MessageText.text = $"{itemName} 횟수를 모두 사용했습니다.\n사과로 구매하시겠습니까?";
        
        UpdateAppleUI();
        
        PurchaseButton.onClick.AddListener(OnClickPurchase);
        AdButton.onClick.AddListener(OnClickAd);
        CancelButton.onClick.AddListener(() => Destroy(gameObject));
    }
    
    private void UpdateAppleUI()
    {
        if(AppleManager.Instance != null)
        {
            AppleCountText.text = $"보유 사과: {AppleManager.Instance.GetAppleCount()}개";
        }
    }
    
    private void OnClickPurchase()
    {
        if(AppleManager.Instance == null) return;
        
        bool success = AppleManager.Instance.TryPurchaseItem(itemName, () =>
        {
            onPurchaseSuccess?.Invoke();
            Destroy(gameObject);
        });
        
        if(!success)
        {
            // 사과 부족 → 광고 제안으로 자동 전환
            MessageText.text = "사과가 부족합니다.\n광고를 시청하시겠습니까?";
            PurchaseButton.gameObject.SetActive(false);
        }
    }
    
    private void OnClickAd()
    {
        AdManager.Instance.ShowRewardedAd((success) =>
        {
            if(success)
            {
                AppleManager.Instance.AddApplesFromAd();
                UpdateAppleUI();
                
                // 사과 획득 후 자동 구매
                AppleManager.Instance.TryPurchaseItem(itemName, () =>
                {
                    onPurchaseSuccess?.Invoke();
                    Destroy(gameObject);
                });
            }
        });
    }
}