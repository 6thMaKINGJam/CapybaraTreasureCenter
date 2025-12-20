using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class HallOfFamePanel : MonoBehaviour
{
    public Button closeButton;
    
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject recordPrefab;
    [SerializeField] private GameObject emptyText;
    [SerializeField] private GameObject loadingUI;
    
    [Header("CapyDialogue 연결")]
    public CapyDialogue CapyDialogue;
    public TMPro.TextMeshProUGUI CapyDialogueText;

    private string myPlayerId;

    public void Open()
    {
        // 1. 네트워크 체크
        if(!NetworkManager.Instance.IsNetworkAvailable())
        {
            Debug.LogWarning("네트워크 연결 필요 팝업 출력");
            
            GameObject popupObj = Instantiate(Resources.Load<GameObject>("Prefabs/UI/BaseWarningPopup"));
            BaseWarningPopup popup = popupObj.GetComponent<BaseWarningPopup>();
            popup.Setup("네트워크 연결이 필요합니다카피!", null);
            
            return;
        }

        // 2. 초기화 및 로딩 표시
        gameObject.SetActive(true);
        loadingUI?.SetActive(true);
        emptyText?.SetActive(false);
        myPlayerId = PlayerPrefs.GetString("playerId", "");
        
        // ===== CapyDialogue 연결 =====
        if(CapyDialogue != null && CapyDialogueText != null)
        {
            CapyDialogue.ShowDialogue(CapyDialogueText, DialogueType.HallOfFame);
        }

        // 3. 데이터 로드
        RankingManager.Instance.GetTopRankings(OnLoadSuccess, OnLoadFailed);
    }

    private void OnLoadSuccess(List<Dictionary<string, object>> rankingList)
    {
        loadingUI?.SetActive(false);

        // 기존 리스트 삭제
        foreach(Transform child in contentParent) Destroy(child.gameObject);

        if(rankingList.Count == 0)
        {
            emptyText?.SetActive(true);
            return;
        }

        // 4. 순위 계산 및 생성 (공동 순위 로직)
        long lastTime = -1;
        int currentRank = 0;

        for(int i = 0; i < rankingList.Count; i++)
        {
            var data = rankingList[i];
            long time = (long)data["timeMilliseconds"];
            string nickname = data["nickname"].ToString();
            
            // 동일 시간이면 동일 순위
            if(time != lastTime)
            {
                currentRank = i + 1;
            }
            lastTime = time;

            // 아이템 생성
            GameObject itemObj = Instantiate(recordPrefab, contentParent);
            RecordItem item = itemObj.GetComponent<RecordItem>();

            // 내 기록 확인 (닉네임으로 비교 - RankingManager에서 playerId 포함하도록 수정 필요)
            bool isMine = (nickname == PlayerPrefs.GetString("MyNickname", ""));

            item.SetData(currentRank, nickname, time, isMine);
        }
    }

    private void OnLoadFailed(string error)
    {
        loadingUI?.SetActive(false);
        Debug.LogError($"랭킹 로드 실패: {error}");
        
        GameObject popupObj = Instantiate(Resources.Load<GameObject>("Prefabs/UI/BaseWarningPopup"));
        BaseWarningPopup popup = popupObj.GetComponent<BaseWarningPopup>();
        popup.Setup("랭킹을 불러오는데 실패했습니다카피!", null);
    }
}