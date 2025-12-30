using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems; // 마우스 클릭 감지 위해 필요

public class ButtonAnimation : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private Vector3 initialScale;
    public float pressScale = 0.8f; // 눌렸을 때 크기 -> 0.9배

    void Awake() {
        initialScale = transform.localScale; // 원래 크기 저장
    }

    // 버튼 눌렸을 때 실행
    public void OnPointerDown(PointerEventData eventData) {
        transform.localScale = initialScale * pressScale; 
    }

    // 버튼에서 손 뗐을 때 실행
    public void OnPointerUp(PointerEventData eventData) {
        transform.localScale = initialScale; 
    }
}
