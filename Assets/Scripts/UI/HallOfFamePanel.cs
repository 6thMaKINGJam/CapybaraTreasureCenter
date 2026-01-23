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
    public TMP_Text MyNicknameText;
    public TMP_Text MyScoreText;
    public TMP_Text MyRankText;
    
    [Header("Ranking Areas")]
    [SerializeField] private Transform topRankContent; // 상위 5명 부모
    [SerializeField] private Transform myRankContent;  // 내 등수 부모

    [SerializeField] private GameObject loadingUI;
    [SerializeField] private GameObject emptyText;

public void OnEnable()
    {
        closeButton.onClick.AddListener(() => 
        {
            gameObject.SetActive(false);
        });
    }   
    public void Open()
    {
        gameObject.SetActive(true);
        loadingUI?.SetActive(true);
        emptyText?.SetActive(false);

        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(() => { gameObject.SetActive(false); });

        RankingManager.Instance.GetTopAndMyRanking((top5, myData, myRank) => {
            loadingUI?.SetActive(false);
            UpdateUI(top5, myData, myRank);
        }, (error) => {
            loadingUI?.SetActive(false);
            Debug.LogError(error);
        });
    }

    public void UpdateUI(List<Dictionary<string, object>> top5, Dictionary<string, object> myData, int myRank)
    {
        // 1. 1등부터 5등까지 반복하며 텍스트 업데이트
        for (int i = 0; i < 5; i++)
        {
            // 인스펙터에 리스트 크기가 5개로 설정되어 있는지 확인
            if (i >= NicknameTexts.Count || i >= ScoreTexts.Count) break;

            if (i < top5.Count)
            {
                // 데이터가 있는 경우
                var entry = top5[i];
                
                // KeyNotFoundException 방지를 위해 ContainsKey 확인
                string name = entry.ContainsKey("nickname") ? entry["nickname"].ToString() : "Unknown";
                string score = entry.ContainsKey("score") ? entry["score"].ToString() : "0";

                NicknameTexts[i].text = name;
                ScoreTexts[i].text = score;
                
                // 해당 줄 활성화
                NicknameTexts[i].gameObject.SetActive(true);
                ScoreTexts[i].gameObject.SetActive(true);
            }
            else
            {
                // 데이터가 없는 빈 등수는 텍스트를 비우거나 비활성화
                NicknameTexts[i].text = "-";
                ScoreTexts[i].text = "-";
            }
        }

        // 2. 내 점수 정보 업데이트
        UpdateMyDataUI(myData, myRank);
    }

    private void UpdateMyDataUI(Dictionary<string, object> myData, int myRank)
    {
        if (myData == null) return;

        // 'id'나 'nickname' 중 있는 것을 사용하도록 안전하게 처리
        string myName = "기록 없음";
        if (myData.ContainsKey("nickname")) myName = myData["nickname"].ToString();
        else if (myData.ContainsKey("id")) myName = myData["id"].ToString();

        string myScore = myData.ContainsKey("score") ? myData["score"].ToString() : "0";

        if (MyNicknameText != null) MyNicknameText.text = myName;
        if (MyScoreText != null) MyScoreText.text = myScore;
        if (MyRankText != null) MyRankText.text = myRank > 0 ? $"{myRank}위" : "순위 밖";
    }
}

