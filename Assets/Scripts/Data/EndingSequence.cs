using System;
using UnityEngine;

// 엔딩 창 타입
public enum EndingDialogType
{
    Box,            // 네모 박스창
    Speech,         // 말풍선 대화창
    Choice,         // 선택창 (Yes/Yes)
    NicknameInput   // 닉네임 입력창
}

// 엔딩 시퀀스 데이터
[Serializable]
public class EndingSequence
{
    [Header("배경 설정")]
    public Sprite BackgroundImage;
    
    [Header("창 설정")]
    public EndingDialogType DialogType;
    
    [Header("텍스트 설정")]
    [TextArea(3, 10)]
    public string MessageText;
    
    [Header("선택창 전용 (Choice일 때만 사용)")]
    public string YesButton1Text = "Yes";
    public string YesButton2Text = "Yes";
    
    [Header("진동 설정")]
    public bool UseVibration = false;
    public VibrationPattern VibrationPattern = VibrationPattern.None;
    public float VibrationDelay = 0f; // 장 시작 후 몇 초 뒤에 진동
    
    [Header("커스텀 진동 패턴 (Custom 선택 시)")]
    [Tooltip("밀리초 단위 배열: {대기, 진동, 대기, 진동, ...}")]
    public long[] CustomVibrationPattern;
}