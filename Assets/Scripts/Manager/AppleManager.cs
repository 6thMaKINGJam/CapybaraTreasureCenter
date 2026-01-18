using UnityEngine;
using System;

/// <summary>
/// 사과 화폐 시스템 관리
/// - 별 클리어 보상
/// - 광고 시청 보상
/// - 아이템 구매
/// </summary>
public class AppleManager : MonoBehaviour
{
    public static AppleManager Instance { get; private set; }
    
    /// <summary>
    /// 사과 개수 변경 시 발생하는 이벤트 (UI 업데이트용)
    /// </summary>
    public event Action<int> OnAppleCountChanged;
    
    private ProgressData progressData;
    
    void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadData();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void LoadData()
    {
        progressData = SaveManager.LoadData<ProgressData>("ProgressData");
        Debug.Log($"[AppleManager] 초기화 완료. 현재 사과: {progressData.TotalApples}개");
    }
    
    /// <summary>
    /// 현재 보유 사과 개수 반환
    /// </summary>
    public int GetAppleCount()
    {
        return progressData.TotalApples;
    }
    
    /// <summary>
    /// 레벨 클리어 시 별 개수에 따른 사과 지급
    /// 공식: 사과 = 별개수 - 1 (별1=사과0, 별2=사과1, 별3=사과2)
    /// 재플레이: 증가분만 지급
    /// </summary>
    public int AddApplesFromStars(int oldStars, int newStars)
    {
        // 공식: 별1(0개), 별2(1개), 별3(2개)
        int oldApples = Mathf.Max(0, oldStars - 1);
        int newApples = Mathf.Max(0, newStars - 1);
        
        // 기존 기록보다 더 많은 별을 땄을 때만 차액 지급
        int earnedApples = Mathf.Max(0, newApples - oldApples);
        
        if(earnedApples > 0)
        {
            // 1. 메모리 데이터 갱신
            progressData.AddApples(earnedApples); 
            
            // 2. 파일에 즉시 저장 (ProgressData 내부의 Save 호출 확인)
            SaveManager.Save(progressData, "ProgressData"); 
            
            // 3. UI 업데이트 이벤트 알림
            OnAppleCountChanged?.Invoke(progressData.TotalApples);
            
            Debug.Log($"[AppleManager] 사과 +{earnedApples}개 획득! (현재 총 {progressData.TotalApples}개)");
        }
        
        // ✅ 실제 지급된 사과 개수 반환
        return earnedApples;
    }
    
    /// <summary>
    /// 광고 시청 보상 (사과 1개)
    /// </summary>
    public void AddApplesFromAd()
    {
        progressData.AddApples(1);
        OnAppleCountChanged?.Invoke(progressData.TotalApples);
        
        Debug.Log($"[AppleManager] 광고 시청으로 사과 +1개! (총 {progressData.TotalApples}개)");
    }
    
    /// <summary>
    /// 아이템 구매 시도 (사과 1개 차감)
    /// </summary>
    /// <param name="itemName">아이템 이름 (로그용)</param>
    /// <param name="onSuccess">구매 성공 시 콜백</param>
    /// <returns>구매 성공 여부</returns>
    public bool TryPurchaseItem(string itemName, Action onSuccess)
    {
        if(progressData.SpendApples(1))
        {
            OnAppleCountChanged?.Invoke(progressData.TotalApples);
            onSuccess?.Invoke();
            
            Debug.Log($"[AppleManager] {itemName} 구매 완료! (사과 -1, 총 {progressData.TotalApples}개)");
            return true;
        }
        
        Debug.Log($"[AppleManager] 사과 부족! 현재: {progressData.TotalApples}개");
        return false;
    }
    
    /// <summary>
    /// 시간추가 구매 시도
    /// </summary>
    /// <param name="onSuccess">구매 성공 시 콜백</param>
    /// <param name="onNoApples">사과 부족 시 콜백</param>
    public void TryPurchaseTimeAdd(Action onSuccess, Action onNoApples)
    {
        if(progressData.TotalApples >= 1)
        {
            if(progressData.SpendApples(1))
            {
                OnAppleCountChanged?.Invoke(progressData.TotalApples);
                onSuccess?.Invoke();
                Debug.Log($"[AppleManager] 시간추가 구매! (사과 -1, 총 {progressData.TotalApples}개)");
            }
        }
        else
        {
            Debug.Log("[AppleManager] 사과 부족 → 광고 제안");
            onNoApples?.Invoke();
        }
    }
}