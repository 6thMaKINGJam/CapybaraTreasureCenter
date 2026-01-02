using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 힌트 시스템 전담 매니저
/// 다양한 전략(탐욕, 백트래킹)을 시도하여 현재 상자를 채울 수 있는 조합을 찾습니다.
/// </summary>
public class HintManager: MonoBehaviour
{
    // ========== 선택 모드 ==========
    private enum SelectionMode 
    { 
        SmallFirst,  // 작은 것부터
        LargeFirst   // 큰 것부터
    }
    
    // ========== 풀 정보 클래스 ==========
    private class PoolInfo
    {
        public List<GemBundle> AvailableBundles;
        public int MaxSelectCount;
    }

    private class WorkingPoolInfo
    {
        public List<GemBundle> AvailableBundles;
        public int RemainingSelectCount;
    }
    
    // ========== 메인 진입점 ==========
    /// <summary>
    /// 현재 상자를 채울 수 있는 힌트 조합을 찾습니다.
    /// </summary>
    /// <param name="currentBox">현재 채워야 할 상자</param>
    /// <param name="bundlePool">전체 번들 풀</param>
    /// <param name="currentDisplayBundles">현재 화면에 표시된 12개 번들</param>
    /// <param name="remainingBoxes">남은 상자 개수</param>
    /// <param name="gemTypeCount">사용 중인 보석 종류 수</param>
    /// <returns>힌트 번들 리스트 (실패 시 null)</returns>
    public List<GemBundle> FindHintCombination(
        Box currentBox,
        List<GemBundle> bundlePool,
        List<GemBundle> currentDisplayBundles,
        int remainingBoxes,
        int gemTypeCount)
    {
        int requiredAmount = currentBox.RequiredAmount;
        
        // 선택 가능 풀 생성
        var pools = BuildSelectablePools(
            bundlePool, 
            currentDisplayBundles, 
            remainingBoxes, 
            requiredAmount, 
            gemTypeCount
        );
        
        if(pools == null)
        {
            Debug.Log("[HintManager] 선택 가능 풀 생성 실패 (이미 글렀음)");
            return null;
        }
        
        // ===== 전략 1: 작은 것부터 =====
        Debug.Log("[HintManager] 전략 1 시도: 작은 것부터");
        var result = TryGreedyStrategy(pools, requiredAmount, gemTypeCount, SelectionMode.SmallFirst);
        if(result != null) 
        {
            Debug.Log("[HintManager] ✅ 전략 1 성공!");
            return result;
        }
        
        // ===== 전략 2: 큰 것부터 =====
        Debug.Log("[HintManager] 전략 2 시도: 큰 것부터");
        result = TryGreedyStrategy(pools, requiredAmount, gemTypeCount, SelectionMode.LargeFirst);
        if(result != null)
        {
            Debug.Log("[HintManager] ✅ 전략 2 성공!");
            return result;
        }
        
        // ===== 전략 3: 백트래킹 (완전 탐색) =====
        Debug.Log("[HintManager] 전략 3 시도: 백트래킹");
        result = TryBacktracking(pools, requiredAmount, gemTypeCount);
        if(result != null)
        {
            Debug.Log("[HintManager] ✅ 전략 3 성공!");
            return result;
        }
        
        // ===== 모든 전략 실패 =====
        Debug.Log("[HintManager] ❌ 모든 전략 실패 - 이미 글렀음");
        return null;
    }
    
    // ========== 선택 가능 풀 생성 ==========
    private Dictionary<GemType, PoolInfo> BuildSelectablePools(
        List<GemBundle> bundlePool,
        List<GemBundle> currentDisplayBundles,
        int remainingBoxes,
        int requiredAmount,
        int gemTypeCount)
    {
        var pools = new Dictionary<GemType, PoolInfo>();
        
        int maxBundleGemCount = requiredAmount - (gemTypeCount - 1);
        
        Debug.Log($"[HintManager] maxBundleGemCount: {maxBundleGemCount} (요구량 {requiredAmount} - 색 {gemTypeCount - 1})");
        
        for(int i = 0; i < gemTypeCount; i++)
        {
            GemType type = (GemType)i;
            
            // 해당 색의 모든 번들
            List<GemBundle> allBundles = bundlePool
                .Where(b => b != null && b.GemType == type)
                .ToList();
            
            int totalBundles = allBundles.Count;
            
            // 여유분 계산
            int surplus = totalBundles - remainingBoxes;
            
            if(surplus < 0)
            {
                Debug.LogError($"[HintManager] {type} 색 번들 부족: {totalBundles}개 < {remainingBoxes}상자");
                return null;
            }
            
            // 화면에 표시 중인 번들만 선택 가능
            List<GemBundle> availableBundles = currentDisplayBundles
                .Where(b => b != null && b.GemType == type && b.GemCount <= maxBundleGemCount)
                .ToList();
            
            if(availableBundles.Count == 0)
            {
                Debug.LogWarning($"[HintManager] {type} 색의 선택 가능 번들이 화면에 없음");
            }
            
            pools[type] = new PoolInfo
            {
                AvailableBundles = availableBundles,
                MaxSelectCount = surplus + 1
            };
            
            Debug.Log($"[HintManager] {type} 색: 총 {totalBundles}개, 화면 {availableBundles.Count}개, MaxSelect {surplus + 1}");
        }
        
        return pools;
    }
    
    // ========== 작업용 풀 복사 ==========
    private Dictionary<GemType, WorkingPoolInfo> CreateWorkingPools(Dictionary<GemType, PoolInfo> originalPools)
    {
        var workingPools = new Dictionary<GemType, WorkingPoolInfo>();
        
        foreach(var kvp in originalPools)
        {
            workingPools[kvp.Key] = new WorkingPoolInfo
            {
                AvailableBundles = new List<GemBundle>(kvp.Value.AvailableBundles),
                RemainingSelectCount = kvp.Value.MaxSelectCount
            };
        }
        
        return workingPools;
    }
    
    // ========== 전략 1 & 2: 탐욕 알고리즘 ==========
    private List<GemBundle> TryGreedyStrategy(
        Dictionary<GemType, PoolInfo> pools, 
        int requiredAmount,
        int gemTypeCount,
        SelectionMode mode)
    {
        // 작업용 복사본
        var workingPools = CreateWorkingPools(pools);
        List<GemBundle> selected = new List<GemBundle>();
        
        // ===== 1단계: 각 색 1개씩 (모드에 따라 정렬) =====
        int total = 0;
        
        for(int i = 0; i < gemTypeCount; i++)
        {
            GemType type = (GemType)i;
            
            if(!workingPools.ContainsKey(type) || 
               workingPools[type].AvailableBundles.Count == 0)
            {
                Debug.Log($"[Greedy-{mode}] {type} 색 선택 불가");
                return null;
            }
            
            // 모드에 따라 정렬
            var sortedBundles = SortByMode(workingPools[type].AvailableBundles, mode);
            var bundle = sortedBundles[0];
            
            selected.Add(bundle);
            total += bundle.GemCount;
            
            workingPools[type].AvailableBundles.Remove(bundle);
            workingPools[type].RemainingSelectCount--;
            
            Debug.Log($"[Greedy-{mode}] 1단계: {type} {bundle.GemCount}개 선택, 총 {total}");
        }
        
        // 초과 체크
        if(total > requiredAmount)
        {
            Debug.Log($"[Greedy-{mode}] 1단계에서 초과: {total} > {requiredAmount}");
            return null;
        }
        
        // 정확히 맞음
        if(total == requiredAmount)
        {
            Debug.Log($"[Greedy-{mode}] 1단계에서 완성!");
            return selected;
        }
        
        // ===== 2단계: 부족분 채우기 =====
        HashSet<GemType> triedColors = new HashSet<GemType>();
        int loopCount = 0;
        int maxLoops = 100;
        
        while(total < requiredAmount)
        {
            loopCount++;
            if(loopCount > maxLoops)
            {
                Debug.LogError($"[Greedy-{mode}] 무한 루프 감지!");
                return null;
            }
            
            int remaining = requiredAmount - total;
            
            // 선택 가능한 색 찾기
            var availableColors = workingPools
                .Where(p => p.Value.RemainingSelectCount > 0 
                         && p.Value.AvailableBundles.Count > 0
                         && !triedColors.Contains(p.Key))
                .ToList();
            
            if(availableColors.Count == 0)
            {
                Debug.Log($"[Greedy-{mode}] 선택 가능 색 없음");
                return null;
            }
            
            // 번들 개수가 많은 색 우선
            int maxBundleCount = availableColors.Max(p => p.Value.AvailableBundles.Count);
            var topColors = availableColors
                .Where(p => p.Value.AvailableBundles.Count == maxBundleCount)
                .ToList();
            
            GemBundle selectedBundle = null;
            GemType selectedColor = GemType.Red;
            
            // 통합 검색 (여러 색 동점일 때)
            if(topColors.Count > 1)
            {
                List<(GemBundle bundle, GemType color)> allCandidates = new List<(GemBundle, GemType)>();
                
                foreach(var colorInfo in topColors)
                {
                    GemType color = colorInfo.Key;
                    var sortedBundles = SortByMode(workingPools[color].AvailableBundles, mode);
                    
                    foreach(var bundle in sortedBundles)
                    {
                        allCandidates.Add((bundle, color));
                    }
                }
                
                // 정확히 맞는 번들 찾기
                var exactMatch = allCandidates
                    .FirstOrDefault(item => item.bundle.GemCount == remaining);
                
                if(exactMatch.bundle != null)
                {
                    selectedBundle = exactMatch.bundle;
                    selectedColor = exactMatch.color;
                }
                else
                {
                    // 없으면 모드에 따라 선택
                    var candidates = allCandidates
                        .Where(item => item.bundle.GemCount <= remaining)
                        .ToList();
                    
                    if(candidates.Count > 0)
                    {
                        var sorted = mode == SelectionMode.SmallFirst
                            ? candidates.OrderBy(c => c.bundle.GemCount)
                            : candidates.OrderByDescending(c => c.bundle.GemCount);
                        
                        var chosen = sorted.First();
                        selectedBundle = chosen.bundle;
                        selectedColor = chosen.color;
                    }
                }
            }
            else
            {
                // 단일 색
                selectedColor = topColors.First().Key;
                var sortedBundles = SortByMode(workingPools[selectedColor].AvailableBundles, mode);
                
                // 정확히 맞는 번들
                selectedBundle = sortedBundles
                    .FirstOrDefault(b => b.GemCount == remaining);
                
                if(selectedBundle == null)
                {
                    // 없으면 모드에 따라 선택
                    selectedBundle = sortedBundles
                        .Where(b => b.GemCount <= remaining)
                        .FirstOrDefault();
                }
            }
            
            // 선택 실패 시 색 제외
            if(selectedBundle == null)
            {
                foreach(var colorInfo in topColors)
                {
                    triedColors.Add(colorInfo.Key);
                }
                Debug.Log($"[Greedy-{mode}] 조건 맞는 번들 없음, {topColors.Count}개 색 제외");
                continue;
            }
            
            // 선택 성공
            selected.Add(selectedBundle);
            total += selectedBundle.GemCount;
            
            workingPools[selectedColor].AvailableBundles.Remove(selectedBundle);
            workingPools[selectedColor].RemainingSelectCount--;
            
            triedColors.Clear();
            
            Debug.Log($"[Greedy-{mode}] 2단계: {selectedColor} {selectedBundle.GemCount}개 선택, 총 {total}/{requiredAmount}");
        }
        
        Debug.Log($"[Greedy-{mode}] 조합 완성! 총 {selected.Count}개 번들");
        return selected;
    }
    
    // ========== 전략 3: 백트래킹 (완전 탐색) ==========
    private List<GemBundle> TryBacktracking(
        Dictionary<GemType, PoolInfo> pools,
        int requiredAmount,
        int gemTypeCount)
    {
        // 모든 선택 가능 번들 수집
        List<GemBundle> allCandidates = new List<GemBundle>();
        foreach(var pool in pools.Values)
        {
            allCandidates.AddRange(pool.AvailableBundles);
        }
        
        Debug.Log($"[Backtracking] 후보 번들 {allCandidates.Count}개, 목표 {requiredAmount}");
        
        List<GemBundle> result = new List<GemBundle>();
        
        // 백트래킹 시작
        if(BacktrackSearch(allCandidates, pools, requiredAmount, gemTypeCount, 0, result, new List<GemBundle>(), 0))
        {
            Debug.Log($"[Backtracking] ✅ 성공! {result.Count}개 번들 선택");
            return result;
        }
        
        Debug.Log("[Backtracking] ❌ 실패 - 조합 없음");
        return null;
    }
    
    // ========== 재귀 백트래킹 ==========
    private bool BacktrackSearch(
        List<GemBundle> candidates,
        Dictionary<GemType, PoolInfo> pools,
        int target,
        int gemTypeCount,
        int currentTotal,
        List<GemBundle> result,
        List<GemBundle> current,
        int startIndex)
    {
        // ===== 성공 조건 =====
        if(currentTotal == target)
        {
            // 모든 색 포함 검증
            if(ValidateAllColors(current, gemTypeCount))
            {
                result.Clear();
                result.AddRange(current);
                return true;
            }
            return false;
        }
        
        // ===== 초과 =====
        if(currentTotal > target) return false;
        
        // ===== 모든 번들 시도 =====
        for(int i = startIndex; i < candidates.Count; i++)
        {
            var bundle = candidates[i];
            
            // 선택 가능 횟수 체크
            int alreadySelected = current.Count(b => b.GemType == bundle.GemType);
            if(alreadySelected >= pools[bundle.GemType].MaxSelectCount)
                continue;
            
            // 선택
            current.Add(bundle);
            
            // 재귀 호출
            if(BacktrackSearch(candidates, pools, target, gemTypeCount, currentTotal + bundle.GemCount, result, current, i + 1))
                return true;
            
            // 되돌리기
            current.RemoveAt(current.Count - 1);
        }
        
        return false;
    }
    
    // ========== 헬퍼: 모든 색 1개 이상 포함 검증 ==========
    private bool ValidateAllColors(List<GemBundle> bundles, int gemTypeCount)
    {
        for(int i = 0; i < gemTypeCount; i++)
        {
            GemType type = (GemType)i;
            if(!bundles.Any(b => b.GemType == type))
            {
                return false;
            }
        }
        return true;
    }
    
    // ========== 헬퍼: 모드별 정렬 ==========
    private List<GemBundle> SortByMode(List<GemBundle> bundles, SelectionMode mode)
    {
        switch(mode)
        {
            case SelectionMode.SmallFirst:
                return bundles.OrderBy(b => b.GemCount).ToList();
            case SelectionMode.LargeFirst:
                return bundles.OrderByDescending(b => b.GemCount).ToList();
            default:
                return bundles;
        }
    }
}