using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

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
            dbRef = FirebaseDatabase.DefaultInstance.RootReference;
            Debug.Log("firebase database initialized");
        }
        else
        {
            Destroy(gameObject);
        }
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
        if (!NetworkManager.Instance.IsConnected())
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
                data.hasSeenLevel4Ending = true; // endingCompleted 대응
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

    public void GetTopRankings(Action<List<Dictionary<string, object>>> onSuccess, Action<string> onFailure)
    {
        dbRef.Child("rankings")
            .OrderByChild("timeMilliseconds")
            .LimitToFirst(5) //
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    onFailure?.Invoke("랭킹 조회 실패");
                    return;
                }

                List<Dictionary<string, object>> rankingList = new List<Dictionary<string, object>>();
                foreach (var child in task.Result.Children)
                {
                    rankingList.Add(child.Value as Dictionary<string, object>);
                }
                onSuccess?.Invoke(rankingList);
            });
    }
    #endregion
}