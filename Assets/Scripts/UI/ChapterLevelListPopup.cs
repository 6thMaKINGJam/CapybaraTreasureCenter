using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

public class ChapterLevelListPopup : MonoBehaviour
{
    [Header("UI 요소")]
    public TextMeshProUGUI TitleText;
    public Transform LevelListContent
    
    ;
    public Button CloseButton;
    
    [Header("레벨 버튼 프리팹")]
    public GameObject LevelButtonPrefab;
    
    [Header("별 스프라이트")]
    public Sprite Star1Sprite;
    public Sprite Star2Sprite;
    public Sprite Star3Sprite;
    
    private int currentChapter;
    private ProgressData progressData;
    
    public void Setup(int chapterNumber)
    {
        currentChapter = chapterNumber;
        progressData = SaveManager.LoadData<ProgressData>("ProgressData");
        
        // 타이틀 설정
        if(chapterNumber == 100)
        {
            TitleText.text = "Lv 100";
        }
        else
        {
            TitleText.text = $"챕터 {chapterNumber}";
        }
        
        // 레벨 버튼 생성
        CreateLevelButtons();
        
        // 닫기 버튼
        CloseButton.onClick.AddListener(() => Destroy(gameObject));
    }
    
    private void CreateLevelButtons()
    {
        // 기존 버튼 정리
        foreach(Transform child in LevelListContent
        
        )
        {
            Destroy(child.gameObject);
        }
        
        int startLevel = GetChapterStartLevel(currentChapter);
        int endLevel = GetChapterEndLevel(currentChapter);
        
        for(int level = startLevel; level <= endLevel; level++)
        {
            CreateLevelButton(level);
        }
    }
    
    private void CreateLevelButton(int levelNum)
    {
        GameObject buttonObj = Instantiate(LevelButtonPrefab, LevelListContent);
        
        // 버튼 컴포넌트
        Button button = buttonObj.GetComponent<Button>();
        
        // 레벨 번호 텍스트
        TextMeshProUGUI levelText = buttonObj.transform.Find("LevelText").GetComponent<TextMeshProUGUI>();
        levelText.text = levelNum.ToString();
        
        // 별 이미지
        Image starImage = buttonObj.transform.Find("StarImage").GetComponent<Image>();
        
        // 잠금 표시
        GameObject lockIcon = buttonObj.transform.Find("LockIcon").gameObject;
        
        // 해금 여부 확인
        bool isUnlocked = (levelNum == 1) || (progressData.LastClearedLevel >= levelNum - 1);
        
        if(isUnlocked)
        {
            lockIcon.SetActive(false);
            button.interactable = true;
            
            // 클리어 여부 확인
            bool isCleared = progressData.HasCleared(levelNum);
            
            if(isCleared)
            {
                int stars = progressData.GetStars(levelNum);
                UpdateStarDisplay(starImage, stars);
            }
            else
            {
                starImage.gameObject.SetActive(false);
            }
            
            // 클릭 이벤트
            button.onClick.AddListener(() => OnLevelClick(levelNum));
        }
        else
        {
            lockIcon.SetActive(true);
            button.interactable = false;
            starImage.gameObject.SetActive(false);
        }
    }
    
    private void UpdateStarDisplay(Image starImage, int starCount)
    {
        starImage.gameObject.SetActive(true);
        
        switch(starCount)
        {
            case 1:
                starImage.sprite = Star1Sprite;
                break;
            case 2:
                starImage.sprite = Star2Sprite;
                break;
            case 3:
                starImage.sprite = Star3Sprite;
                break;
            default:
                starImage.gameObject.SetActive(false);
                break;
        }
    }
    
    private void OnLevelClick(int levelNum)
    {
        PlayerPrefs.SetInt("SelectedLevel", levelNum);
        PlayerPrefs.Save();
        
        SceneManager.LoadScene("LevelMode");
    }
    
    private int GetChapterStartLevel(int chapterNumber)
    {
        switch(chapterNumber)
        {
            case 1: return 1;
            case 2: return 34;
            case 3: return 67;
            case 100: return 100;
            default: return 1;
        }
    }
    
    private int GetChapterEndLevel(int chapterNumber)
    {
        switch(chapterNumber)
        {
            case 1: return 33;
            case 2: return 66;
            case 3: return 99;
            case 100: return 100;
            default: return 33;
        }
    }
}