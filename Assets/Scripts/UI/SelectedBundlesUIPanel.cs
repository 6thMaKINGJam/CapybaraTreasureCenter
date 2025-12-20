using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SelectedBundlesUIPanel : MonoBehaviour
{
    [Header("프리팹")]
    public GameObject BundlePrefab; // GemBundlePrefab (읽기 전용)
    
    [Header("패널 부모")]
    public Transform PanelParent; // Horizontal Layout Group
    
    // 오브젝트 풀
    private List<GemBundlePrefab> pool = new List<GemBundlePrefab>();

    // ========== UI 업데이트 (선택된 묶음들 표시) ==========
    public void UpdateUI(List<GemBundle> selectedBundles)
    {
        // 기존 활성화된 객체 전부 비활성화
        foreach(var prefab in pool)
        {
            prefab.gameObject.SetActive(false);
        }

        // 선택된 묶음들 표시
        for(int i = 0; i < selectedBundles.Count; i++)
        {
            GemBundlePrefab prefab = GetFromPool(i);
            prefab.SetData(selectedBundles[i]);
            prefab.gameObject.SetActive(true);
        }
    }

    // ========== 풀에서 객체 가져오기 ==========
    private GemBundlePrefab GetFromPool(int index)
    {
        // 인덱스에 해당하는 객체가 이미 있으면 반환
        if(index < pool.Count)
        {
            return pool[index];
        }

        // 없으면 새로 생성
        GameObject obj = Instantiate(BundlePrefab, PanelParent);
        GemBundlePrefab script = obj.GetComponent<GemBundlePrefab>();
        
        // 선택 패널에 표시된 묶음은 클릭 불가
        Button btn = obj.GetComponent<Button>();
        if(btn != null)
        {
            btn.interactable = false;
        }
        
        pool.Add(script);
        return script;
    }
}