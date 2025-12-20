using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Scripts.UI
{
    public class HallOfFameUI : MonoBehaviour
    {
        public Button closeButton; // X 버튼 추가
        [SerializeField] private Transform contentParent; // ScrollView의 Content
        [SerializeField] private GameObject recordPrefab; // 기록 한 줄을 나타낼 프리팹

        public void SetupRecords(List<float> clearTimes)
        {
            // 기존에 생성된 리스트 아이템 삭제 (초기화)
            foreach (Transform child in contentParent)
            {
                Destroy(child.gameObject);
            }

            // 새로운 기록 데이터 바인딩
            for (int i = 0; i < clearTimes.Count; i++)
            {
                GameObject item = Instantiate(recordPrefab, contentParent);
                // TODO: item 내의 Text 컴포넌트에 시간을 입력하는 로직
                // 예: item.GetComponent<RecordItem>().SetData(i + 1, clearTimes[i]);
            }
        }
    }
}