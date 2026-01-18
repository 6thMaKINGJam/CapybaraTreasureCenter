using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class TutorialStepPanel : MonoBehaviour
{
    [Header("필요한 기존 패널")]
    [SerializeField] private GameObject[] requiredPanels;
    
    [Header("UI 요소")]
    [SerializeField] private Button nextButton;
    [SerializeField] private CanvasGroup canvasGroup;
    
    public event System.Action OnCompleted;
    
    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        
        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextClicked);
    }
    
    public void Show()
    {
        // 필요한 패널 활성화
        if (requiredPanels != null)
        {
            foreach (var panel in requiredPanels)
            {
                if (panel != null)
                    panel.SetActive(true);
            }
        }
        
        gameObject.SetActive(true);
        
        // 페이드인 애니메이션
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            canvasGroup.DOFade(1, 0.3f);
        }
    }
    
    public void Hide()
    {
        if (canvasGroup != null)
        {
            canvasGroup.DOFade(0, 0.2f).OnComplete(() => 
            {
                gameObject.SetActive(false);
            });
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
    
    private void OnNextClicked()
    {
        OnCompleted?.Invoke();
    }
    
    private void OnDestroy()
    {
        if (nextButton != null)
            nextButton.onClick.RemoveListener(OnNextClicked);
    }
}