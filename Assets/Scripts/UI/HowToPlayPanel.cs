using UnityEngine;
using UnityEngine.UI;

public class HowToPlayPanel : MonoBehaviour
{
    [SerializeField] private Image tutorialDisplayImage;
    [SerializeField] private Sprite[] tutorialSprites;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button prevButton;
    public Button closeButton;  // X 버튼
    private int currentTutorialIndex = 0;

    private void Awake()
        {
            // 버튼 클릭 시 로직 연결
            nextButton.onClick.AddListener(NextTutorial);
            prevButton.onClick.AddListener(PrevTutorial);
            // closeButton은 MainHomeManager에서 일괄 연결하므로 여기서는 생략 가능합니다.
        }

    public void Init() // 패널이 열릴 때 초기화
    {
        currentTutorialIndex = 0;
        UpdateTutorialUI();
    }

    public void NextTutorial() {
        if (currentTutorialIndex < tutorialSprites.Length - 1) {
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
            if (tutorialSprites.Length > 0){
                tutorialDisplayImage.sprite = tutorialSprites[currentTutorialIndex];
            }

            // 첫/마지막 페이지에서 버튼 비활성화
            prevButton.interactable = (currentTutorialIndex > 0);
            nextButton.interactable = (currentTutorialIndex < tutorialSprites.Length - 1);
        }
}