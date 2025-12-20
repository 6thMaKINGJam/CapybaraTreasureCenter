using System;
using UnityEngine;

// 튜토리얼 창 타입 정의
public enum DialogType
{
    Box,      // 네모 박스창
    Speech,   // 대화창 (말풍선)
    Choice    // 선택창 (Yes/Yes)
}

// 튜토리얼 시퀀스 데이터
[Serializable]
public class TutorialSequence
{
    [Header("배경 설정")]
    public Sprite BackgroundImage; // 배경 이미지
    
    [Header("창 설정")]
    public DialogType DialogType; // 창 타입
    
    [Header("텍스트 설정")]
    [TextArea(3, 10)]
    public string MessageText; // 표시할 텍스트
    
    [Header("선택창 전용 (Choice일 때만 사용)")]
    public string YesButton1Text = "Yes"; // 첫 번째 버튼 텍스트
    public string YesButton2Text = "Yes"; // 두 번째 버튼 텍스트
}