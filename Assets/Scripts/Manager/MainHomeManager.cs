using UnityEngine;
using Scripts.UI;

public class MainHomeManager : MonoBehaviour
{
    [SerializeField] private MainHomeUI mainHomeUI;
    [SerializeField] private LevelSelectUI levelSelectUI;
    [SerializeField] private HowToPlayUI howToPlayUI;

    private ProgressData currentProgress;

    void Start()
    {
        // 1. 데이터 로드
        currentProgress = SaveManager.LoadData<ProgressData>("ProgressData");
        
        // 2. UI 버튼들에 기능(함수) 연결
        SetupButtons();

        // 3. 초기 화면 설정
        mainHomeUI.ShowMain();
    }

    private void SetupButtons()
    {
        // UI 클래스가 들고 있는 버튼에 매니저의 함수를 연결합니다.
        mainHomeUI.startButton.onClick.AddListener(OnClickGameStart);
        mainHomeUI.howToPlayButton.onClick.AddListener(OnClickHowToPlay);
        mainHomeUI.hallOfFameButton.onClick.AddListener(OnClickHallOfFame);
    }

    public void OnClickGameStart()
    {
        mainHomeUI.OpenPanel(mainHomeUI.LevelSelectPanel);
        levelSelectUI.RefreshLevelNodes(currentProgress.LastClearedLevel);
    }

    public void OnClickHowToPlay()
    {
        mainHomeUI.OpenPanel(mainHomeUI.HowToPlayPanel);
        howToPlayUI.Init();
    }

    public void OnClickHallOfFame()
    {
        mainHomeUI.OpenPanel(mainHomeUI.HallOfFamePanel);
        // hallOfFameUI.SetupRecords(currentProgress.level4ClearTimes);
    }
}