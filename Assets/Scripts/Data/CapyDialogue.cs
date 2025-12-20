using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
using System;

// 대사 상황 타입
public enum DialogueType
{
    Default,              // 기본 (게임 시작, 평상시)
    BoxCompleted,         // 상자 완료
    ConsecutiveSuccess,   // 연속 성공
    GemDepletedGameOver,  // 보석 0개로 게임오버
    Warning,              // 경고 (뭔가 잘못될 것 같을 때)
    TimeOverGameOver,     // 시간 오버로 게임오버
    TimeLowWarning,       // 시간 부족 경고
    HallOfFame            // 명예의 전당 (레벨4 클리어)
}

// 상황별 대사 데이터
[Serializable]
public class DialogueData
{
    [Header("상황 타입")]
    public DialogueType Type;
    
    [Header("대사 목록")]
    [TextArea(2, 5)]
    public string[] Dialogues;
    
    [Header("반복 설정")]
    [Tooltip("true: 대사가 계속 나왔다 사라졌다 반복 / false: 한 번만 표시")]
    public bool IsLoop = false;
    
    [Header("루프 설정 (IsLoop = true일 때만 적용)")]
    [Tooltip("다음 대사까지 대기 시간 (초)")]
    public float LoopInterval = 3f;
}

public class CapyDialogue : MonoBehaviour
{
    [Header("대사 데이터")]
    public List<DialogueData> DialogueDatas = new List<DialogueData>();
    
    [Header("애니메이션 설정")]
    [Tooltip("페이드 인 시간 (초)")]
    public float FadeInDuration = 0.3f;
    
    [Tooltip("대사 표시 유지 시간 (초, IsLoop = false일 때만 적용)")]
    public float DisplayDuration = 2f;
    
    [Tooltip("페이드 아웃 시간 (초)")]
    public float FadeOutDuration = 0.3f;
    
    // 내부 변수 - 현재 작동 중인 대사별 코루틴 추적
    private Dictionary<TextMeshProUGUI, Coroutine> activeDialogues = new Dictionary<TextMeshProUGUI, Coroutine>();
    private Dictionary<TextMeshProUGUI, CanvasGroup> canvasGroups = new Dictionary<TextMeshProUGUI, CanvasGroup>();
    
    
    /// <summary>
    /// 특정 상황의 대사를 표시합니다 (배열에서 랜덤 선택)
    /// </summary>
    /// <param name="targetText">대사를 표시할 TextMeshProUGUI</param>
    /// <param name="type">대사 상황 타입</param>
    public void ShowDialogue(TextMeshProUGUI targetText, DialogueType type)
    {
        if (targetText == null)
        {
            Debug.LogError("[CapyDialogue] targetText가 null입니다!");
            return;
        }
        
        // 해당 타입의 대사 데이터 찾기
        DialogueData data = DialogueDatas.Find(d => d.Type == type);
        
        if (data == null)
        {
            Debug.LogWarning($"[CapyDialogue] {type} 타입의 대사 데이터를 찾을 수 없습니다.");
            return;
        }
        
        if (data.Dialogues == null || data.Dialogues.Length == 0)
        {
            Debug.LogWarning($"[CapyDialogue] {type} 타입의 대사가 비어있습니다.");
            return;
        }
        
        // 기존 대사 즉시 중단
        StopDialogue(targetText);
        
        // CanvasGroup 준비
        CanvasGroup canvasGroup = GetOrCreateCanvasGroup(targetText);
        
        // 새 대사 시작
        if (data.IsLoop)
        {
            activeDialogues[targetText] = StartCoroutine(LoopDialogueCoroutine(targetText, canvasGroup, data));
        }
        else
        {
            activeDialogues[targetText] = StartCoroutine(ShowOnceCoroutine(targetText, canvasGroup, data));
        }
    }
    
    /// <summary>
    /// 특정 텍스트를 강제로 표시합니다 (overrideText)
    /// </summary>
    /// <param name="targetText">대사를 표시할 TextMeshProUGUI</param>
    /// <param name="text">표시할 텍스트</param>
    /// <param name="isLoop">반복 여부</param>
    /// <param name="loopInterval">반복 간격 (isLoop = true일 때만 적용)</param>
    public void ShowDialogue(TextMeshProUGUI targetText, string text, bool isLoop = false, float loopInterval = 3f)
    {
        if (targetText == null)
        {
            Debug.LogError("[CapyDialogue] targetText가 null입니다!");
            return;
        }
        
        // 기존 대사 즉시 중단
        StopDialogue(targetText);
        
        // CanvasGroup 준비
        CanvasGroup canvasGroup = GetOrCreateCanvasGroup(targetText);
        
        // 임시 DialogueData 생성
        DialogueData tempData = new DialogueData
        {
            Dialogues = new string[] { text },
            IsLoop = isLoop,
            LoopInterval = loopInterval
        };
        
        // 새 대사 시작
        if (tempData.IsLoop)
        {
            activeDialogues[targetText] = StartCoroutine(LoopDialogueCoroutine(targetText, canvasGroup, tempData));
        }
        else
        {
            activeDialogues[targetText] = StartCoroutine(ShowOnceCoroutine(targetText, canvasGroup, tempData));
        }
    }
    
    /// <summary>
    /// 특정 UI Text의 대사를 즉시 중단합니다
    /// </summary>
    /// <param name="targetText">중단할 TextMeshProUGUI</param>
    public void StopDialogue(TextMeshProUGUI targetText)
    {
        if (targetText == null) return;
        
        // 해당 Text의 코루틴이 실행 중이면 중단
        if (activeDialogues.ContainsKey(targetText) && activeDialogues[targetText] != null)
        {
            StopCoroutine(activeDialogues[targetText]);
            activeDialogues.Remove(targetText);
        }
        
        // CanvasGroup이 있으면 DOTween 정리 및 투명화
        if (canvasGroups.ContainsKey(targetText))
        {
            CanvasGroup canvasGroup = canvasGroups[targetText];
            canvasGroup.DOKill();
            canvasGroup.alpha = 0f;
        }
        
        // 텍스트 초기화
        targetText.text = "";
    }
    
    /// <summary>
    /// 모든 대사를 중단합니다
    /// </summary>
    public void StopAllDialogues()
    {
        foreach (var kvp in activeDialogues)
        {
            if (kvp.Value != null)
            {
                StopCoroutine(kvp.Value);
            }
            
            // CanvasGroup 정리
            if (canvasGroups.ContainsKey(kvp.Key))
            {
                canvasGroups[kvp.Key].DOKill();
                canvasGroups[kvp.Key].alpha = 0f;
            }
            
            // 텍스트 초기화
            kvp.Key.text = "";
        }
        
        activeDialogues.Clear();
    }
    
    // CanvasGroup 가져오거나 생성
    private CanvasGroup GetOrCreateCanvasGroup(TextMeshProUGUI targetText)
    {
        // 이미 캐시되어 있으면 반환
        if (canvasGroups.ContainsKey(targetText))
        {
            return canvasGroups[targetText];
        }
        
        // 없으면 생성 및 캐시
        CanvasGroup canvasGroup = targetText.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = targetText.gameObject.AddComponent<CanvasGroup>();
        }
        
        canvasGroups[targetText] = canvasGroup;
        
        // 초기 상태: 투명
        canvasGroup.alpha = 0f;
        
        return canvasGroup;
    }
    
    // 한 번만 표시하는 코루틴
    private IEnumerator ShowOnceCoroutine(TextMeshProUGUI targetText, CanvasGroup canvasGroup, DialogueData data)
    {
        // 랜덤 대사 선택
        string selectedDialogue = data.Dialogues[UnityEngine.Random.Range(0, data.Dialogues.Length)];
        
        // 텍스트 설정
        targetText.text = selectedDialogue;
        
        // 페이드 인
        yield return canvasGroup.DOFade(1f, FadeInDuration).WaitForCompletion();
        
        // 표시 유지
        yield return new WaitForSeconds(DisplayDuration);
        
        // 페이드 아웃
        yield return canvasGroup.DOFade(0f, FadeOutDuration).WaitForCompletion();
        
        // 텍스트 초기화
        targetText.text = "";
        
        // 완료 후 딕셔너리에서 제거
        if (activeDialogues.ContainsKey(targetText))
        {
            activeDialogues.Remove(targetText);
        }
    }
    
    // 반복 표시하는 코루틴
    private IEnumerator LoopDialogueCoroutine(TextMeshProUGUI targetText, CanvasGroup canvasGroup, DialogueData data)
    {
        while (true) // 외부에서 StopDialogue()로 중단할 때까지 무한 반복
        {
            // 랜덤 대사 선택
            string selectedDialogue = data.Dialogues[UnityEngine.Random.Range(0, data.Dialogues.Length)];
            
            // 텍스트 설정
            targetText.text = selectedDialogue;
            
            // 페이드 인
            yield return canvasGroup.DOFade(1f, FadeInDuration).WaitForCompletion();
            
            // 표시 유지 (루프에서는 짧게)
            yield return new WaitForSeconds(DisplayDuration);
            
            // 페이드 아웃
            yield return canvasGroup.DOFade(0f, FadeOutDuration).WaitForCompletion();
            
            // 텍스트 초기화
            targetText.text = "";
            
            // 다음 대사까지 대기
            yield return new WaitForSeconds(data.LoopInterval);
        }
    }
    
    private void OnDestroy()
    {
        // 모든 DOTween 정리
        foreach (var canvasGroup in canvasGroups.Values)
        {
            canvasGroup?.DOKill();
        }
        
        canvasGroups.Clear();
        activeDialogues.Clear();
    }
}