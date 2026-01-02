using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SelectedBundlesUIPanel : MonoBehaviour
{
    [Header("프리팹")]
    public GameObject BundlePrefab; // GemBundlePrefab (읽기 전용)
    
    [Header("패널 부모")]
    public Transform PanelParent; // Horizontal Layout Group
    
    [SerializeField] private BoxVisualController boxVisual;

private void OnEnable()
{
    if (boxVisual != null)
    {
        boxVisual.OnGemTrfChanged += HandleGemTrfChanged;

        // 시작할 때도 현재 gem_trf로 1번 세팅
        HandleGemTrfChanged(boxVisual.CurrentGemTrf);
    }
}

private void OnDisable()
{
    if (boxVisual != null)
        boxVisual.OnGemTrfChanged -= HandleGemTrfChanged;
}

private void HandleGemTrfChanged(RectTransform newGemTrf)
{
    if (newGemTrf == null) return;

    // ★ 핵심: PanelParent를 현재 박스의 gem_trf로 갱신
    PanelParent = newGemTrf;

    // (선택) 이미 만들어진 UI가 남아있을 수 있으면 부모도 같이 옮김
    foreach (var prefab in pool)
    {
        if (prefab != null)
            prefab.transform.SetParent(PanelParent, false);
    }
}

    // 오브젝트 풀
    private List<GemBundlePrefab> pool = new List<GemBundlePrefab>();

    // ========== UI 업데이트 (선택된 묶음들 표시) ==========
    // SelectedBundlesUIPanel.cs 수정
public void UpdateUI(List<GemBundle> selectedBundles)
{
    // ===== 변경: SetActive(false) 대신 Destroy =====
    foreach(var prefab in pool)
    {
        if(prefab != null && prefab.gameObject != null)
        {
            Destroy(prefab.gameObject);
        }
    }
    pool.Clear(); // 풀도 완전히 비움
    
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