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

        /// <summary>
        /// 현재 활성화된 씬을 다시 로드 (새로 시작)
        /// </summary>
        public void RestartCurrentLevel()
        {
            Time.timeScale = 1f; // 시간 흐름 복구
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        /// <summary>
        /// 메인 홈 화면으로 이동
        /// </summary>
        public void GoToMainHome()
        {
            Time.timeScale = 1f; // 시간 흐름 복구
            SceneManager.LoadScene("MainHome");
        }
    }
}