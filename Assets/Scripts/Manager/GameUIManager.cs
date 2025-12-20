using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance { get; private set; }

    [Header("세부 매니저 연결 (PHASE 3-C, 3-D)")]
    public BundleGridManager GridManager; //하단 12개 그리드 관리
    public SelectedBundlesUIPanel SelectionPanel; //선택된 묶음 패널 관리

    [Header("보석별 총량 표시")] //Sapphire, Ruby, Diamond, Emerald, Amethyst 순서   
    public List<Text> GemCountTexts;

    [Header("상자 정보")]
    public TMP_Text BoxIndexText; //상자 #번호
    public TMP_Text BoxProgressText; //담은 개수 / 총개수

    [Header("상단/하단 버튼들")]
    public Button PauseButton;
    public Button HintButton;
    public Button CancelSelectButton;
    public Button UndoButton;
    public Button CompleteButton;
    public Button RetryButton;

    [Header("팝업 패널")]
    public GameObject PausePopupPanel;
    public GameObject AdPopup;

    [Header("타이머 바")]
    public Slider TimerSlider;


    //singleton
    void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
        }
    }

    //보석별 총량 표시 업데이트
    public void UpdateTotalGemUI(Dictionary<GemType, int> gemCounts) {
        foreach(var gem in gemCounts) { //각 보석이 몇 개인지 확인
            int index = (int)gem.Key; //GemType을 숫자로 변환해서 인덱스로 사용
            if (index < GemCountTexts.Count) {
                GemCountTexts[index].text = gem.Value.ToString(); //남은 개수 저장
                GemCountTexts[index].color = (gem.Value <= 0) ? Color.red : Color.white; //보석의 수가 0개가 되면 강조 표시
            }
        }
    }
    //상자 진행도 표시 업데이트
    public void UpdateBoxUI (int boxNum, int currentBox, int totalBoxes) {
        BoxIndexText.text = "상자 #" + (boxNum + 1);
        BoxProgressText.text = $"{currentCapacity} / {targetCapacity}";

        //totalBoxes = currentBox일 때 (용량 맞을 때) 완료 버튼 활성화
        CompleteButton.interactable = (currentCapacity == targetCapacity);
    }

    // 일시정지 버튼을 눌렀을 때 팝업
    public void OpenPausePopup() {
        if (PausePopupPanel != null) {
            PausePopupPanel.SetActive(true); // 팝업 켜기
            Time.timeScale = 0f;             // 게임 시간 멈추기 (선택 사항)
        }
    }

    public void ClosePausePopup() {
        if (PausePopupPanel != null) {
            PausePopupPanel.SetActive(false); // 팝업 끄기
            Time.timeScale = 1f;              // 게임 시간 다시 재생
        }
    }

    //몇번 이상 힌트, 되돌리기, 새로 고침을 눌렀을 때 광고 팝업이 뜨는 건 구현 안 된 상태
}
