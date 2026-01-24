using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class AppleTutorialManager : MonoBehaviour
{
    [Header("Tutorial Steps")]
    [SerializeField] private List<TutorialStepPanel> tutorialSteps;
    
    [Header("Common UI")]
    [SerializeField] private GameObject dimBackground;
    [SerializeField] private Button skipButton;
    
    private int currentStepIndex = 0;
    private bool isTutorialActive = false;

   
    
    private void Awake()
    {
        // 모든 단계 비활성화
        foreach (var step in tutorialSteps)
        {
            if (step != null)
                step.gameObject.SetActive(false);
        }
        
        if (dimBackground != null)
            dimBackground.SetActive(false);
        
        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(false);
            skipButton.onClick.AddListener(OnSkipClicked);
        }
    }
    
    private void Start()
    {
        
            StartTutorial();
        
    }
    
    public void StartTutorial()
    {
        if (SaveManager.IsTutorialCompleted(TutorialType.Apple))
            return;
        
       
        if (isTutorialActive)
            return;
        
        currentStepIndex = 0;
        isTutorialActive = true;
        
        if (dimBackground != null)
            dimBackground.SetActive(true);
        
        if (skipButton != null)
            skipButton.gameObject.SetActive(true);
        
        ShowCurrentStep();
    }
    
    private void ShowCurrentStep()
    {
        if (currentStepIndex >= tutorialSteps.Count)
        {
            CompleteTutorial();
            return;
        }
        
        TutorialStepPanel currentPanel = tutorialSteps[currentStepIndex];
        currentPanel.OnCompleted += OnStepCompleted;
        currentPanel.Show();
    }
    
    private void OnStepCompleted()
    {
        if (currentStepIndex < tutorialSteps.Count)
        {
            tutorialSteps[currentStepIndex].OnCompleted -= OnStepCompleted;
            tutorialSteps[currentStepIndex].Hide();
        }
        
        currentStepIndex++;
        ShowCurrentStep();
    }
    
    private void OnSkipClicked()
    {
        if (currentStepIndex < tutorialSteps.Count)
        {
            tutorialSteps[currentStepIndex].OnCompleted -= OnStepCompleted;
            tutorialSteps[currentStepIndex].Hide();
        }
        
        CompleteTutorial();
    }
    
    private void CompleteTutorial()
    {
        
        
        if (dimBackground != null)
            dimBackground.SetActive(false);
        
        if (skipButton != null)
            skipButton.gameObject.SetActive(false);
        
        isTutorialActive = false;

        // ✅ Level 1 설정 후 LevelMode 씬으로 이동
    
   
   SaveManager.SetTutorialCompleted(TutorialType.Apple, true);

        Destroy(gameObject);
        PlayerPrefs.SetInt("SelectedLevel", 1);
    PlayerPrefs.Save();
   
   SceneManager.LoadScene("LevelMode");

    }
    
    private void OnDestroy()
    {
        if (skipButton != null)
            skipButton.onClick.RemoveListener(OnSkipClicked);
        
        // 이벤트 정리
        foreach (var step in tutorialSteps)
        {
            if (step != null)
                step.OnCompleted -= OnStepCompleted;
        }
    }
}