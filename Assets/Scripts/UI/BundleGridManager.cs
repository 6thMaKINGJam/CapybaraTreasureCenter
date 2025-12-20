using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using DG.Tweening; // ← 추가!

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
        // 새로고침 시 흔들림 중지
        StopAllShaking();
        
        // 기존 활성화된 묶음 전부 비활성화
        foreach(var bundlePrefab in activeBundles)
        {
            bundlePrefab.gameObject.SetActive(false);
            bundlePrefab.OnClickBundle -= onBundleClickCallback;
        }
        activeBundles.Clear();
        
        // 새 데이터 저장
        currentDisplayData = new List<GemBundle>(newBundles);
        onBundleClickCallback = clickCallback;
        
        // 새 묶음들 표시
        foreach(var bundleData in newBundles)
        {
            GemBundlePrefab prefab = GetFromPool();
            prefab.transform.SetParent(GridParent);
            prefab.SetData(bundleData);
            prefab.OnClickBundle += onBundleClickCallback;
            prefab.SetSelected(false);
            prefab.gameObject.SetActive(true);
            
            activeBundles.Add(prefab);
        }
    }

    // ========== 단일 묶음 교체 ==========
    public void ReplaceBundle(GemBundlePrefab targetPrefab, GemBundle newData, Action<GemBundlePrefab> clickCallback)
    {
        targetPrefab.OnClickBundle -= clickCallback;
        targetPrefab.gameObject.SetActive(false);
        activeBundles.Remove(targetPrefab);
        
        if(newData != null)
        {
            GemBundlePrefab newPrefab = GetFromPool();
            newPrefab.SetData(newData);
            newPrefab.OnClickBundle += clickCallback;
            newPrefab.SetSelected(false);
            newPrefab.gameObject.SetActive(true);
            
            activeBundles.Add(newPrefab);
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
                // DOTween으로 Z축 회전 (-5° ~ +5° 왕복)
                Tweener shakeTween = prefab.transform
                    .DORotate(new Vector3(0, 0, 5f), 0.1f) // 5도 회전, 0.1초
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
    private void StopAllShaking()
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