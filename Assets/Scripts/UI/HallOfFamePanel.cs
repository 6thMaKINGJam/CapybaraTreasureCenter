using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class HallOfFamePanel : MonoBehaviour
{
    public Button closeButton;
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject recordPrefab;
    [SerializeField] private GameObject emptyText; // 데이터 없을 때 안내 문구
    [SerializeField] private GameObject loadingUI;  // 로딩 UI

    private string myPlayerId;

    public void Open()
    {
        // 1. 네트워크 체크
        if (!NetworkManager.Instance.IsNetworkAvailable())
        {
            Debug.LogWarning("네트워크 연결 필요 팝업 출력");
            // BaseWarningPopup.Instance.Show("네트워크 연결이 필요합니다.");
            return;
        }

        // 2. 초기화 및 로딩 표시
        gameObject.SetActive(true);
        loadingUI?.SetActive(true);
        emptyText?.SetActive(false);
        myPlayerId = PlayerPrefs.GetString("playerId", "");

        // 3. 데이터 로드
        RankingManager.Instance.GetTopRankings(OnLoadSuccess, OnLoadFailed);
    }

    private void OnLoadSuccess(List<Dictionary<string, object>> rankingList)
    {
        loadingUI?.SetActive(false);

        // 기존 리스트 삭제
        foreach (Transform child in contentParent) Destroy(child.gameObject);

        if (rankingList.Count == 0)
        {
            emptyText?.SetActive(true);
            return;
        }

        // 4. 순위 계산 및 생성 (공동 순위 로직 포함)
        long lastTime = -1;
        int currentRank = 0;

        for (int i = 0; i < rankingList.Count; i++)
        {
            var data = rankingList[i];
            long time = (long)data["timeMilliseconds"];
            string nickname = data["nickname"].ToString();
            
            // 동일 시간일 시 동일 순위 유지 로직
            if (time != lastTime)
            {
                currentRank = i + 1;
            }
            lastTime = time;

            // 아이템 생성 및 데이터 설정
            GameObject itemObj = Instantiate(recordPrefab, contentParent);
            RecordItem item = itemObj.GetComponent<RecordItem>();

            // 내 아이디 확인 (Firebase 상의 Key 또는 저장된 닉네임 비교)
            // RankingManager에서 데이터를 가져올 때 ID 정보도 포함되도록 수정이 필요할 수 있습니다.
            // 여기서는 단순 비교를 위해 닉네임 등으로 예시를 듭니다.
            bool isMine = false; // (실제 구현 시 데이터 내 ID 필드와 비교)

            item.SetData(currentRank, nickname, time, isMine);
        }
    }

    private void OnLoadFailed(string error)
    {
        loadingUI?.SetActive(false);
        Debug.LogError($"랭킹 로드 실패: {error}");
    }
}
