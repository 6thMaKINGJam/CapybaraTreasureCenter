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
    [Header("배경 이미지")]
    

[SerializeField] private GameObject chapter1Background;
[SerializeField] private GameObject chapter2Background;
[SerializeField] private GameObject chapter3Background;

    private int currentChapter;
    private ProgressData progressData;
    
    public void Setup(int chapterNumber)
{
    currentChapter = chapterNumber;
    progressData = SaveManager.LoadData<ProgressData>("ProgressData");
    
    // 배경 설정
    UpdateBackground(chapterNumber);
    
    // 타이틀 설정
    TitleText.text = $"챕터 {chapterNumber}";
    
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

    private void UpdateBackground(int chapterNumber)
{
    
    switch(chapterNumber)
    {
        case 1:
            chapter1Background.SetActive(true);
            chapter2Background.SetActive(false);
            chapter3Background.SetActive(false);
            break;
        case 2:
            chapter1Background.SetActive(false);
            chapter2Background.SetActive(true);
            chapter3Background.SetActive(false);
            break;
        case 3:
            chapter3Background.SetActive(true);
            chapter1Background.SetActive(false);
            chapter2Background.SetActive(false);
            break;
        default:
            Debug.LogWarning($"[ChapterLevelListPopup] 알 수 없는 챕터: {chapterNumber}");
            break;
    }
}
    
  private void CreateLevelButton(int levelNum)
{
    GameObject buttonObj = Instantiate(LevelButtonPrefab, LevelListContent);
    LevelButtonPrefab buttonScript = buttonObj.GetComponent<LevelButtonPrefab>();
    
    if(buttonScript == null)
    {
        Debug.LogError("[ChapterLevelListPopup] LevelButtonPrefab 컴포넌트가 없습니다!");
        return;
    }
    
    // 해금 여부
    bool isUnlocked = (levelNum == 1) || (progressData.LastClearedLevel >= levelNum - 1);
    
    // 별 개수 (미클리어면 0)
    int starCount = 0;
    if(isUnlocked && progressData.HasCleared(levelNum))
    {
        starCount = progressData.GetStars(levelNum);
    }
    
    // Setup 호출
    buttonScript.Setup(levelNum, isUnlocked, starCount);
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
            case 2: return 12;
            case 3: return 22;
            default: return 1;
        }
    }
    
    private int GetChapterEndLevel(int chapterNumber)
    {
        switch(chapterNumber)
        {
            case 1: return 11;
            case 2: return 22;
            case 3: return 33;
            default: return 33;
        }
    }
}