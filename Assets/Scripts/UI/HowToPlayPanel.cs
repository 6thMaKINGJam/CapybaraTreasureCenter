using UnityEngine;
using UnityEngine.UI;

public class HowToPlayPanel : MonoBehaviour
{
    [Header("UI References")]
    // 이제 단일 Image가 아니라, 페이지별로 보여줄 오브젝트(이미지) 배열입니다.
    [SerializeField] private GameObject[] tutorialPages; 
    
    [SerializeField] private Button nextButton;
    [SerializeField] private Button prevButton;
    public Button closeButton;  // X 버튼
    
    private int currentTutorialIndex = 0;

public void OnEnable()
    {
        closeButton.onClick.AddListener(() => 
        {
            gameObject.SetActive(false);
        });
    }  
    
    private void Awake()
    {
        // 버튼 클릭 시 로직 연결
        nextButton.onClick.AddListener(NextTutorial);
        prevButton.onClick.AddListener(PrevTutorial);
    }

    public void Init() // 패널이 열릴 때 초기화
    {
        currentTutorialIndex = 0;
        UpdateTutorialUI();
    }

    public void NextTutorial() {
        if (currentTutorialIndex < tutorialPages.Length - 1) {
            currentTutorialIndex++;
            UpdateTutorialUI();
        }
    }

    public void PrevTutorial() {
        if (currentTutorialIndex > 0) {
            currentTutorialIndex--;
            UpdateTutorialUI();
        }
    }

    private void UpdateTutorialUI()
    {
        // 1. 모든 페이지를 일단 비활성화
        for (int i = 0; i < tutorialPages.Length; i++)
        {
            if (tutorialPages[i] != null)
                tutorialPages[i].SetActive(false);
        }

        // 2. 현재 인덱스에 해당하는 페이지만 활성화
        if (tutorialPages.Length > 0 && tutorialPages[currentTutorialIndex] != null)
        {
            tutorialPages[currentTutorialIndex].SetActive(true);
        }

        // 3. 첫/마지막 페이지에서 버튼 비활성화 상태 업데이트
        prevButton.interactable = (currentTutorialIndex > 0);
        nextButton.interactable = (currentTutorialIndex < tutorialPages.Length - 1);
    }
}