using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GemCountPanelManager : MonoBehaviour
{
    [Header("레벨별 보석 종류 표시용 프리팹")]
    public GameObject AvailableGemItemPrefab; 

    // 현재 레벨의 보석 현황판을 초기화하는 함수
    public void InitLevelGemStatus(List<GemBundle> levelGems)
    {
        // 1. 기존 UI 아이템들 삭제
        foreach (Transform child in transform) {
            Destroy(child.gameObject);
        }

        // 2. 이번 레벨에 지정된 보석 종류만큼 생성
        foreach (var gem in levelGems) {
            GameObject item = Instantiate(AvailableGemItemPrefab, transform);
            
            //GemBundlePrefab 스크립트를 그대로 사용
            var prefabScript = item.GetComponent<GemBundlePrefab>();
            if(prefabScript != null) {
                prefabScript.SetData(gem); // 보석 종류와 전체 개수 표시
            }
        }
    }
}
