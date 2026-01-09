using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core
{
    /// <summary>
    /// 씬 전환 및 재시작 로직을 전담하는 클래스
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        // 일시정지 (Time.timeScale 제어)
        public void TogglePause(bool isPause)
        {
            Time.timeScale = isPause ? 0f : 1f;
            Debug.Log($"[Core] 게임 {(isPause ? "일시정지" : "재개")}");
        }

        /// <summary>
        /// 현재 활성화된 씬을 다시 로드 (새로 시작)
        /// </summary>
        public void RestartCurrentLevel()
        {
            Time.timeScale = 1f; // 시간 흐름 복구
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            Debug.Log("[Core] 현재 레벨 재시작");  
        }

        /// <summary>
        /// 메인 홈 화면으로 이동
        /// </summary>
        public void GoToMainHome()
        {
            Time.timeScale = 1f; // 시간 흐름 복구
            SceneManager.LoadScene("MainHome");
        }
        public void ExecuteUndo1()
        {
            // 현재 씬에 GameManager가 있으면 실행
            if (GameManager.Instance != null)
            {
                GameManager.Instance.Process1Undo();
                Debug.Log("[Core] 레벨 모드 Undo1 실행");
            }
            // 없으면 ChallengeManager가 있는지 확인 후 실행
            else if (ChallengeManager.Instance != null)
            {
                ChallengeManager.Instance.Process1Undo();
                Debug.Log("[Core] 챌린지 모드 Undo1 실행");
            }
        }

    }
}