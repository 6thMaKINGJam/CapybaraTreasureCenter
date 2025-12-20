using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GemCountPanelManager : MonoBehaviour
{
    [Header("레벨별 보석 종류 표시용 프리팹")]
    public GameObject AvailableGemItemPrefab;

    private Dictionary<GemType, GemBundlePrefab> gemUIList = new Dictionary<GemType, GemBundlePrefab>();

    // 현재 레벨의 보석 현황판을 초기화하는 함수
    public void InitLevelGemStatus(List<GemBundle> levelGems)
    {
        gemUIList.Clear();
        foreach (Transform child in transform) {
            Destroy(child.gameObject);
        }

        foreach (var gem in levelGems) {
            GameObject item = Instantiate(AvailableGemItemPrefab, transform);
            var prefabScript = item.GetComponent<GemBundlePrefab>();
            if(prefabScript != null) {
                prefabScript.SetData(gem);
                // 종류별로 스크립트 참조 저장
                gemUIList[gem.GemType] = prefabScript;
            }
        }
    }

    public void UpdateGemCount(GemType type, int count)
    {
        if(gemUIList.ContainsKey(type))
        {
            // 기존 데이터 구조를 유지하면서 숫자만 갱신
            GemBundle updatedData = gemUIList[type].GetData();
            updatedData.GemCount = count;
            gemUIList[type].SetData(updatedData);
        }
    }
}
