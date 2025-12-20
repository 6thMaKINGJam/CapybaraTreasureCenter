using UnityEngine;
using UnityEngine.UI;
using Scripts.UI;


public class LevelSelectPanel : MonoBehaviour
{
    [Header("Level Buttons")]
        [SerializeField] private Button[] levelButtons;
        [SerializeField] private GameObject[] lockVisuals;
        [SerializeField] private GameObject[] clearVisuals; // 클리어 표시용 UI (추가)

        [Header("Common UI")]
        public Button closeButton; 

        // PlayerPrefs Key 이름 확정
        private const string SelectedLevelKey = "SelectedLevel";

        private void Awake()
        {
            // 각 버튼에 클릭 이벤트 동적 연결
            for (int i = 0; i < levelButtons.Length; i++)
            {
                int levelIndex = i + 1;
                levelButtons[i].onClick.AddListener(() => OnLevelClick(levelIndex));
            }
        }

        /// <summary>
        /// 패널이 열릴 때 호출하여 해금 및 클리어 상태 동기화
        /// </summary>
        /// <param name="lastClearedLevel">가장 최근에 클리어한 레벨 번호</param>
        public void RefreshLevelNodes(int lastClearedLevel)
        {
            for (int i = 0; i < levelButtons.Length; i++)
            {
                int currentLevelNum = i + 1;

                // 1. 잠금 해제 규칙: 레벨 1은 항상 해제, N 클리어 시 N+1 해제
                bool isUnlocked = (currentLevelNum == 1) || (lastClearedLevel >= i);
                
                // 버튼 자체의 interactable은 true로 유지하여 잠긴 레벨 클릭 시 팝업을 띄울 수 있게 함
                // (만약 클릭조차 막으려면 levelButtons[i].interactable = isUnlocked; 사용)

                // 2. 잠금 상태 표시 (자물쇠 아이콘 등)
                if (lockVisuals.Length > i && lockVisuals[i] != null)
                {
                    lockVisuals[i].SetActive(!isUnlocked);
                }

                // 3. 클리어 표시 (별이나 '완료' 텍스트 등)
                if (clearVisuals.Length > i && clearVisuals[i] != null)
                {
                    clearVisuals[i].SetActive(lastClearedLevel >= currentLevelNum);
                }
            }
        }

        /// <summary>
        /// 레벨 버튼 클릭 시 동작
        /// </summary>
        private void OnLevelClick(int levelNum)
        {
            // 현재 해금 상태를 확인하기 위해 ProgressData 다시 체크 (또는 상태 저장 변수 활용)
            ProgressData data = SaveManager.LoadData<ProgressData>("ProgressData");
            bool isUnlocked = (levelNum == 1) || (data.LastClearedLevel >= levelNum - 1);

            if (isUnlocked)
            {
                // 해제된 레벨: PlayerPrefs 저장 후 이동
                PlayerPrefs.SetInt(SelectedLevelKey, levelNum);
                PlayerPrefs.Save();
                SceneManager.LoadScene("GameScene");
            }
            else
            {
                // 잠긴 레벨: 안내 팝업 출력 (2-D 팝업 시스템 활용)
                Debug.Log($"{levelNum - 1} 레벨을 먼저 클리어해야 합니다.");
                // BaseWarningPopup.Instance.Show("이전 레벨을 먼저 클리어하세요!"); 
            }
        }
}
