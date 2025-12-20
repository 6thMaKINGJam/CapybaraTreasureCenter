using UnityEngine;
using UnityEngine.UI;

public class HowToPlayUI : MonoBehaviour
{
    [SerializeField] private Image tutorialDisplayImage;
    [SerializeField] private Sprite[] tutorialSprites;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button prevButton;
    private int currentTutorialIndex = 0;

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

    private void UpdateTutorialUI() {
        tutorialDisplayImage.sprite = tutorialSprites[currentTutorialIndex];
        prevButton.interactable = (currentTutorialIndex > 0);
        nextButton.interactable = (currentTutorialIndex < tutorialSprites.Length - 1);
    }
}