using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelSelectPanel : MonoBehaviour
{
    [Header("챕터 버튼")]
    public Button Chapter1Button;
    public Button Chapter2Button;
    public Button Chapter3Button;
    public Button Level100Button;
    
    [Header("챕터 잠금 표시")]
    public GameObject Chapter2Lock;
    public GameObject Chapter3Lock;
    public GameObject Level100Lock;
    
    [Header("Common UI")]
    public Button closeButton;
    
    [Header("챕터 레벨 리스트 팝업 프리팹")]
    public GameObject ChapterLevelListPopupPrefab;
    
    private const string SelectedLevelKey = "SelectedLevel";
    
    private void Awake()
    {
        Chapter1Button.onClick.AddListener(() => OnChapterClick(1));
        Chapter2Button.onClick.AddListener(() => OnChapterClick(2));
        Chapter3Button.onClick.AddListener(() => OnChapterClick(3));
        Level100Button.onClick.AddListener(() => OnChapterClick(100));
        
        closeButton.onClick.AddListener(() => gameObject.SetActive(false));
    }
    
    private void OnEnable()
    {
        RefreshUI();
    }
    
    private void RefreshUI()
    {
        ProgressData progressData = SaveManager.LoadData<ProgressData>("ProgressData");
        int lastClearedLevel = progressData.LastClearedLevel;
        
        // 챕터 2 잠금 (레벨 33 클리어 필요)
        bool chapter2Unlocked = lastClearedLevel >= 33;
        if(Chapter2Lock != null) Chapter2Lock.SetActive(!chapter2Unlocked);
        Chapter2Button.interactable = chapter2Unlocked;
        
        // 챕터 3 잠금 (레벨 66 클리어 필요)
        bool chapter3Unlocked = lastClearedLevel >= 66;
        if(Chapter3Lock != null) Chapter3Lock.SetActive(!chapter3Unlocked);
        Chapter3Button.interactable = chapter3Unlocked;
        
        // 레벨 100 잠금 (레벨 99 클리어 필요)
        bool level100Unlocked = lastClearedLevel >= 99;
        if(Level100Lock != null) Level100Lock.SetActive(!level100Unlocked);
        Level100Button.interactable = level100Unlocked;
    }
    
    private void OnChapterClick(int chapterNumber)
    {
        if(ChapterLevelListPopupPrefab == null)
        {
            Debug.LogError("[LevelSelectPanel] ChapterLevelListPopupPrefab이 없습니다!");
            return;
        }
        
        GameObject popupObj = Instantiate(ChapterLevelListPopupPrefab, transform.parent);
        ChapterLevelListPopup popup = popupObj.GetComponent<ChapterLevelListPopup>();
        
        if(popup != null)
        {
            popup.Setup(chapterNumber);
        }
    }
}