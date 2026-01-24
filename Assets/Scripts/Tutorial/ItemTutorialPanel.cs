using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ItemTutorialPanel : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Button closeButton; // 닫기 버튼만
      [SerializeField] private TouchHintLoop hintRoutine;
   

    
    public event System.Action OnClosed;
    
    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        
        if (closeButton != null)
            closeButton.onClick.AddListener(HandleCloseClicked);


    }
    
    public void Show()
    {
        gameObject.SetActive(true);
        
        // 페이드인
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            canvasGroup.DOFade(1, 0.3f).SetUpdate(true);
        }
        hintRoutine.Play();
    }
    
    public void Hide()
    {
        if (canvasGroup != null)
        {
            canvasGroup.DOFade(0, 0.2f).SetUpdate(true).OnComplete(() => 
            {
                Destroy(gameObject);
            });
        }
        else
        {
            Destroy(gameObject);
        }

            hintRoutine.Stop();
    }
    
    private void HandleCloseClicked()
    {
        OnClosed?.Invoke();
    }
    
    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(HandleCloseClicked);
    }
}