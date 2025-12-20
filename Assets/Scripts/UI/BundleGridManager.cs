using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class BundleGridManager : MonoBehaviour
{
    [Header("프리팹")]
    public GameObject BundlePrefab; // GemBundlePrefab
    
    [Header("그리드 부모")]
    public Transform GridParent; // Grid Layout Group
    
    // 오브젝트 풀
    private List<GemBundlePrefab> pool = new List<GemBundlePrefab>();
    
    // 현재 활성화된 묶음들
    private List<GemBundlePrefab> activeBundles = new List<GemBundlePrefab>();
    
    // 현재 표시 중인 데이터 (참조용)
    private List<GemBundle> currentDisplayData = new List<GemBundle>();
    
    // 콜백
    private Action<GemBundlePrefab> onBundleClickCallback;

    // ========== 그리드 갱신 (12개 묶음 표시) ==========
    public void RefreshGrid(List<GemBundle> newBundles, Action<GemBundlePrefab> clickCallback)
    {
        // 기존 활성화된 묶음 전부 비활성화
        foreach(var bundlePrefab in activeBundles)
        {
            bundlePrefab.gameObject.SetActive(false);
            bundlePrefab.OnClickBundle -= onBundleClickCallback; // 이벤트 해제
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
            prefab.SetSelected(false); // 초기 선택 해제
            prefab.gameObject.SetActive(true);
            
            activeBundles.Add(prefab);
        }
    }

    // ========== 단일 묶음 교체 (사용된 묶음 제거 후 새 묶음 추가) ==========
    public void ReplaceBundle(GemBundlePrefab targetPrefab, GemBundle newData, Action<GemBundlePrefab> clickCallback)
    {
        // 타겟 객체 이벤트 해제 및 비활성화
        targetPrefab.OnClickBundle -= clickCallback;
        targetPrefab.gameObject.SetActive(false);
        activeBundles.Remove(targetPrefab);
        
        // 새 데이터가 있으면 추가
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

    // ========== 힌트: 특정 묶음들 강조 ==========
    public void HighlightBundles(List<GemBundle> bundlesToHighlight, float duration)
    {
        StartCoroutine(HighlightCoroutine(bundlesToHighlight, duration));
    }

    private IEnumerator HighlightCoroutine(List<GemBundle> bundlesToHighlight, float duration)
    {
        List<GemBundlePrefab> highlightedPrefabs = new List<GemBundlePrefab>();
        
        // 강조 표시
        foreach(var bundleData in bundlesToHighlight)
        {
            GemBundlePrefab prefab = FindPrefabByData(bundleData);
            if(prefab != null)
            {
                prefab.transform.localScale *= 1.2f; // 크기 확대
                highlightedPrefabs.Add(prefab);
            }
        }
        
        // 지속 시간 대기
        yield return new WaitForSeconds(duration);
        
        // 원래대로 복구
        foreach(var prefab in highlightedPrefabs)
        {
            prefab.transform.localScale /= 1.2f;
        }
    }

    // ========== 유틸리티 ==========
    
    // 풀에서 사용 가능한 객체 가져오기
    private GemBundlePrefab GetFromPool()
    {
        // 비활성화된 객체 찾기
        foreach(var prefab in pool)
        {
            if(!prefab.gameObject.activeSelf)
            {
                return prefab;
            }
        }
        
        // 없으면 새로 생성
        GameObject obj = Instantiate(BundlePrefab);
        GemBundlePrefab script = obj.GetComponent<GemBundlePrefab>();
        pool.Add(script);
        
        return script;
    }

    // 데이터로 Prefab 찾기 (BundleID 기준)
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