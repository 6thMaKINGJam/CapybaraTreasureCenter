using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using System;


public class TutorialManager : MonoBehaviour
{
    [Header("튜토리얼 시퀀스")]
    public List<TutorialSequence> Sequences = new List<TutorialSequence>();
    
    [Header("UI 요소")]
    public Image BackgroundImage; // 배경 이미지를 표시할 Image
    public GameObject BoxDialogPrefab; // 네모 박스창 Prefab
    public GameObject SpeechDialogPrefab; // 대화창 Prefab
    public GameObject ChoiceDialogPrefab; // 선택창 Prefab
    public Transform DialogParent; // 창을 생성할 부모 Transform (Canvas 하위)
    
    [Header("트랜지션 설정")]
    public float BackgroundFadeDuration = 0.5f; // 배경 전환 시간
    public float DialogFadeOutDuration = 0.3f; // 창 사라지는 시간
    public float DialogFadeInDuration = 0.3f; // 창 나타나는 시간
    public Ease BackgroundEaseType = Ease.InOutQuad;
    public Ease DialogEaseType = Ease.OutQuad;
    
    private int currentSequenceIndex = 0;
    private GameObject currentDialog;
    private Sprite previousBackgroundSprite;
    private bool isTransitioning = false; // 전환 중인지 확인
    public static TutorialManager Instance;

[Header("Skip 버튼")]
    public Button skipButton;  // ← 추가
    

    void Awake() => Instance = this;
    
    void Start()
    {// Story Tutorial 이미 완료했으면 바로 MainHome으로
    if (SaveManager.IsTutorialCompleted(TutorialType.Story))
    {
        SceneManager.LoadScene("MainHome");
        return;
    }

     
        // Skip 버튼 설정
        if (skipButton != null)
        {
            skipButton.onClick.AddListener(OnSkipClicked);
            skipButton.gameObject.SetActive(true);
        }
        

    
    // 초기 배경 설정 (페이드 없이)
    if(BackgroundImage != null)
    {
        Color initialColor = BackgroundImage.color;
        initialColor.a = 0f;
        BackgroundImage.color = initialColor;
    }

    ShowSequence(0);

    }

 // Skip 버튼 클릭 시
    private void OnSkipClicked()
    {
        if (isTransitioning) return;
        
        // 현재 창 즉시 정리
        if (currentDialog != null)
        {
            Destroy(currentDialog);
            currentDialog = null;
        }
        
        CompleteTutorial();
    }

    
    // 특정 시퀀스 표시
   private void ShowSequence(int index, bool fadeBackground = true)
{
    if(isTransitioning) return;
    
    if(index >= Sequences.Count)
    {
        CompleteTutorial();
        return;
    }
    
    currentSequenceIndex = index;
    TutorialSequence sequence = Sequences[index];
    
    // ========== 진동 실행 (추가) ==========
    if (sequence.UseVibration)
    {
        VibrationManager.Instance.Vibrate(
            sequence.VibrationPattern, 
            sequence.VibrationDelay, 
            sequence.CustomVibrationPattern
        );
    }
    // ====================================
    
    bool needsBackgroundTransition = fadeBackground && 
                                     sequence.BackgroundImage != null && 
                                     previousBackgroundSprite != sequence.BackgroundImage;
    
    if(needsBackgroundTransition)
    {
        StartCoroutine(TransitionSequence(sequence));
    }
    else
    {
        SetupBackgroundImmediate(sequence);
        ShowDialog(sequence);
    }
    
    previousBackgroundSprite = sequence.BackgroundImage;
}

    
    // 배경 전환이 필요한 경우
    private IEnumerator TransitionSequence(TutorialSequence sequence)
    {
        isTransitioning = true;
        
        // 1. 현재 창 페이드 아웃
        if(currentDialog != null)
        {
            yield return FadeOutDialog(currentDialog);
            Destroy(currentDialog);
            currentDialog = null;
        }
        
        // 2. 배경 페이드 아웃
        yield return FadeOutBackground();
        
        // 3. 배경 이미지 변경
        if(sequence.BackgroundImage != null)
        {
            BackgroundImage.sprite = sequence.BackgroundImage;
        }
        
        // 4. 배경 페이드 인
        yield return FadeInBackground();
        
        // 5. 새 창 표시
        ShowDialog(sequence);
        
        isTransitioning = false;
    }

   
    
    // 배경을 즉시 설정 (전환 불필요한 경우)
    private void SetupBackgroundImmediate(TutorialSequence sequence)
    {
        if(sequence.BackgroundImage != null && BackgroundImage.sprite == null)
        {
            BackgroundImage.sprite = sequence.BackgroundImage;
            
            // 첫 배경인 경우 페이드 인
            if(currentSequenceIndex == 0)
            {
                StartCoroutine(FadeInBackground());
            }
            else
            {
                // 이미 배경이 있는 경우 즉시 표시
                Color color = BackgroundImage.color;
                color.a = 1f;
                BackgroundImage.color = color;
            }
        }
    }
    
    // 창 표시 (페이드 인 효과 포함)
    private void ShowDialog(TutorialSequence sequence)
    {
        // 새 창 생성
        GameObject dialogPrefab = GetDialogPrefab(sequence.DialogType);
        if(dialogPrefab != null)
        {
            currentDialog = Instantiate(dialogPrefab, DialogParent);
            SetupDialog(currentDialog, sequence);
            
            // 창 페이드 인
            StartCoroutine(FadeInDialog(currentDialog));
        }
        else
        {
            Debug.LogError($"[TutorialManager] {sequence.DialogType} Prefab이 할당되지 않았습니다!");
        }

       
        
       
    }
    
    // 배경 페이드 아웃
    private IEnumerator FadeOutBackground()
    {
        if(BackgroundImage == null) yield break;
        
        yield return BackgroundImage.DOFade(0f, BackgroundFadeDuration)
            .SetEase(BackgroundEaseType)
            .WaitForCompletion();
    }
    
    // 배경 페이드 인
    private IEnumerator FadeInBackground()
    {
        if(BackgroundImage == null) yield break;
        
        yield return BackgroundImage.DOFade(1f, BackgroundFadeDuration)
            .SetEase(BackgroundEaseType)
            .WaitForCompletion();
    }
    
    // 창 페이드 아웃
    private IEnumerator FadeOutDialog(GameObject dialog)
    {
        if(dialog == null) yield break;
        
        CanvasGroup canvasGroup = dialog.GetComponent<CanvasGroup>();
        if(canvasGroup == null)
        {
            canvasGroup = dialog.AddComponent<CanvasGroup>();
        }
        
        yield return canvasGroup.DOFade(0f, DialogFadeOutDuration)
            .SetEase(DialogEaseType)
            .WaitForCompletion();
    }
    
    // 창 페이드 인
    private IEnumerator FadeInDialog(GameObject dialog)
    {
        if(dialog == null) yield break;
        
        CanvasGroup canvasGroup = dialog.GetComponent<CanvasGroup>();
        if(canvasGroup == null)
        {
            canvasGroup = dialog.AddComponent<CanvasGroup>();
        }
        
        // 시작 시 투명하게
        canvasGroup.alpha = 0f;
        
        yield return canvasGroup.DOFade(1f, DialogFadeInDuration)
            .SetEase(DialogEaseType)
            .WaitForCompletion();
    }
    
    // 창 타입에 맞는 Prefab 반환
    private GameObject GetDialogPrefab(DialogType type)
    {
        switch(type)
        {
            case DialogType.Box:
                return BoxDialogPrefab;
            case DialogType.Speech:
                return SpeechDialogPrefab;
            case DialogType.Choice:
                return ChoiceDialogPrefab;
            default:
                return null;
        }
    }
    
    // 창 설정
    private void SetupDialog(GameObject dialog, TutorialSequence sequence)
    {
        // 창 타입에 따라 다른 컴포넌트 사용
        if(sequence.DialogType == DialogType.Box || sequence.DialogType == DialogType.Speech)
        {
            // 단순 텍스트 표시창
            TutorialTextDialog textDialog = dialog.GetComponent<TutorialTextDialog>();
            if(textDialog != null)
            {
                textDialog.Setup(sequence.MessageText, OnDialogClicked);
            }
        }
        else if(sequence.DialogType == DialogType.Choice)
        {
            // 선택창
            TutorialChoiceDialog choiceDialog = dialog.GetComponent<TutorialChoiceDialog>();
            if(choiceDialog != null)
            {
                choiceDialog.Setup(
                    sequence.MessageText,
                    sequence.YesButton1Text,
                    sequence.YesButton2Text,
                    OnDialogClicked
                );
            }
        }
    }
    
    // 창 클릭 시 다음 시퀀스로
    private void OnDialogClicked()
    {
        if(isTransitioning ) return; // 액션 대기 중이면 클릭 무시
        
        if(currentDialog != null) StartCoroutine(FadeOutAndNext());
    }


    // Scripts/Manager/TutorialManager.cs (371번 라인 부근 수정)

    
    // 창을 페이드 아웃한 후 다음 시퀀스로
    private IEnumerator FadeOutAndNext()
    {
        isTransitioning = true;
        
        yield return FadeOutDialog(currentDialog);
        Destroy(currentDialog);
        currentDialog = null;
        
        isTransitioning = false;
        
        ShowSequence(currentSequenceIndex + 1);
    }
    
    // 튜토리얼 완료
    private void CompleteTutorial()
    {
        Debug.Log("[TutorialManager] 튜토리얼 완료!");
     
    SaveManager.SetTutorialCompleted(TutorialType.Story, true);
    
        // 배경과 마지막 창을 페이드 아웃한 후 Scene 전환
        StartCoroutine(FadeOutAndLoadMainHome());
    }
    
    // 페이드 아웃 후 MainHome 로드
    private IEnumerator FadeOutAndLoadMainHome()
    {
        if(currentDialog != null)
        {
            yield return FadeOutDialog(currentDialog);
        }
        
        yield return FadeOutBackground();
        
        SceneManager.LoadScene("MainHome");
    }
    
    // Scene이 파괴될 때 DOTween 정리
    private void OnDestroy()
    {
        DOTween.KillAll();
           if (skipButton != null)
            skipButton.onClick.RemoveListener(OnSkipClicked);
   
    }

 

   


    
}