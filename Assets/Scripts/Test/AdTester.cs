using TMPro;
using UnityEngine;
using UnityEngine.UI; // UI 텍스트 갱신용

public class AdTester : MonoBehaviour
{
    // 테스트 결과를 눈으로 확인하기 위한 텍스트 (없으면 콘솔로만 확인)
    public TextMeshProUGUI statusText; 
    
    // 현재 보유 골드 (테스트용)
    private int currentGold = 0;

    private void Start()
    {
        UpdateUI("버튼을 눌러 광고를 테스트하세요.");
    }

    // ★ 이 함수를 버튼의 OnClick에 연결하세요
    public void OnClickShowAd()
    {
        Debug.Log("[AdTester] 광고 보기 버튼 클릭함!");
        UpdateUI("광고 요청 중...");

        // AdManager에게 광고 요청
        AdManager.Instance.ShowRewardedAd(OnAdComplete);
    }

    // 광고가 끝나면 호출될 콜백 함수
    private void OnAdComplete(bool isSuccess)
    {
        if (isSuccess)
        {
            // 성공 시 보상 지급 로직
            currentGold += 100; // 100골드 추가
            Debug.Log($"[AdTester] 광고 시청 성공! 100골드 지급됨. (현재: {currentGold})");
            UpdateUI($"성공! 100G 획득. (총 {currentGold}G)");
        }
        else
        {
            // 실패 또는 취소 시
            Debug.Log("[AdTester] 광고 시청 실패 또는 도중에 닫음.");
            UpdateUI("광고 시청 실패/취소");
        }
    }

    // UI 텍스트 갱신 헬퍼
    private void UpdateUI(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
}