using UnityEngine;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine.Networking;
using System.Collections;

public class RankingManager : MonoBehaviour
{
    public static RankingManager Instance { get; private set; }


    private const string PlayerIdKey = "playerId";
    
    private string playerId;
    private string firebaseUrl = "https://capybaratreasurecenter-default-rtdb.firebaseio.com";

    private void Awake()
{
    if (Instance == null)
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializePlayerId();

        // [핵심] JSON 파일 대신 코드로 URL을 직접 박아넣습니다.
        
        
        try {
      
            Debug.Log("<color=green>Firebase 연결 성공!</color>");
        } catch (System.Exception e) {
            Debug.LogError($"Firebase 연결 실패: {e.Message}");
        }
    }
    else { Destroy(gameObject); }
}

    // playerId 관리: 최초 1회 GUID 생성 및 저장
    private void InitializePlayerId()
    {
        if (!PlayerPrefs.HasKey(PlayerIdKey))
        {
            playerId = Guid.NewGuid().ToString();
            PlayerPrefs.SetString(PlayerIdKey, playerId);
            PlayerPrefs.Save();
        }
        else
        {
            playerId = PlayerPrefs.GetString(PlayerIdKey);
            
        }
    }


    #region 기능 1 & 2: 랭킹 등록 및 갱신
    
    /// 신규 랭킹 등록 메서드

    public void RegisterRanking(string nickname, long score, Action onSuccess, Action<string> onFailure)
    {
        string url = $"{firebaseUrl}/rankings/{playerId}.json";
        RankingData data = new RankingData { nickname = nickname, score = score };
        string json = JsonUtility.ToJson(data);

        StartCoroutine(UploadToFirebaseCoroutine(url, json, onSuccess, onFailure));
    }

    private IEnumerator UploadToFirebaseCoroutine(string url, string json, Action onSuccess, Action<string> onFailure)
    {
        using (UnityWebRequest request = UnityWebRequest.Put(url, json))
        {
            request.method = "PUT"; // UnityWebRequest.Put은 기본이 PUT입니다.
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
                onSuccess?.Invoke();
            else
                onFailure?.Invoke(request.error);
        }
    }
    #endregion

    #region 추가 요청 기능: 닉네임 중복 체크
    
    /// 기존에 동일한 닉네임이 존재하는지 체크

    public async Task<bool> IsNicknameExists(string nickname)
    {
        string url = $"{firebaseUrl}/rankings.json";
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            var operation = request.SendWebRequest();
            while (!operation.isDone) await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success) return false;

            // JSON 데이터 파싱 (단순화를 위해 string.Contains 또는 정규식 활용 가능)
            // Firebase RTDB는 유니티 JsonUtility로 Dictionary 파싱이 까다로우므로 
            // Newtonsoft.Json(Json.NET) 사용을 권장하지만, 없다면 문자열 검사로 대체 가능합니다.
            return request.downloadHandler.text.Contains($"\"nickname\":\"{nickname}\"");
        }
    }
    #endregion

    #region 기능 3: 랭킹 조회

    /// 시간 기준 상위 5명 반환
    
    public void GetTopAndMyRanking(Action<List<Dictionary<string, object>>, Dictionary<string, object>, int> onComplete, Action<string> onFailure)
    {
        StartCoroutine(GetRankingCoroutine(onComplete, onFailure));
    }
    private IEnumerator GetRankingCoroutine(Action<List<Dictionary<string, object>>, Dictionary<string, object>, int> onComplete, Action<string> onFailure)
    {
        // Firebase에서 점수(score) 기준 내림차순으로 가져오기 위해 쿼리 매개변수 사용
        // 주의: Firebase 규칙 설정에서 "score" 인덱싱이 필요합니다.
        string url = $"{firebaseUrl}/rankings.json?orderBy=\"score\"";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onFailure?.Invoke(request.error);
                yield break;
            }

            string jsonResponse = request.downloadHandler.text;

            // 1. 데이터 파싱 및 정렬 호출
            List<RankingEntry> allRankings = ParseAndSort(jsonResponse);

            // 2. 상위 5명 데이터 추출 (Dictionary 형태로 변환하여 콜백 전달)
            List<Dictionary<string, object>> top5List = allRankings
                .Take(5)
                .Select(entry => new Dictionary<string, object> {
                    { "nickname", entry.Nickname },
                    { "score", entry.Score }
                }).ToList();

            // 3. 내 데이터 및 등수 찾기
            var myEntry = allRankings.FirstOrDefault(x => x.Id == playerId);
            Dictionary<string, object> myDataDict = null;
            int myRank = -1;

            if (myEntry != null)
            {
                myRank = allRankings.IndexOf(myEntry) + 1;
                myDataDict = new Dictionary<string, object> {
                    { "nickname", myEntry.Nickname },
                    { "score", myEntry.Score }
                };
            }

            onComplete?.Invoke(top5List, myDataDict, myRank);
        }
    }
    #endregion

    /// <summary>
    /// Firebase의 중첩된 JSON 구조를 RankingEntry 리스트로 변환하고 점수순으로 정렬합니다.
    /// </summary>
    private List<RankingEntry> ParseAndSort(string json)
    {
        List<RankingEntry> list = new List<RankingEntry>();

        if (string.IsNullOrEmpty(json) || json == "null") return list;

        // 외부 라이브러리 없이 단순 문자열 파싱 (Firebase 특정 구조 처리)
        // 각 사용자의 데이터를 분리하여 처리
        string[] items = json.Split(new string[] { "},\"" }, StringSplitOptions.None);

        foreach (var item in items)
        {
            try
            {
                // ID 추출
                string id = item.Split('"')[0].Replace("{", "").Replace("\"", "").Replace(":", "");
                
                // Nickname 추출 (단순 문자열 찾기)
                int nickStart = item.IndexOf("\"nickname\":\"") + 12;
                int nickEnd = item.IndexOf("\"", nickStart);
                string nickname = item.Substring(nickStart, nickEnd - nickStart);

                // Score 추출
                int scoreStart = item.IndexOf("\"score\":") + 8;
                int scoreEnd = item.IndexOf(",", scoreStart);
                if (scoreEnd == -1) scoreEnd = item.IndexOf("}", scoreStart);
                long score = long.Parse(item.Substring(scoreStart, scoreEnd - scoreStart).Trim());

                list.Add(new RankingEntry { Id = id, Nickname = nickname, Score = score });
            }
            catch (Exception e)
            {
                Debug.LogWarning("파싱 중 오류 발생: " + e.Message);
            }
        }

        // 내림차순 정렬 (높은 점수가 1등)
        return list.OrderByDescending(x => x.Score).ToList();
    }
}

[Serializable]
public class RankingData
{
    public string nickname;
    public long score;
}

// 리스트 정렬 및 등수 계산을 위한 확장 클래스
public class RankingEntry
{
    public string Id;
    public string Nickname;
    public long Score;
}

// Firebase에서 전체 데이터를 받아올 때 사용 (Dictionary 형태)
[Serializable]
public class FirebaseRankingResponse
{
    public Dictionary<string, RankingData> rankings;
}
