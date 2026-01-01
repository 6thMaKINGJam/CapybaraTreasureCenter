using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class LevelSelectPanel : MonoBehaviour
{
    [Header("Level Buttons")]
    [SerializeField] private Button[] levelButtons;
    [SerializeField] private GameObject[] lockVisuals;
    
    // ✅ 변경: Image 컴포넌트 배열 (각 레벨의 별 표시 이미지)
    [SerializeField] private Image[] starImages; 
    
    // ✅ 추가: 별 개수별 스프라이트
    [Header("Star Sprites")]
    [SerializeField] private Sprite star1Sprite; // 별 1개 이미지
    [SerializeField] private Sprite star2Sprite; // 별 2개 이미지
    [SerializeField] private Sprite star3Sprite; // 별 3개 이미지
    
    [Header("Common UI")]
    public Button closeButton;
    
    private const string SelectedLevelKey = "SelectedLevel";
    
    private void Awake()
    {
        for (int i = 0; i < levelButtons.Length; i++)
        {
            int levelIndex = i + 1;
            levelButtons[i].onClick.AddListener(() => OnLevelClick(levelIndex));
        }
    }
    
    public void RefreshLevelNodes(int lastClearedLevel)
    {
        ProgressData progressData = SaveManager.LoadData<ProgressData>("ProgressData");
        
        for (int i = 0; i < levelButtons.Length; i++)
        {
            int currentLevelNum = i + 1;
            
            // 1. 해금 여부
            bool isUnlocked = (currentLevelNum == 1) || (lastClearedLevel >= currentLevelNum - 1);
            
            // 2. 자물쇠 표시 (해금되면 숨김)
            if (lockVisuals.Length > i && lockVisuals[i] != null)
            {
                lockVisuals[i].SetActive(!isUnlocked);
            }
            
            // 3. 별 표시 (클리어한 레벨만)
            if (starImages.Length > i && starImages[i] != null)
            {
                bool isCleared = progressData.LevelStars.ContainsKey(currentLevelNum);
                
                if (isCleared)
                {
                    int stars = progressData.LevelStars[currentLevelNum];
                    UpdateStarDisplay(starImages[i], stars);
                }
                else
                {
                    // 미클리어 → 별 이미지 숨김
                    starImages[i].gameObject.SetActive(false);
                }
            }
        }
    }
    
    // ✅ 새 메서드: 별 개수에 따라 스프라이트 교체
    private void UpdateStarDisplay(Image starImage, int starCount)
    {
        starImage.gameObject.SetActive(true);
        
        switch(starCount)
        {
            case 1:
                starImage.sprite = star1Sprite;
                break;
            case 2:
                starImage.sprite = star2Sprite;
                break;
            case 3:
                starImage.sprite = star3Sprite;
                break;
            default:
                Debug.LogWarning($"[LevelSelectPanel] 잘못된 별 개수: {starCount}");
                starImage.gameObject.SetActive(false);
                break;
        }
    }
    
    private void OnLevelClick(int levelNum)
    {
        ProgressData data = SaveManager.LoadData<ProgressData>("ProgressData");
        bool isUnlocked = (levelNum == 1) || (data.LastClearedLevel >= levelNum - 1);
        
        if (isUnlocked)
        {
            PlayerPrefs.SetInt(SelectedLevelKey, levelNum);
            PlayerPrefs.Save();
            SceneManager.LoadScene("Game");
        }
        else
        {
            Debug.Log($"{levelNum - 1} 레벨을 먼저 클리어해야 합니다.");
        }
    }
}