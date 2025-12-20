using UnityEngine;
using System.Collections.Generic;

public class RankingTest : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            // 로컬에 저장된 ID를 지워서 새로운 사람인 것처럼 속입니다.
            PlayerPrefs.DeleteKey("playerId"); 
            Debug.Log("<color=yellow>새로운 플레이어 ID로 리셋되었습니다! 이제 1번을 누르면 새 유저로 등록됩니다.</color>");
        }
        // 숫자 1 키를 누르면 랭킹 등록 테스트
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Debug.Log("랭킹 등록 테스트 시작...");
            RankingManager.Instance.RegisterRanking("카피바라킹", 10000, 
                () => Debug.Log("<color=green>랭킹 등록 성공!</color>"),
                (error) => Debug.LogError($"랭킹 등록 실패: {error}"));
        }

        // 숫자 2 키를 누르면 닉네임 중복 체크 테스트
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            TestNicknameCheck("카피바라킹");
        }

        // 숫자 3 키를 누르면 상위 100명 조회 테스트
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            RankingManager.Instance.GetTopRankings(
                (list) => {
                    Debug.Log($"<color=blue>랭킹 조회 성공! 데이터 개수: {list.Count}</color>");
                    foreach (var item in list) {
                        Debug.Log($"닉네임: {item["nickname"]}, 시간: {item["timeMilliseconds"]}");
                    }
                },
                (error) => Debug.LogError($"조회 실패: {error}"));
        }
    }

    private async void TestNicknameCheck(string nickname)
    {
        Debug.Log($"{nickname} 중복 체크 중...");
        bool exists = await RankingManager.Instance.IsNicknameExists(nickname);
        Debug.Log($"중복 여부: {exists}");
    }
}