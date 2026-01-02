using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Linq;

public class SelectedBundlesUIPanel : MonoBehaviour
{
    [Header("프리팹")]
    public GameObject BundlePrefab;
    
    [Header("패널 부모")]
    public Transform PanelParent;
    
    [Header("랜덤 생성 설정")]
    [SerializeField] private float randomRotationRange = 20f; // ±20도
    
    [SerializeField] private BoxVisualController boxVisual;

    private RectTransform spawnArea; // ← 캐싱
    private List<GemBundlePrefab> pool = new List<GemBundlePrefab>();

    private void OnEnable()
    {
        if (boxVisual != null)
        {
            boxVisual.OnGemTrfChanged += HandleGemTrfChanged;
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

        PanelParent = newGemTrf;
        
        // ← spawnArea 캐싱 (trf 변경 시점에만)
        spawnArea = PanelParent.GetComponent<RectTransform>();

        foreach (var prefab in pool)
        {
            if (prefab != null)
                prefab.transform.SetParent(PanelParent, false);
        }
    }

    public void UpdateUI(List<GemBundle> selectedBundles)
    {
        int currentCount = pool.Count(p => p != null && p.gameObject.activeSelf);
        int targetCount = selectedBundles.Count;
        
        // ========== Case 1: 전체 제거 (CancelSelection) ==========
        if (targetCount == 0)
        {
            foreach (var prefab in pool)
            {
                if (prefab != null && prefab.gameObject != null)
                {
                    Destroy(prefab.gameObject);
                }
            }
            pool.Clear();
            return;
        }
        
        // ========== Case 2: 보석 추가 (1개씩) ==========
        if (targetCount > currentCount)
        {
            // 새로 추가된 보석만 생성
            for (int i = currentCount; i < targetCount; i++)
            {
                GemBundlePrefab prefab = CreateNewGem();
                prefab.SetData(selectedBundles[i]);
                
                // ← 새 보석만 랜덤 배치 + 뽀잉 효과
                SetupGemWithAnimation(prefab);
                
                prefab.gameObject.SetActive(true);
            }
        }
    }

    // ========== 새 보석 생성 ==========
    private GemBundlePrefab CreateNewGem()
    {
        GameObject obj = Instantiate(BundlePrefab, PanelParent);
        GemBundlePrefab script = obj.GetComponent<GemBundlePrefab>();
        
        // 클릭 불가 처리
        Button btn = obj.GetComponent<Button>();
        if (btn != null)
        {
            btn.interactable = false;
        }
        
        pool.Add(script);
        return script;
    }

    // ========== 뽀잉 애니메이션 + 랜덤 배치 ==========
    private void SetupGemWithAnimation(GemBundlePrefab prefab)
    {
        RectTransform rt = prefab.GetComponent<RectTransform>();
        
        // 1. 랜덤 위치 설정
        if (spawnArea != null)
        {
            float randomX = Random.Range(-spawnArea.rect.width / 2, spawnArea.rect.width / 2);
            float randomY = Random.Range(-spawnArea.rect.height / 2, spawnArea.rect.height / 2);
            
            rt.anchoredPosition = new Vector2(randomX, randomY);
        }
        else
        {
            rt.anchoredPosition = Vector2.zero;
        }
        
        // 2. 랜덤 회전 설정 (±20도)
        float randomRotation = Random.Range(-randomRotationRange, randomRotationRange);
        rt.localRotation = Quaternion.Euler(0, 0, randomRotation);
        
        // 3. 뽀잉 애니메이션 (0 → 1.2 → 1.0, 총 0.3초)
        rt.localScale = Vector3.zero;
        
        rt.DOScale(1.2f, 0.15f)
            .SetEase(Ease.OutBack)
            .OnComplete(() => {
                rt.DOScale(1.0f, 0.15f)
                    .SetEase(Ease.InOutQuad);
            });
    }
}