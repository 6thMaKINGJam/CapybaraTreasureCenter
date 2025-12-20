using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class BundleGridManager : MonoBehaviour
{
    public GameObject BundlePrefab;
    public Transform GridParent; //보석들이 부착될 grid layout

    private List<GemBundlePrefab> pool = new List<GemBundlePrefab>();
    private List<GemBundlePrefab> activeBundles = new List<GemBundlePrefab>();

    public void RefreshGrid(List<GemBundle> newPool, Action<GemBundlePrefab> callback){

        //현재 올라와있는 보석 데이터들 전부 비활성화
        foreach (var b in activeBundles) b.gameObject.SetActive(false);
        activeBundles.Clear();

        foreach (var data in newPool) {
            var b = GetFromPool();
            b.transform.SetParent(GridParent); //부모 레이아웃 설정
            b.SetData(data); //데이터 설정
            b.OnClickBundle += callback; //콜백 연결
            b.gameObject.SetActive(true); //활성화
            activeBundles.Add(b); //리스트 추가
        }
    }

    public void ReplaceBundle(GemBundlePrefab target, GemBundle newData, Action<GemBundlePrefab> callback) {
        //타겟 객체의 이벤트 구독 해제
        target.OnClickBundle -= callback;
        target.gameObject.SetActive(false);
        activeBundles.Remove(target);

        //새로운 데이터가 존재하는 경우 보충 로직 실행
        if (newData != null) {
            var b = GetFromPool(); //재사용 가능한 객체 획득
            b.SetData(newData); //신규 데이터 주입
            b.OnClickBundle += callback; //이벤트 다시 등록
            b.gameObject.SetActive(true); //객체 활성화
            activeBundles.Add(b);
        }

    }
    private GemBundlePrefab GetFromPool() {
        //pool 순회하며 사용 중이지 않은 객체를 탐색
        foreach (var b in pool) {
            if (!b.gameObject.activeSelf) return b;
        }

        //사용 가능한 공간이 없을 때 새로 생성
        var obj= Instantiate(BundlePrefab);
        var script = obj.GetComponent<GemBundlePrefab>();
        pool.Add(script); //재사용 위해 pool 리스트에 등록
        return script;
    }


}
