using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class LevelButtonPrefab : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private GameObject starParent;
    [SerializeField] private Image starImage;
    [SerializeField] private GameObject lockIcon;
    [SerializeField] private Button button;
    
    [Header("별 스프라이트")]
    [SerializeField] private Sprite star1Sprite;
    [SerializeField] private Sprite star2Sprite;
    [SerializeField] private Sprite star3Sprite;
    
    /// <summary>
    /// 레벨 버튼 초기화
    /// </summary>
    /// <param name="levelNum">레벨 번호</param>
    /// <param name="isUnlocked">해금 여부</param>
    /// <param name="starCount">별 개수 (0=미클리어, 1~3=클리어)</param>
    public void Setup(int levelNum, bool isUnlocked, int starCount)
    {
        // 기존 리스너 제거
        button.onClick.RemoveAllListeners();
        
        // 레벨 번호
        levelText.text = levelNum.ToString();
        
        // 잠금 상태
        lockIcon.SetActive(!isUnlocked);
        button.interactable = isUnlocked;
        
        if(isUnlocked)
        {
            // 별 표시
            UpdateStarDisplay(starCount);
            
            // 클릭 이벤트
            button.onClick.AddListener(() => OnLevelClick(levelNum));
        }
        else
        {
            // 잠금 상태면 별 숨김
            starParent.SetActive(false);
        }
    }
    
    private void UpdateStarDisplay(int starCount)
    {
        if(starCount > 0)
        {
            // 1. 부모 먼저 활성화
            starParent.SetActive(true);
            
            // 2. 자식 이미지 활성화
            starImage.gameObject.SetActive(true);
            
            // 3. 스프라이트 변경
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
                    Debug.LogWarning($"[LevelButtonPrefab] 잘못된 별 개수: {starCount}");
                    starParent.SetActive(false);
                    break;
            }
        }
        else
        {
            // 미클리어면 별 숨김
            starParent.SetActive(false);
        }
    }
    
    private void OnLevelClick(int levelNum)
    {
        PlayerPrefs.SetInt("SelectedLevel", levelNum);
        PlayerPrefs.Save();
        
        SceneManager.LoadScene("LevelMode");
    }
}