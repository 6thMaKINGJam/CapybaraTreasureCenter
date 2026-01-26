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

    // ✅ 상수 설정
    private const int MAX_REGEN_APPLES = 5;       // 자동 충전 최대치
    private const int REGEN_TIME_MINUTES = 20;    // 충전 간격 (20분)
    
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
        RefreshAppleRegen();
        Debug.Log($"[AppleManager] 초기화 완료. 현재 사과: {progressData.TotalApples}개");
    }

    private void Update()
    {//좌연주 수정필요
        // 매 프레임마다 충전 시간이 되었는지 체크 (레벨 100 클리어 시에만)
        if (progressData.isLevel100Completed)
        {
            RefreshAppleRegen();
        }
    }

    /// <summary>
    /// 사과 자동 충전 로직 (오프라인/실시간 공용)
    /// </summary>
    public void RefreshAppleRegen()
    {//수정 필요 좌연주
        // 레벨 100 미만이거나 이미 사과가 5개 이상이면 시간만 갱신하고 리턴
        if (progressData.LastClearedLevel < 100 || progressData.TotalApples >= MAX_REGEN_APPLES)
        {
            progressData.LastAppleUpdateTime = DateTime.Now.ToString();
            return;
        }

        DateTime lastTime;
        if (!DateTime.TryParse(progressData.LastAppleUpdateTime, out lastTime))
        {
            lastTime = DateTime.Now;
        }

        TimeSpan elapsed = DateTime.Now - lastTime;
        int applesToAdd = (int)(elapsed.TotalMinutes / REGEN_TIME_MINUTES);

        if (applesToAdd > 0)
        {
            int currentApples = progressData.TotalApples;
            // 5개를 넘지 않도록 계산
            int newApples = Mathf.Min(MAX_REGEN_APPLES, currentApples + applesToAdd);
            int actualAdded = newApples - currentApples;

            if (actualAdded > 0)
            {
                progressData.TotalApples = newApples;
                // 남은 자투리 시간은 보존하여 다음 충전에 반영
                DateTime nextLastTime = lastTime.AddMinutes(actualAdded * REGEN_TIME_MINUTES);
                progressData.LastAppleUpdateTime = nextLastTime.ToString();
                
                SaveManager.Save(progressData, "ProgressData");
                OnAppleCountChanged?.Invoke(progressData.TotalApples);
            }
        }
    }

    /// <summary>
    /// 다음 충전까지 남은 시간을 초 단위로 반환
    /// </summary>
    public float GetRemainingRegenSeconds()
    {//수정필요 좌연주
        if (!progressData.isLevel100Completed || progressData.TotalApples >= MAX_REGEN_APPLES)
            return 0;

        DateTime lastTime;
        DateTime.TryParse(progressData.LastAppleUpdateTime, out lastTime);
        
        DateTime nextUpdateTime = lastTime.AddMinutes(REGEN_TIME_MINUTES);
        return (float)(nextUpdateTime - DateTime.Now).TotalSeconds;
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
    public void TryPurchaseItem(string itemName, Action onSuccess, Action onNoApples)
    {
        int currentApples = GetAppleCount();
        if (currentApples >= 1)
        {
            // 사과 1개 차감 (기존 로직 사용)
            progressData.SpendApples(1); 
            onSuccess?.Invoke();
            Debug.Log($"[AppleManager] {itemName} 구매 성공");
        }
        else
        {
            // 사과 부족 시 콜백 실행
            onNoApples?.Invoke();
        }
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