using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI; // ← 추가!

public class BundleGridManager : MonoBehaviour
{
    [Header("프리팹")]
    public GameObject BundlePrefab;
    
    [Header("그리드 부모")]
    public Transform GridParent;

    
    
    // 오브젝트 풀
    private List<GemBundlePrefab> pool = new List<GemBundlePrefab>();
    
    
    // 현재 활성화된 묶음들
    private List<GemBundlePrefab> activeBundles = new List<GemBundlePrefab>();
    
    // 현재 표시 중인 데이터 (참조용)
    private List<GemBundle> currentDisplayData = new List<GemBundle>();
    
    // 콜백
    private Action<GemBundlePrefab> onBundleClickCallback;
    
    // ===== 힌트 흔들림 관련 =====
    private Dictionary<string, Tweener> shakingTweens = new Dictionary<string, Tweener>(); // BundleID -> Tween




    // ========== 그리드 갱신 (12개 묶음 표시) ==========
   public void RefreshGrid(List<GemBundle> newBundles, Action<GemBundlePrefab> clickCallback)
{
    StopAllShaking();
    
    foreach(var bundlePrefab in activeBundles)
    {
        bundlePrefab.gameObject.SetActive(false);
        bundlePrefab.OnClickBundle -= onBundleClickCallback;
    }
    activeBundles.Clear();
    
    currentDisplayData = new List<GemBundle>(newBundles);
    onBundleClickCallback = clickCallback;
    
    // ★ 핵심: 인덱스 순서대로 생성하고 SiblingIndex 명시적 설정
    for(int i = 0; i < newBundles.Count; i++)
    {
        GemBundle bundleData = newBundles[i];
        
        GemBundlePrefab prefab = GetFromPool();
        prefab.transform.SetParent(GridParent);
        prefab.transform.SetSiblingIndex(i); // ★ 명시적 설정!
        
        if(bundleData == null) // Placeholder
        {
            SetupAsPlaceholder(prefab);
        }
        else // 일반 번들
        {
            prefab.SetData(bundleData);
            prefab.OnClickBundle += onBundleClickCallback;
            prefab.SetSelected(false);
            
            // 투명도 복원 (혹시 Placeholder였을 경우)
            CanvasGroup cg = prefab.GetComponent<CanvasGroup>();
            if(cg != null) cg.alpha = 1f;
            
            Button btn = prefab.GetComponent<Button>();
            if(btn != null) btn.interactable = true;
        }
        
        prefab.gameObject.SetActive(true);
        activeBundles.Add(prefab);
    }
}

// ===== 특정 인덱스의 번들만 교체 (기존 ReplaceBundleWithAnimation 대체) =====
public void ReplaceBundleAtIndex(
    int index,
    GemBundle newData,
    Action<GemBundlePrefab> clickCallback,
    bool isRestoring = false)
{
    // 해당 인덱스의 Prefab 찾기
    GemBundlePrefab targetPrefab = null;
    foreach(var prefab in activeBundles)
    {
        if(prefab.transform.GetSiblingIndex() == index && prefab.gameObject.activeSelf)
        {
            targetPrefab = prefab;
            break;
        }
    }
    
    if(targetPrefab == null)
    {
        Debug.LogError($"[ReplaceBundleAtIndex] 인덱스 {index}의 Prefab을 찾을 수 없습니다!");
        return;
    }
    
    if(isRestoring)
    {
        // 복원: 애니메이션 없이 즉시
        targetPrefab.SetData(newData);
        targetPrefab.SetSelected(false);
        
        // 투명도 복원
        CanvasGroup cg = targetPrefab.GetComponent<CanvasGroup>();
        if(cg != null) cg.alpha = 1f;
        
        Button btn = targetPrefab.GetComponent<Button>();
        if(btn != null) btn.interactable = true;
        
        // 콜백 재연결
        targetPrefab.OnClickBundle -= clickCallback;
        targetPrefab.OnClickBundle += clickCallback;
    }
    else
    {
        // 일반 교체: 애니메이션
        targetPrefab.transform.DOScale(0f, 0.15f)
            .SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                if(newData == null) // Placeholder
                {
                    SetupAsPlaceholder(targetPrefab);
                }
                else // 일반 번들
                {
                    targetPrefab.SetData(newData);
                    targetPrefab.OnClickBundle -= clickCallback;
                    targetPrefab.OnClickBundle += clickCallback;
                    targetPrefab.SetSelected(false);
                    
                    // 투명도 복원
                    CanvasGroup cg = targetPrefab.GetComponent<CanvasGroup>();
                    if(cg != null) cg.alpha = 1f;
                    
                    Button btn = targetPrefab.GetComponent<Button>();
                    if(btn != null) btn.interactable = true;
                }
                
                // 팝업 애니메이션
                targetPrefab.transform.localScale = Vector3.zero;
                targetPrefab.transform.DOScale(1.2f, 0.1f)
                    .SetEase(Ease.OutBack)
                    .OnComplete(() =>
                    {
                        targetPrefab.transform.DOScale(1.0f, 0.05f);
                    });
            });
    }
}

// ===== Placeholder 설정 헬퍼 =====
private void SetupAsPlaceholder(GemBundlePrefab prefab)
{
    prefab.SetData(null); // ← 명시적으로 null 설정
    
    CanvasGroup cg = prefab.GetComponent<CanvasGroup>();
    if(cg == null) cg = prefab.gameObject.AddComponent<CanvasGroup>();
    cg.alpha = 0f;
    
    Button btn = prefab.GetComponent<Button>();
    if(btn != null) 
    {
        btn.interactable = false; // ← 클릭 자체를 막음
    }
}


   
    // ========== 모든 선택 해제 ==========
    public void ClearAllSelections()
    {
        foreach(var prefab in activeBundles)
        {
            prefab.SetSelected(false);
        }
    }

    // ========== 힌트: 특정 묶음들 흔들기 (제자리 회전) ==========
    public void ShakeBundles(List<GemBundle> bundlesToShake)
    {
        // 기존 흔들림 전부 정지
        StopAllShaking();
        
        foreach(var bundleData in bundlesToShake)
        {
            GemBundlePrefab prefab = FindPrefabByData(bundleData);
            if(prefab != null)
            {
                // DOTween으로 Z축 회전 (-10° ~ +10° 왕복)
                Tweener shakeTween = prefab.transform
                    .DORotate(new Vector3(0, 0, 15f), 0.1f) // 10도 회전, 0.1초
                    .SetLoops(-1, LoopType.Yoyo) // 무한 왕복
                    .SetEase(Ease.InOutSine); // 부드러운 곡선
                
                shakingTweens[bundleData.BundleID] = shakeTween;
            }
        }
    }

    // ========== 흔들림 중지 (터치한 번들만) ==========
    public void StopShakingBundle(GemBundle bundleData)
    {
        if(!shakingTweens.ContainsKey(bundleData.BundleID)) return;
        
        // Tween 중지
        Tweener tween = shakingTweens[bundleData.BundleID];
        if(tween != null && tween.IsActive())
        {
            tween.Kill();
        }
        
        // 원래 각도로 복귀
        GemBundlePrefab prefab = FindPrefabByData(bundleData);
        if(prefab != null)
        {
            prefab.transform.rotation = Quaternion.identity;
        }
        
        shakingTweens.Remove(bundleData.BundleID);
    }

    // ========== 모든 흔들림 중지 ==========
    public void StopAllShaking()
    {
        foreach(var kvp in shakingTweens)
        {
            if(kvp.Value != null && kvp.Value.IsActive())
            {
                kvp.Value.Kill();
            }
        }
        
        // 모든 번들 각도 초기화
        foreach(var prefab in activeBundles)
        {
            prefab.transform.rotation = Quaternion.identity;
        }
        
        shakingTweens.Clear();
    }

    // ========== 유틸리티 ==========
    
    private GemBundlePrefab GetFromPool()
    {
        foreach(var prefab in pool)
        {
            if(!prefab.gameObject.activeSelf)
            {
                return prefab;
            }
        }
        
        GameObject obj = Instantiate(BundlePrefab);
        GemBundlePrefab script = obj.GetComponent<GemBundlePrefab>();
        pool.Add(script);
        
        return script;
    }

    private GemBundlePrefab FindPrefabByData(GemBundle bundleData)
    {
        foreach(var prefab in activeBundles)
        {
            if(prefab.GetData().BundleID == bundleData.BundleID)
            {
                return prefab;
            }
        }
        return null;
    }
}