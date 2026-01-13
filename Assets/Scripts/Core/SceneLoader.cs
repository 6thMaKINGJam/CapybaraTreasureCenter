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

        public void ExecuteUndo1()
        {
            // 현재 씬에 levelModeManager 있으면 실행
            if (LevelModeManager.Instance != null)
            {
                LevelModeManager.Instance.Process1Undo();
                Debug.Log("[Core] 레벨 모드 Undo1 실행");
            }
            // 없으면 ChallengeModeManager가 있는지 확인 후 실행
            else if (ChallengeModeManager.Instance != null)
            {
                ChallengeModeManager.Instance.Process1Undo();
                Debug.Log("[Core] 챌린지 모드 Undo1 실행");
            }
        }

        

    }
}