using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 레벨별 상자 개수/상자당 최대 요구량/보석 종류 수(5) 설정

// 파일 생성 버튼 추가 (Unity에서 우클릭 메뉴)
[CreateAssetMenu(fileName = "Level_00", menuName = "Configs/LevelConfig")]
public class LevelConfig : ScriptableObject
{
    [Header("레벨 기본 정보")]
    public int LevelID; // 1. 몇 레벨인지

    [Header("상자 설정")]
    public int BoxCount; // 2. 레벨별 상자 개수
    public int MaxRequiredPerBox; // 3. 상자당 최대 요구량

    [Header("보석 설정")]
    [Range(1,5)]
    // 4. 사용할 보석 종류 수 (최대 5가지)
    public int GemTypeCount = 5; 

    [Header("기타 설정")]
    public float TimeLimit; // 5. 제한 시간
}
