using UnityEngine;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

public class RankingManager : MonoBehaviour
{
    public static RankingManager Instance { get; private set; }


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
       
    }

    private void UploadToFirebase(string nickname, long score, Action onSuccess, Action<string> onFailure)
    {
        
    }
    #endregion

    #region 추가 요청 기능: 닉네임 중복 체크
    
    /// 기존에 동일한 닉네임이 존재하는지 체크

    public async Task<bool> IsNicknameExists(string nickname)
    {
        return false;
    }
    #endregion

    #region 기능 3: 랭킹 조회

    /// 시간 기준 상위 5명 반환
    
    public void GetTopAndMyRanking(Action<List<Dictionary<string, object>>, Dictionary<string, object>, int> onComplete, Action<string> onFailure)
{
}
    #endregion
}