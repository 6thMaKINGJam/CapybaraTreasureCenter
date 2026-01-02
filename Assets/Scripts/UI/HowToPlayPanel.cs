using UnityEngine;
using UnityEngine.UI;

public class HowToPlayPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image tutorialImage;          // 화면에 표시할 단일 Image
    [SerializeField] private Sprite[] tutorialSprites;     // 페이지별 스프라이트 배열

    [SerializeField] private Button nextButton;
    [SerializeField] private Button prevButton;
    public Button closeButton;

    private int currentTutorialIndex = 0;

    private void OnEnable()
    {
        // OnEnable마다 AddListener하면 중복 등록될 수 있어서 한 번만 등록하도록 정리 권장
        // (아래처럼 RemoveAllListeners 후 AddListener 하거나, Awake에서 등록하는 방식 추천)
        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(() =>
        {
            gameObject.SetActive(false);
        });
    }

    private void Awake()
    {
        nextButton.onClick.AddListener(NextTutorial);
        prevButton.onClick.AddListener(PrevTutorial);
    }

    public void Init()
    {
        currentTutorialIndex = 0;
        UpdateTutorialUI();
    }

    public void NextTutorial()
    {
        if (currentTutorialIndex < tutorialSprites.Length - 1)
        {
            currentTutorialIndex++;
            UpdateTutorialUI();
        }
    }

    public void PrevTutorial()
    {
        if (currentTutorialIndex > 0)
        {
            currentTutorialIndex--;
            UpdateTutorialUI();
        }
    }

    private void UpdateTutorialUI()
    {
        if (tutorialSprites == null || tutorialSprites.Length == 0)
        {
            tutorialImage.enabled = false;
            prevButton.interactable = false;
            nextButton.interactable = false;
            return;
        }

        tutorialImage.enabled = true;
        tutorialImage.sprite = tutorialSprites[currentTutorialIndex];

        // (선택) 원본 이미지 비율 유지하고 싶으면
        tutorialImage.preserveAspect = true;

        prevButton.interactable = (currentTutorialIndex > 0);
        nextButton.interactable = (currentTutorialIndex < tutorialSprites.Length - 1);
    }
}
