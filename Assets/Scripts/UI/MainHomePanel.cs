using UnityEngine;
using UnityEngine.UI;

namespace Scripts.UI
{
    public class MainHomePanel : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject levelSelectPanel;
        [SerializeField] private GameObject howToPlayPanel;
        [SerializeField] private GameObject hallOfFamePanel;

        [Header("Main Buttons")]
        // 매니저가 리스너를 달 수 있도록 public으로 선언하거나 Get함수를 만듭니다.
        public Button startButton;
        public Button howToPlayButton;
        public Button hallOfFameButton;

        public void ShowMain()
        {
            SetAllPanelsInactive();
            mainPanel.SetActive(true);
        }

        public void OpenPanel(GameObject targetPanel)
        {
            SetAllPanelsInactive();
            targetPanel.SetActive(true);
        }

        private void SetAllPanelsInactive()
        {
            
            levelSelectPanel.SetActive(false);
            howToPlayPanel.SetActive(false);
            hallOfFamePanel.SetActive(false);
        }

        // 매니저가 패널 객체에 접근하기 위한 프로퍼티
        public GameObject LevelSelectPanel => levelSelectPanel;
        public GameObject HowToPlayPanel => howToPlayPanel;
        public GameObject HallOfFamePanel => hallOfFamePanel;
    }
}