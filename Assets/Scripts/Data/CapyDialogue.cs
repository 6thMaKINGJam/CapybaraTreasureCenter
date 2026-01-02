using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
using System;
using UnityEngine.UI;

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
    HallOfFame,            // 명예의 전당 (레벨4 클리어)
    AlreadyFailed
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

     [Header("표시 설정")]
    [Tooltip("true: 계속 떠있음 (다른 대사 호출 전까지) / false: 일정 시간 후 사라짐")]
    public bool IsPersistent = false; // ✅ 추가
    
    
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
    
    [Header("말풍선 이미지")] // ← 추가
public GameObject DialogueBubble; // Inspector에 할당

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
    
    private CanvasGroup bubbleCanvasGroup; // ← 추가

    void Awake() // ← 추가
{
    if(DialogueBubble != null)
    {
        bubbleCanvasGroup = DialogueBubble.GetComponent<CanvasGroup>();
        if(bubbleCanvasGroup == null)
        {
            bubbleCanvasGroup = DialogueBubble.AddComponent<CanvasGroup>();
        }
    }
}
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
        
        // 텍스트 색상 설정
        SetTextColor(targetText, type);
        
        // CanvasGroup 준비
        CanvasGroup canvasGroup = GetOrCreateCanvasGroup(targetText);
        
        // 새 대사 시작
       if (data.IsLoop)
        {
            activeDialogues[targetText] = StartCoroutine(LoopDialogueCoroutine(targetText, canvasGroup, data));
        }
        else if (data.IsPersistent) // ✅ 추가
        {
            activeDialogues[targetText] = StartCoroutine(PersistentDialogueCoroutine(targetText, canvasGroup, data));
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
    public void ShowDialogue(TextMeshProUGUI targetText, string text, bool isLoop = false, float loopInterval = 3f, bool isPersistent = false)
    {
        if (targetText == null)
        {
            Debug.LogError("[CapyDialogue] targetText가 null입니다!");
            return;
        }
        
        // 기존 대사 즉시 중단
        StopDialogue(targetText);
        
        // 커스텀 텍스트는 기본 색상(검정) 사용
        targetText.color = Color.black;
        
        // CanvasGroup 준비
        CanvasGroup canvasGroup = GetOrCreateCanvasGroup(targetText);
        
        // 임시 DialogueData 생성
        DialogueData tempData = new DialogueData
        {
            Dialogues = new string[] { text },
            IsLoop = isLoop,
            LoopInterval = loopInterval,
             IsPersistent = isPersistent // ✅ 추가
        };
        
        // 새 대사 시작
        if (tempData.IsLoop)
        {
            activeDialogues[targetText] = StartCoroutine(LoopDialogueCoroutine(targetText, canvasGroup, tempData));
        }
        else if (tempData.IsPersistent) // ✅ 추가
        {
            activeDialogues[targetText] = StartCoroutine(PersistentDialogueCoroutine(targetText, canvasGroup, tempData));
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
    
    if (activeDialogues.ContainsKey(targetText) && activeDialogues[targetText] != null)
    {
        StopCoroutine(activeDialogues[targetText]);
        activeDialogues.Remove(targetText);
    }
    
    if (canvasGroups.ContainsKey(targetText))
    {
        canvasGroups[targetText].DOKill();
        canvasGroups[targetText].alpha = 1f; // 텍스트는 항상 보이게
    }
    
    // ← 말풍선 페이드 아웃
    if(bubbleCanvasGroup != null)
    {
        bubbleCanvasGroup.DOKill();
        bubbleCanvasGroup.alpha = 0f;
    }
    
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
    
    /// <summary>
    /// DialogueType에 따라 텍스트 색상을 설정합니다
    /// </summary>
    private void SetTextColor(TextMeshProUGUI targetText, DialogueType type)
    {
        Color outlineColor = Color.black;
        switch(type)
        {
            case DialogueType.Warning:
            case DialogueType.TimeLowWarning:
            case DialogueType.AlreadyFailed:
                targetText.color = Color.red;
             outlineColor = Color.red;
             targetText.fontSizeMax = 55f;
                break;
            
            case DialogueType.BoxCompleted:
                targetText.color = new Color(1f, 0.6f, 0f);
                outlineColor = new Color(1f, 0.4f, 0f); // 어두운 노랑
                targetText.fontSizeMax = 55f;
             
                break;
            default:
                targetText.color = Color.black;
                outlineColor = Color.black;
                targetText.fontSizeMax = 45f;
               
                break;

        }
        // ✅ 전용 Material이므로 바로 수정 가능
    if (targetText.fontMaterial != null)
    {
        targetText.fontMaterial.SetColor("_OutlineColor", outlineColor);
    }
    }
    
    /// <summary>
    /// 특정 Text의 Default 대사를 딜레이 후 다시 시작합니다
    /// </summary>
    /// <param name="targetText">대사를 표시할 TextMeshProUGUI</param>
    /// <param name="delay">재시작까지 대기 시간 (초)</param>
    public void RestartDefault(TextMeshProUGUI targetText, float delay = 0.5f)
    {
        if (targetText == null)
        {
            Debug.LogError("[CapyDialogue] RestartDefault: targetText가 null입니다!");
            return;
        }
        
        StartCoroutine(RestartDefaultAfterDelay(targetText, delay));
    }
    
    private IEnumerator RestartDefaultAfterDelay(TextMeshProUGUI targetText, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Default 재시작 전 색상 검정으로 복원
        targetText.color = Color.black;
        
        // Default 대사 다시 시작
        ShowDialogue(targetText, DialogueType.Default);
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
    
    /// <summary>
    /// 특정 타입의 대사 중 하나를 랜덤으로 반환합니다 (화면 표시 X)
    /// </summary>
    public string GetRandomMessage(DialogueType type)
    {
        // 데이터 찾기
        DialogueData data = DialogueDatas.Find(d => d.Type == type);
        
        // 데이터 유효성 검사
        if (data != null && data.Dialogues != null && data.Dialogues.Length > 0)
        {
            // 랜덤으로 하나 뽑아서 리턴
            return data.Dialogues[UnityEngine.Random.Range(0, data.Dialogues.Length)];
        }
        
        // 데이터가 없으면 빈 문자열 반환
        return "";
    }
    
 // 한 번만 표시
private IEnumerator ShowOnceCoroutine(TextMeshProUGUI targetText, CanvasGroup canvasGroup, DialogueData data)
{
    string selectedDialogue = data.Dialogues[UnityEngine.Random.Range(0, data.Dialogues.Length)];
    targetText.text = selectedDialogue;
    
    // ← 말풍선 페이드 인 (텍스트 CanvasGroup은 항상 1로)
    canvasGroup.alpha = 1f;
    if(bubbleCanvasGroup != null)
    {
        yield return bubbleCanvasGroup.DOFade(1f, FadeInDuration).WaitForCompletion();
    }
    
    yield return new WaitForSeconds(DisplayDuration);
    
    // ← 말풍선 페이드 아웃
    if(bubbleCanvasGroup != null)
    {
        yield return bubbleCanvasGroup.DOFade(0f, FadeOutDuration).WaitForCompletion();
    }
    
    targetText.text = "";
    
    if (activeDialogues.ContainsKey(targetText))
    {
        activeDialogues.Remove(targetText);
    }
}

// 계속 표시
private IEnumerator PersistentDialogueCoroutine(TextMeshProUGUI targetText, CanvasGroup canvasGroup, DialogueData data)
{
    string selectedDialogue = data.Dialogues[UnityEngine.Random.Range(0, data.Dialogues.Length)];
    targetText.text = selectedDialogue;
    
    canvasGroup.alpha = 1f;
    if(bubbleCanvasGroup != null)
    {
        yield return bubbleCanvasGroup.DOFade(1f, FadeInDuration).WaitForCompletion();
    }
}

// 반복 표시
private IEnumerator LoopDialogueCoroutine(TextMeshProUGUI targetText, CanvasGroup canvasGroup, DialogueData data)
{
    while (true)
    {
        string selectedDialogue = data.Dialogues[UnityEngine.Random.Range(0, data.Dialogues.Length)];
        targetText.text = selectedDialogue;
        
        canvasGroup.alpha = 1f;
        if(bubbleCanvasGroup != null)
        {
            yield return bubbleCanvasGroup.DOFade(1f, FadeInDuration).WaitForCompletion();
        }
        
        yield return new WaitForSeconds(DisplayDuration);
        
        if(bubbleCanvasGroup != null)
        {
            yield return bubbleCanvasGroup.DOFade(0f, FadeOutDuration).WaitForCompletion();
        }
        
        targetText.text = "";
        
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