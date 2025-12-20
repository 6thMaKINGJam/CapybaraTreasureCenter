using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using UnityEngine;

public class GemBundlePrefab : MonoBehaviour
{
    public Image GemIcon; //보석 이미지 아이콘
    //public Text CountText; //각 상자마다 보석 개수
    public Outline Outline;

    private GemBundle bundleData;
    public event Action<GemBundlePrefab> OnClickBundle; //클릭 이벤트를 외부에 알림

    public void SetData(GemBundle data) { //Gembundle 데이터 바인딩
        bundleData = data; //데이터 타입
        //CountText.text = data.GemCount.ToString(); //개수 텍스트

        string spriteName = data.GemType.ToString() + data.GemCount;
        Sprite loadedSprite = Resources.Load<Sprite>("GemSprites/" + spriteName);
        GemIcon.sprite = loadedSprite;
    }

    public GemBundle GetData() => bundleData;

    public void SetSelected(bool isSelected) { //선택 시 테투리 변경
        if (Outline != null) Outline.enabled = isSelected;
    }

    public void OnClick() {
        OnClickBundle?.Invoke(this);
    }
}
