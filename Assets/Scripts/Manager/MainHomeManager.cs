using UnityEngine;
using Scripts.UI;

public class MainHomeManager : MonoBehaviour
{
    [SerializeField] private MainHomeUI mainHomeUI;
    [SerializeField] private LevelSelectUI levelSelectUI;
    [SerializeField] private HowToPlayUI howToPlayUI;
    [SerializeField] private HallOfFameUI hallOfFameUI;

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
        // 1. 메인 패널 버튼들
        mainHomeUI.startButton.onClick.AddListener(OnClickGameStart);
        mainHomeUI.howToPlayButton.onClick.AddListener(OnClickHowToPlay);
        mainHomeUI.hallOfFameButton.onClick.AddListener(OnClickHallOfFame);

        // 2. 각 패널의 X(닫기) 버튼들 연결 -> 공통적으로 메인 화면을 보여줌
        levelSelectUI.closeButton.onClick.AddListener(mainHomeUI.ShowMain);
        howToPlayUI.closeButton.onClick.AddListener(mainHomeUI.ShowMain);
        hallOfFameUI.closeButton.onClick.AddListener(mainHomeUI.ShowMain);
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

    // TODO: 엔딩씬 봤는지 확인하고 보지 않았다면 엔딩씬으로 이동하는 코드 필요

}