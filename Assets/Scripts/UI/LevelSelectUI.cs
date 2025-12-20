using UnityEngine;
using UnityEngine.UI;

namespace Scripts.UI
{
    public class LevelSelectUI : MonoBehaviour
    {
        [SerializeField] private Button[] levelButtons;
        [SerializeField] private GameObject[] lockVisuals;

        // 패널이 열릴 때 호출하여 해금 상태 동기화
        public void RefreshLevelNodes(int lastClearedLevel)
        {
            for (int i = 0; i < levelButtons.Length; i++)
            {
                // 레벨 1(index 0)은 항상 오픈, 나머지는 클리어 기록에 따라 결정
                bool isUnlocked = (i == 0) || (lastClearedLevel >= i);
                
                levelButtons[i].interactable = isUnlocked;

                if (lockVisuals.Length > i && lockVisuals[i] != null)
                {
                    lockVisuals[i].SetActive(!isUnlocked);
                }
            }
        }
    }
}