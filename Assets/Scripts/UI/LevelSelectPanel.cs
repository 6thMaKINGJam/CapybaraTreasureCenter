using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelSelectPanel : MonoBehaviour
{
    [Header("챕터 버튼")]
    public Button Chapter1Button;
    public Button Chapter2Button;
    public Button Chapter3Button;
    public Button Level34Button;
    
    [Header("챕터 잠금 표시")]
    public GameObject Chapter2Lock;
    public GameObject Chapter3Lock;
    public GameObject Level34Lock;
    
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
        Level34Button.onClick.AddListener(() => OnChapterClick(34));
        
        closeButton.onClick.AddListener(() => gameObject.SetActive(false));
     
    }
    
    private void OnEnable()
    {
        RefreshUI();
    }
    
    private void RefreshUI()
{
     // 혹시 스케일이 0으로 남는 문제 방지
    Chapter2Button.transform.localScale = Vector3.one;
    Chapter3Button.transform.localScale = Vector3.one;
    Level34Button.transform.localScale = Vector3.one;


    ProgressData progressData = SaveManager.LoadData<ProgressData>("ProgressData");
    int lastClearedLevel = progressData.LastClearedLevel;

BouncyEffect chapter1Effect = Chapter1Button.GetComponent<BouncyEffect>();
    chapter1Effect.StartBounce();
    // 챕터 2 잠금 (레벨 11 클리어 필요)
    bool chapter2Unlocked = lastClearedLevel >= 11;
    if(Chapter2Lock != null) Chapter2Lock.SetActive(!chapter2Unlocked);
    Chapter2Button.interactable = chapter2Unlocked;
    
    // Chapter2Button의 BouncyEffect 제어
    BouncyEffect chapter2Effect = Chapter2Button.GetComponent<BouncyEffect>();
     if (chapter2Unlocked) chapter2Effect.StartBounce();
    else chapter2Effect.StopBounce();

    // 챕터 3 잠금 (레벨 22 클리어 필요)
    bool chapter3Unlocked = lastClearedLevel >= 22;
    if(Chapter3Lock != null) Chapter3Lock.SetActive(!chapter3Unlocked);
    Chapter3Button.interactable = chapter3Unlocked;
    
    // Chapter3Button의 BouncyEffect 제어
    BouncyEffect chapter3Effect = Chapter3Button.GetComponent<BouncyEffect>();
     if (chapter3Unlocked) chapter3Effect.StartBounce();
    else chapter3Effect.StopBounce();

    // 레벨 34 잠금 (레벨 33 클리어 필요)
    bool level34Unlocked = lastClearedLevel >= 33;
  
    if(Level34Lock != null) Level34Lock.SetActive(!level34Unlocked);
    Level34Button.interactable = level34Unlocked;
   
    // Level34Button의 BouncyEffect 제어
    BouncyEffect level34Effect = Level34Button.GetComponent<BouncyEffect>();
     if (level34Unlocked) level34Effect.StartBounce();
    else level34Effect.StopBounce();
}

    
    private void OnChapterClick(int chapterNumber)
    {
        
        // Level 34은 팝업 없이 바로 시작
    if(chapterNumber == 34)
    {
        ProgressData progressData = SaveManager.LoadData<ProgressData>("ProgressData");
        bool level34Unlocked_real = progressData.AreAllLevelsThreeStars(1, 33);
         if(!level34Unlocked_real)
    {
        GameObject warnpopupObj = PopupParentSetHelper.Instance.CreatePopup("Prefabs/BaseWarningPopup");
        BaseWarningPopup warningPopuppopup = warnpopupObj.GetComponent<BaseWarningPopup>();
        warningPopuppopup.Setup("별 99개를 모아야 플레이할 수 있습니다!", null);
        return;
        
    }

        PlayerPrefs.SetInt("SelectedLevel", 34);
        PlayerPrefs.Save();
        SceneManager.LoadScene("LevelMode");
        return;
    }
    
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