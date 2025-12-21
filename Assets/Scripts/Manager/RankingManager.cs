using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;
using System.Reflection.PortableExecutable;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;

public class RankingManager : MonoBehaviour
{
    public static RankingManager Instance { get; private set; }

    private DatabaseReference dbRef;
    private const string PlayerIdKey = "playerId";
    
    private string playerId;

    private void Awake()
{
    if (Instance == null)
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializePlayerId();

        // [핵심] JSON 파일 대신 코드로 URL을 직접 박아넣습니다.
        string firebaseUrl = "https://capybaratreasurecenter-default-rtdb.firebaseio.com";
        
        try {
            // GetInstance에 URL을 직접 전달하여 예외를 방지합니다.
            dbRef = FirebaseDatabase.GetInstance(firebaseUrl).RootReference;
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
        if (!NetworkManager.Instance.IsNetworkAvailable())
        {
            onFailure?.Invoke("네트워크 연결이 없어 자동 반영 대기 모드로 전환됩니다.");
            return;
        }

        // 기존 기록 확인 후 갱신 로직 실행
        dbRef.Child("rankings").Child(playerId).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                onFailure?.Invoke("데이터 조회 실패");
                return;
            }

            bool shouldUpdate = true;
            if (task.Result.Exists)
            {
                long existingTime = long.Parse(task.Result.Child("timeMilliseconds").Value.ToString());
                // 현재 기록이 더 짧은(좋은) 경우에만 갱신
                shouldUpdate = score < existingTime;
            }

            if (shouldUpdate)
            {
                UploadToFirebase(nickname, score, onSuccess, onFailure);
            }
            else
            {
                onSuccess?.Invoke(); // 기존 기록이 더 좋으므로 바로 성공 처리
            }
        });
    }

    private void UploadToFirebase(string nickname, long score, Action onSuccess, Action<string> onFailure)
    {
        Dictionary<string, object> entry = new Dictionary<string, object>
        {
            { "nickname", nickname },
            { "timeMilliseconds", score },
            { "timestamp", ServerValue.Timestamp } //
        };

        dbRef.Child("rankings").Child(playerId).UpdateChildrenAsync(entry).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && !task.IsFaulted)
            {
                // 업로드 성공 시 ProgressData 업데이트
                var data = SaveManager.LoadData<ProgressData>("ProgressData");
                data.EndingCompleted = true; // endingCompleted 대응
                SaveManager.Save(data, "ProgressData");
                
                onSuccess?.Invoke();
            }
            else
            {
                onFailure?.Invoke(task.Exception?.ToString());
            }
        });
    }
    #endregion

    #region 추가 요청 기능: 닉네임 중복 체크
    
    /// 기존에 동일한 닉네임이 존재하는지 체크

    public async Task<bool> IsNicknameExists(string nickname)
    {
        var dataSnapshot = await dbRef.Child("rankings")
            .OrderByChild("nickname")
            .EqualTo(nickname)
            .GetValueAsync();

        return dataSnapshot.Exists;
    }
    #endregion

    #region 기능 3: 랭킹 조회

    /// 시간 기준 상위 5명 반환
    public void GetTopAndMyRanking(Action<List<Dictionary<string, object>>, Dictionary<string, object>, int> onComplete, Action<string> onFailure)
{
    Debug.Log("<color=yellow>1. 서버 데이터 요청 시작</color>");
    
    // reference를 가져올 때 dbRef(이미 Awake에서 검증됨)를 사용하는 것이 더 안전합니다.
    dbRef.Child("rankings").OrderByChild("timeMilliseconds").GetValueAsync().ContinueWithOnMainThread(task =>
    {
        // 1. 서버 연결 자체 실패 체크
        if (task.IsFaulted || task.IsCanceled) 
        { 
            string error = task.Exception?.ToString() ?? "Unknown Error";
            Debug.LogError($"서버 응답 실패: {error}");
            onFailure?.Invoke("서버 연결 실패카피!"); 
            return; 
        }

        Debug.Log($"<color=green>2. 데이터 수신 성공! 자식 수: {task.Result.ChildrenCount}</color>");

        List<Dictionary<string, object>> top5List = new List<Dictionary<string, object>>();
        Dictionary<string, object> myData = null;
        int myRank = 0;
        int count = 0;
        
        // PlayerIdKey 상수를 사용하거나 초기화 때 쓴 키와 정확히 일치시켜야 함
        string myId = PlayerPrefs.GetString("playerId", "");

        foreach (var child in task.Result.Children)
        {
            count++;
            
            // [중요 수정] 'as Dictionary' 대신 수동으로 데이터를 매핑하여 에러 방지
            Dictionary<string, object> data = new Dictionary<string, object>();
            data["id"] = child.Key;
            data["nickname"] = child.Child("nickname").Value?.ToString() ?? "익명";
            
            // 숫자 데이터는 안전하게 파싱
            if (child.Child("timeMilliseconds").Value != null)
            {
                data["timeMilliseconds"] = Convert.ToInt64(child.Child("timeMilliseconds").Value);
            }
            else
            {
                data["timeMilliseconds"] = 0L;
            }

            // 상위 5명 리스트에 추가
            if (count <= 5) top5List.Add(data);

            // 내 데이터 찾기
            if (child.Key == myId)
            {
                myData = data;
                myRank = count;
                Debug.Log($"내 랭킹 발견: {myRank}위");
            }
        }

        Debug.Log($"3. 가공 완료: Top5({top5List.Count}명), 내 순위({myRank})");
        onComplete?.Invoke(top5List, myData, myRank);
    });
}
    #endregion
}