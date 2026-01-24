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
    public TMP_Text MessageText;
    public TMP_Text AppleCountText; // 현재 보유 사과
    public Button PurchaseButton; // 사과로 구매
    public Button AdButton; // 광고로 구매
    public Button CancelButton;
    
    private Action onPurchaseSuccess;
    private string itemName;
    
    public void Setup(string itemName, Action purchaseSuccessCallback)
    {
        this.itemName = itemName;
        onPurchaseSuccess = purchaseSuccessCallback;
        
        MessageText.text = $"사과로 {itemName}를 구매하시겠습니까?";
        
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
        
        // 버튼 중복 클릭 방지
        PurchaseButton.interactable = false;

        // [수정] 콜백 내부에서 팝업이 확실히 닫히도록 로직 강화
        AppleManager.Instance.TryPurchaseItem(itemName, () =>
        {
            onPurchaseSuccess?.Invoke();
            Destroy(gameObject); // 기능 실행 후 팝업 제거
        });

        // 만약 사과가 부족하다면 (TryPurchaseItem이 false를 반환할 경우를 대비)
        if (AppleManager.Instance.GetAppleCount() <= 0)
        {
            MessageText.text = "사과가 부족합니다카피!\n광고를 시청하시겠습니까?";
            PurchaseButton.gameObject.SetActive(false);
        }
    }
    
    private void OnClickAd()
    {
        AdButton.interactable = false; // 광고 버튼 비활성화

        AdManager.Instance.ShowRewardedAd((success) =>
        {
            if(success)
            {
                // 1. 광고 보상 사과 지급
                AppleManager.Instance.AddApplesFromAd();
                UpdateAppleUI();
                
                // 2. [수정] 사과 획득 후 즉시 해당 아이템 구매 로직 재시도
                AppleManager.Instance.TryPurchaseItem(itemName, () =>
                {
                    onPurchaseSuccess?.Invoke();
                    if (this != null && gameObject != null)
                        Destroy(gameObject);
                });
            }
            else
            {
                // 광고 실패 시 다시 버튼 활성화
                AdButton.interactable = true;
            }
        });
    }
}