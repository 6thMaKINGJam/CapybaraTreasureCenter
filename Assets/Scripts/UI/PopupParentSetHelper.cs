using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopupParentSetHelper : MonoBehaviour
{
    public static PopupParentSetHelper Instance { get; private set; }
    
    [Header("팝업용 부모")]
    public Transform PopupParentTransform;
    
    void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            Debug.Log("[PopupParentSetHelper] Instance 초기화 완료");
        }
        else
        {
            Debug.LogWarning("[PopupParentSetHelper] 중복된 인스턴스 감지, 파괴합니다.");
            Destroy(gameObject);
        }
    }
    
    public GameObject CreatePopup(string prefabPath)
    {
        Debug.Log($"[PopupParentSetHelper] CreatePopup 호출: {prefabPath}");
        
        if(PopupParentTransform == null)
        {
            Debug.LogError("[PopupParentSetHelper] PopupParentTransform이 할당되지 않았습니다!");
            return null;
        }
        
        GameObject prefab = Resources.Load<GameObject>(prefabPath);
        
        if(prefab == null)
        {
            Debug.LogError($"[PopupParentSetHelper] Prefab을 찾을 수 없습니다: Resources/{prefabPath}");
            return null;
        }
        
        GameObject popupObj = Instantiate(prefab, PopupParentTransform);
        Debug.Log($"[PopupParentSetHelper] Popup 생성 완료: {popupObj.name}");
        
        return popupObj;
    }
}
