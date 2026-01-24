using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System;

public class LevelModeTutorial : MonoBehaviour
{
    [Header("튜토리얼 패널")]
    [SerializeField] private GameObject[] tutorialPanels;
    
    [Header("네비게이션 버튼")]
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private TextMeshProUGUI nextButtonText;
    
    [Header("페이드 설정")]
    [SerializeField] private float fadeDuration = 0.3f;
    
    private int currentPanelIndex = 0;
    private CanvasGroup[] panelCanvasGroups;
    
    public event Action OnTutorialCompleted;
    
    void Start()
    {
        InitializeTutorial();
    }
    
    void InitializeTutorial()
    {
        // CanvasGroup 배열 준비
        panelCanvasGroups = new CanvasGroup[tutorialPanels.Length];
        
        for(int i = 0; i < tutorialPanels.Length; i++)
        {
            // CanvasGroup 가져오거나 추가
            panelCanvasGroups[i] = tutorialPanels[i].GetComponent<CanvasGroup>();
            if(panelCanvasGroups[i] == null)
            {
                panelCanvasGroups[i] = tutorialPanels[i].AddComponent<CanvasGroup>();
            }
            
            // 초기 상태: 모두 비활성화
            tutorialPanels[i].SetActive(false);
            panelCanvasGroups[i].alpha = 0f;
        }
        
        // 버튼 이벤트 연결
        prevButton.onClick.AddListener(OnPrevClick);
        nextButton.onClick.AddListener(OnNextClick);
        
        // 첫 패널 표시
        currentPanelIndex = 0;
        ShowCurrentPanel();
    }
    
    void ShowCurrentPanel()
    {
        // 모든 패널 비활성화
        for(int i = 0; i < tutorialPanels.Length; i++)
        {
            if(i != currentPanelIndex)
            {
                tutorialPanels[i].SetActive(false);
            }
        }
        
        // 현재 패널 활성화 및 페이드 인
        tutorialPanels[currentPanelIndex].SetActive(true);
        panelCanvasGroups[currentPanelIndex].alpha = 0f;
        panelCanvasGroups[currentPanelIndex].DOFade(1f, fadeDuration).SetUpdate(true); 
        
        UpdateButtonStates();
    }
    
    void UpdateButtonStates()
    {
        // 이전 버튼 상태
        prevButton.gameObject.SetActive(currentPanelIndex > 0);
        
        // 다음 버튼 텍스트
        if(currentPanelIndex == tutorialPanels.Length - 1)
        {
            nextButtonText.text = "완료";
        }
        else
        {
            nextButtonText.text = "다음";
        }
    }
    
    void OnPrevClick()
    {
        if(currentPanelIndex <= 0) return;
        
        // 현재 패널 페이드 아웃
        panelCanvasGroups[currentPanelIndex].DOFade(0f, fadeDuration).SetUpdate(true)
            .OnComplete(() => 
            {
                tutorialPanels[currentPanelIndex].SetActive(false);
                currentPanelIndex--;
                ShowCurrentPanel();
            });
    }
    
    void OnNextClick()
    {
        // 마지막 패널이면 완료 처리
        if(currentPanelIndex == tutorialPanels.Length - 1)
        {
            CompleteTutorial();
            return;
        }
        
        // 현재 패널 페이드 아웃
        panelCanvasGroups[currentPanelIndex].DOFade(0f, fadeDuration).SetUpdate(true)
            .OnComplete(() => 
            {
                tutorialPanels[currentPanelIndex].SetActive(false);
                currentPanelIndex++;
                ShowCurrentPanel();
            });
    }
    
    void CompleteTutorial()
    {
        // ProgressData에 완료 플래그 저장
        TutorialProgress tutorialprogress = SaveManager.LoadData<TutorialProgress>("TutorialProgress");
     
        tutorialprogress.levelTutoCompleted = true;
        SaveManager.Save(tutorialprogress, "TutorialProgress");
        
         SaveManager.SetTutorialCompleted(TutorialType.LevelTuto, true);

        Debug.Log("[LevelModeTutorial] 레벨 모드 튜토리얼 완료");
        
        // 마지막 패널 페이드 아웃
        panelCanvasGroups[currentPanelIndex].DOFade(0f, fadeDuration).SetUpdate(true)
            .OnComplete(() => 
            {
                // 완료 이벤트 발생
                OnTutorialCompleted?.Invoke();
                
                // Prefab 삭제
                Destroy(gameObject);
            });
    }
}