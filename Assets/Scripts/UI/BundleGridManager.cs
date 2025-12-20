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


/// <summary>
/// 원본 프리팹을 복제한 뒤, 껍데기(투명 Placeholder)로 개조하여 반환합니다.
/// </summary>
public GameObject CreatePlaceholderFrom(GameObject originalPrefab, Transform parent)
{
    // 1. 원본 프리팹을 생성 (Layout 설정을 그대로 가져오기 위해)
    GameObject placeholder = Instantiate(originalPrefab, parent);
    
    // 2. 이름 변경 (구분을 위해)
    placeholder.name = "Placeholder_Generated";

    // 3. 기능 제거: GemBundlePrefab 스크립트 삭제 (더 이상 보석 기능 안 함)
    // (컴포넌트만 제거하고 게임오브젝트는 남김)
    GemBundlePrefab script = placeholder.GetComponent<GemBundlePrefab>();
    if (script != null)
    {
        Destroy(script); 
    }
    
    // 4. 투명화 및 터치 차단 (CanvasGroup 사용)
    CanvasGroup cg = placeholder.GetComponent<CanvasGroup>();
    if (cg == null) cg = placeholder.AddComponent<CanvasGroup>();
    
    cg.alpha = 0f;               // 완전 투명
    cg.blocksRaycasts = false;   // 터치 통과
    cg.interactable = false;     // 상호작용 불가

    // 5. (선택사항) 하위 이미지들 제거해서 가볍게 만들고 싶다면?
    // 하지만 CanvasGroup alpha=0 이면 어차피 렌더링 비용이 적으므로 굳이 안 해도 됨.

    return placeholder;
}

// ===== 특정 위치 번들만 애니메이션과 함께 교체 =====
// ===== 번들 교체 (애니메이션 포함) =====
// ===== 번들 교체 (애니메이션 포함) - 수정 버전 =====
public void ReplaceBundleWithAnimation(
    GemBundlePrefab oldPrefab,
    GemBundle newData,
    Action<GemBundlePrefab> clickCallback,
    bool isRestoring = false)
{
    if(isRestoring)
    {
        // ===== 복원 모드: 애니메이션 없이 즉시 복원 =====
        oldPrefab.SetData(newData);
        oldPrefab.SetSelected(false);
        
        // 콜백 재연결
        oldPrefab.OnClickBundle -= clickCallback;
        oldPrefab.OnClickBundle += clickCallback;
        
        // 투명도 복원
        CanvasGroup canvasGroup = oldPrefab.GetComponent<CanvasGroup>();
        if(canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }
        
        // 클릭 가능하게 복원
        Button btn = oldPrefab.GetComponent<Button>();
        if(btn != null)
        {
            btn.interactable = true;
        }
    }
    else
    {
        // ===== 일반 교체 모드: 애니메이션 =====
        int siblingIndex = oldPrefab.transform.GetSiblingIndex();
        
        // 1. 기존 번들 축소 애니메이션
        oldPrefab.transform.DOScale(0f, 0.3f)
            .SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                // 2. 새 번들 or Placeholder 생성
                GemBundlePrefab newPrefab;
                
                if(newData == null) // Placeholder 생성
                {
                    newPrefab = CreatePlaceholder();
                }
                else // 일반 번들 생성
                {
                    newPrefab = GetFromPool();
                    newPrefab.SetData(newData);
                    newPrefab.OnClickBundle += clickCallback;
                    newPrefab.SetSelected(false);
                }
                
                // 3. 기존 Prefab 처리
                oldPrefab.OnClickBundle -= clickCallback;
                oldPrefab.gameObject.SetActive(false);
                activeBundles.Remove(oldPrefab); // ← 리스트에서 제거
                
                // 4. 새 Prefab 위치 설정
                newPrefab.transform.SetParent(GridParent);
                newPrefab.transform.SetSiblingIndex(siblingIndex); // ← Transform 순서 지정
                newPrefab.gameObject.SetActive(true);
                
                // ★ 수정: activeBundles는 순서 상관없이 관리
                // SiblingIndex로 위치 찾으므로 그냥 Add
                activeBundles.Add(newPrefab);
                
                // 5. 새 번들 팝업 애니메이션
                newPrefab.transform.localScale = Vector3.zero;
                newPrefab.transform.DOScale(1.2f, 0.2f)
                    .SetEase(Ease.OutBack)
                    .OnComplete(() =>
                    {
                        newPrefab.transform.DOScale(1.0f, 0.1f);
                    });
            });
    }
}
// ===== 외부 접근용: activeBundles 리스트 반환 =====
public List<GemBundlePrefab> GetActiveBundles()
{
    return new List<GemBundlePrefab>(activeBundles);
}

// ===== Placeholder 생성 =====
private GemBundlePrefab CreatePlaceholder()
{
    // 기존 BundlePrefab을 투명하게 사용
    GameObject obj = Instantiate(BundlePrefab);
    
    // 투명 처리
    CanvasGroup canvasGroup = obj.GetComponent<CanvasGroup>();
    if(canvasGroup == null)
    {
        canvasGroup = obj.AddComponent<CanvasGroup>();
    }
    canvasGroup.alpha = 0f; // 완전 투명
    
    // 클릭 불가 처리
    Button btn = obj.GetComponent<Button>();
    if(btn != null)
    {
        btn.interactable = false;
    }
    
    GemBundlePrefab script = obj.GetComponent<GemBundlePrefab>();
    pool.Add(script);
    
    return script;
}

// ===== 특정 인덱스의 Prefab 가져오기 (GameManager용) =====
// ===== 특정 SiblingIndex의 Prefab 가져오기 =====
public GemBundlePrefab GetPrefabAtIndex(int siblingIndex)
{
    foreach(var prefab in activeBundles)
    {
        if(prefab.gameObject.activeSelf && prefab.transform.GetSiblingIndex() == siblingIndex)
        {
            return prefab;
        }
    }
    return null;
}

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