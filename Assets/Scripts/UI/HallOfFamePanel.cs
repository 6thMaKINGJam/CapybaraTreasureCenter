using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class HallOfFamePanel : MonoBehaviour
{
    public Button closeButton;
    
    [Header("Ranking Areas")]
    [SerializeField] private Transform topRankContent; // 상위 5명 부모
    [SerializeField] private Transform myRankContent;  // 내 등수 부모

    [SerializeField] private GameObject recordPrefab;
    [SerializeField] private GameObject loadingUI;
    [SerializeField] private GameObject emptyText;

    public void Open()
    {
        gameObject.SetActive(true);
        loadingUI?.SetActive(true);
        emptyText?.SetActive(false);

        RankingManager.Instance.GetTopAndMyRanking((top5, myData, myRank) => {
            loadingUI?.SetActive(false);
            UpdateUI(top5, myData, myRank);
        }, (error) => {
            loadingUI?.SetActive(false);
            Debug.LogError(error);
        });
    }

    private void UpdateUI(List<Dictionary<string, object>> top5, Dictionary<string, object> myData, int myRank)
    {
        // 기존 리스트 청소
        foreach (Transform child in topRankContent) Destroy(child.gameObject);
        foreach (Transform child in myRankContent) Destroy(child.gameObject);

        if (top5.Count == 0) { emptyText?.SetActive(true); return; }

        // 1. 상위 5명 생성
        for (int i = 0; i < top5.Count; i++)
        {
            CreateItem(topRankContent, i + 1, top5[i], top5[i]["id"].ToString() == PlayerPrefs.GetString("playerId"));
        }

        // 2. 내 랭킹 하단 고정 생성 (상위에 내가 있더라도 또 생성)
        if (myData != null)
        {
            CreateItem(myRankContent, myRank, myData, true);
        }
    }

    private void CreateItem(Transform parent, int rank, Dictionary<string, object> data, bool isMine)
    {
        var item = Instantiate(recordPrefab, parent).GetComponent<RecordItem>();
        item.SetData(rank, data["nickname"].ToString(), (long)data["timeMilliseconds"], isMine);
    }
}

