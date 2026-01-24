using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class HallOfFamePanel : MonoBehaviour
{
    public Button closeButton;

    [Header("1등 ~ 5등 닉네임 텍스트 (인스펙터 연결)")]
    public List<TMP_Text> NicknameTexts; // 인스펙터에서 5개의 TextMeshPro 요소를 드래그하세요.

    [Header("1등 ~ 5등 점수 텍스트 (인스펙터 연결)")]
    public List<TMP_Text> ScoreTexts; // 인스펙터에서 5개의 TextMeshPro 요소를 드래그하세요. 

    [Header("My Ranking UI")]
    public GameObject MyRankingPanel;
    public TMP_Text MyNicknameText;
    public TMP_Text MyScoreText;
    public TMP_Text MyRankText;

    private bool hasClearedChallenge = false; // 서버에서 받은 내 랭킹 데이터 기준
    private bool hasClearedInLocal = false;   // 로컬 progressData.json 기준
        
    [Header("Ranking Areas")]
    [SerializeField] private Transform topRankContent; // 상위 5명 부모
    [SerializeField] private Transform myRankContent;  // 내 등수 부모
    public Button myRankingCloseButton;

    [SerializeField] private GameObject loadingUI;
    [SerializeField] private GameObject emptyText;

    private bool isPopupActive = false;
    private bool hasServerData = false;

    public void OnEnable()
    {
        closeButton.onClick.AddListener(() => 
        {
            gameObject.SetActive(false);
        });

        // 내 랭킹 패널 닫기 버튼 이벤트 연결
        if (myRankingCloseButton != null)
        {
            myRankingCloseButton.onClick.RemoveAllListeners();
            myRankingCloseButton.onClick.AddListener(OnCloseMyRanking);
        }
    }   
    public void Open()
    {
        gameObject.SetActive(true);
        loadingUI?.SetActive(true);
        emptyText?.SetActive(false);
        hasServerData = false;

        // 1. 서버 데이터를 가져오기 전, 먼저 로컬 progressData.json에서 클리어 정보를 확인합니다.
        CheckLocalProgress();

        RankingManager.Instance.GetTopAndMyRanking((top5, myData, myRank) => {
            loadingUI?.SetActive(false);
            if (myData != null) 
            {
                hasServerData = true; 
            }
            UpdateUI(top5, myData, myRank);
        }, (error) => {
            loadingUI?.SetActive(false);
            Debug.LogError(error);
        });
    }

    private void CheckLocalProgress()
    {
        ProgressData progressData = SaveManager.LoadData<ProgressData>("ProgressData");
        if (progressData != null)
        {
            // 닉네임을 등록(클리어)한 적이 있는지 확인
            hasClearedInLocal = progressData.EndingCompleted;
            
            if (hasClearedInLocal)
            {
                MyNicknameText.text = progressData.MyNickname;
                MyScoreText.text = progressData.BestTime.ToString();
                MyRankText.text = "- 위"; 
            }
        }
    }

    public void UpdateUI(List<Dictionary<string, object>> top5, Dictionary<string, object> myData, int myRank)
    {
        // 1. 상위 5명 UI 업데이트
        for (int i = 0; i < 5; i++)
        {
            if (i >= NicknameTexts.Count || i >= ScoreTexts.Count) break;

            if (i < top5.Count)
            {
                var entry = top5[i];
                NicknameTexts[i].text = entry.ContainsKey("nickname") ? entry["nickname"].ToString() : "Unknown";
                ScoreTexts[i].text = entry.ContainsKey("score") ? entry["score"].ToString() : "0";
                
                NicknameTexts[i].gameObject.SetActive(true);
                ScoreTexts[i].gameObject.SetActive(true);
            }
            else
            {
                NicknameTexts[i].text = "-";
                ScoreTexts[i].text = "-";
            }
        }

        // 2. 내 데이터 UI 업데이트
        // 서버 데이터(myData)가 있으면 그것을 우선하고, 없으면 로컬 데이터를 사용합니다.
        if (myData != null)
        {
            MyNicknameText.text = myData.ContainsKey("nickname") ? myData["nickname"].ToString() : "Unknown";
            MyScoreText.text = myData.ContainsKey("score") ? myData["score"].ToString() : "0";
            
            // [수정] myRank 가 0보다 크면 즉시 반영
            if (myRank > 0)
            {
                MyRankText.text = $"{myRank}위";
                Debug.Log($"내 등수 확인됨: {myRank}");
            }
            else
            {
                // 데이터는 찾았으나 등수 계산이 밀린 경우
                MyRankText.text = "순위 산정 중";
            }
        }
        else
        {
            // 서버에서 내 데이터를 아예 못 찾은 경우
            MyRankText.text = "미등록";
        }
    }

    // 내 랭킹보기 버튼 클릭 시 호출
    public void OnClickSeeMyRanking()
    {
        if (isPopupActive) return;

        // 로컬 클리어 기록이 있거나 서버 데이터가 있는 경우
        if (hasClearedInLocal || hasServerData)
        {
            if (topRankContent != null) topRankContent.gameObject.SetActive(false); // 상위 5명 숨기기
            if (MyRankingPanel != null) MyRankingPanel.SetActive(true); // 내 랭킹 표시
        }
        else
        {
            ShowChallengeWarning();
        }
    }

    // 내 랭킹 패널의 닫기 버튼을 눌렀을 때
    public void OnCloseMyRanking()
    {
        if (MyRankingPanel != null) MyRankingPanel.SetActive(false); // 내 랭킹 숨기기
        if (topRankContent != null) topRankContent.gameObject.SetActive(true); // 상위 5명 다시 표시
    }

    private void ShowChallengeWarning()
    {
        if (PopupParentSetHelper.Instance != null)
        {
            GameObject popupObj = PopupParentSetHelper.Instance.CreatePopup("Prefabs/BaseWarningPopup");
            if (popupObj != null)
            {
                BaseWarningPopup popup = popupObj.GetComponent<BaseWarningPopup>();
                if (popup != null)
                {
                    isPopupActive = true;
                    popup.Setup("챌린지 모드를 클리어해야 명예의 전당에 오를 수 있다 카피", () => 
                    {
                        isPopupActive = false;
                    });
                }
            }
        }
    }
}

