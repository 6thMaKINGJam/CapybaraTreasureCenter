using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine;

public class PausePanel : MonoBehaviour
{
    // 이어하기 
    public void ResumeGame() {
        Time.timeScale = 1f; // 시간 다시 흐르게 하기
        gameObject.SetActive(false); // 팝업 끄기
    }

    // 일시정지 
    public void PauseGame() {
        Time.timeScale = 0f; // 시간 정지
        gameObject.SetActive(true); // 팝업 켜기
    }

    // 새로시작 
    public void RestartGame() {
        Time.timeScale = 1f; //재시작해도 시간은 흐름
        // TODO: 데이터 삭제 로직 추가 필요 (5-D)
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // 메인 홈으로 이동
    public void GoToMainHome()
    {
        Time.timeScale = 1f; // 메인으로 갈 때 시간 정상화
        //게임 현황 삭제?
        SceneManager.LoadScene("MainHome"); // 메인 홈 씬 로드
    }
}
