using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class SelectedBundlesUIPanel : MonoBehaviour
{
    public GameObject BundlePrefab;
    public Transform PanelParent; //Horizontal Layout Group 기반

    private List<GemBundlePrefab> pool = new List<GemBundlePrefab>();

    //game manager에서 리스트 넘기면 UI 재구성
    public void UpdateUI (List<GemBundle> selectedList) {
        //기존 활성화 되어있던 UI 객체 전부 비활성화
        foreach (var b in pool) b.gameObject.SetActive(false);

        //int totalSum = 0;
        //전달 받은 데이터 리스트의 개수만큼 루프 실행
        for (int i = 0 ; i < selectedList.Count; i++) {

            //pool에서 사용 가능한 객체 획득
            var b = GetFromPool(i);
            //가져온 UI 객체에 실제 보석 데이터(종류, 개수)를 주입
            b.SetData(selectedList[i]);
            //객체 활성화
            b.gameObject.SetActive(true);
        }
    }

    //남아있는 객체가 있는지 확인 후 반환
    public GemBundlePrefab GetFromPool(int index) {
        //
        if (index < pool.Count) {
            return pool[index];
        }
        // 풀에 객체 부족할 경우 새로 생성
        var obj = Instantiate(BundlePrefab, PanelParent);
        
        //선택된 상단 패널에 표시된 보석은 선택되면 안되므로 버튼 기능 비활성화
        if (obj.TryGetComponent<Button>(out var btn)) btn.interactable = false;

        //스크립트 참고 저장하여 풀 리스트에 저장해놓기
        var script = obj.GetComponent<GemBundlePrefab>(); 
        pool.Add(script);

        return script;
    }
}
