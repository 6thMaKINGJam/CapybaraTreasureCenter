using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 레벨별 보석 종류와 남은 개수를 표시하는 패널 관리
/// </summary>
public class GemCountPanelManager : MonoBehaviour
{
    [Header("보석 개수 아이템 프리팹")]
    [Tooltip("Image + TextMeshProUGUI가 있는 프리팹을 할당하세요")]
    public GameObject GemCountItemPrefab;
    
    [Header("스프라이트 데이터베이스")]
    public GemSpriteDatabase SpriteDatabase;
    
    // GemType → GemCountItem 매핑
    private Dictionary<GemType, GemCountItem> gemItemDict = new Dictionary<GemType, GemCountItem>();
    
    /// <summary>
    /// 현재 레벨의 보석 현황판 초기화
    /// </summary>
    /// <param name="totalGems">보석 종류별 총량 (ChunkData.TotalRemainingGems)</param>
    /// <param name="gemTypeCount">레벨에서 사용하는 보석 종류 수 (LevelConfig.GemTypeCount)</param>
    public void InitLevelGemStatus(Dictionary<GemType, int> totalGems, int gemTypeCount)
    {
        // 기존 UI 전부 제거
        ClearAllItems();
        
        if (SpriteDatabase == null)
        {
            Debug.LogError("[GemCountPanelManager] SpriteDatabase가 할당되지 않았습니다!");
            return;
        }
        
        if (GemCountItemPrefab == null)
        {
            Debug.LogError("[GemCountPanelManager] GemCountItemPrefab이 할당되지 않았습니다!");
            return;
        }
        
        // 레벨에서 사용하는 보석 종류만큼 생성 (0 ~ gemTypeCount-1)
        for (int i = 0; i < gemTypeCount; i++)
        {
            GemType type = (GemType)i;
            
            // 해당 타입의 총량 가져오기 (없으면 0)
            int count = totalGems.ContainsKey(type) ? totalGems[type] : 0;
            
            // 프리팹 생성
            GameObject itemObj = Instantiate(GemCountItemPrefab, transform);
            GemCountItem item = itemObj.GetComponent<GemCountItem>();
            
            if (item == null)
            {
                Debug.LogError($"[GemCountPanelManager] GemCountItemPrefab에 GemCountItem 컴포넌트가 없습니다!");
                Destroy(itemObj);
                continue;
            }
            
            // 데이터 설정
            item.SetData(type, count, SpriteDatabase);
            
            // Dictionary에 저장
            gemItemDict[type] = item;
        }
        
        Debug.Log($"[GemCountPanelManager] {gemTypeCount}종류 보석 UI 초기화 완료");
    }
    
    /// <summary>
    /// 특정 보석 종류의 개수만 업데이트
    /// </summary>
    /// <param name="type">업데이트할 보석 종류</param>
    /// <param name="newCount">새로운 개수</param>
    public void UpdateGemCount(GemType type, int newCount)
    {
        if (gemItemDict.ContainsKey(type))
        {
            gemItemDict[type].UpdateCount(newCount);
        }
        else
        {
            Debug.LogWarning($"[GemCountPanelManager] {type} 타입의 UI 아이템이 존재하지 않습니다!");
        }
    }
    
    /// <summary>
    /// 모든 보석 종류의 개수를 한 번에 업데이트 (필요 시 사용)
    /// </summary>
    public void UpdateAllGemCounts(Dictionary<GemType, int> gemCounts)
    {
        foreach (var kvp in gemCounts)
        {
            UpdateGemCount(kvp.Key, kvp.Value);
        }
    }
    
    /// <summary>
    /// 기존 UI 아이템 전부 제거
    /// </summary>
    private void ClearAllItems()
    {
        gemItemDict.Clear();
        
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    }
}