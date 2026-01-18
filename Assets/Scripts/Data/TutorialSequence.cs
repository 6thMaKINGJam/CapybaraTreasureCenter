using System;
using System.Diagnostics.Contracts;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;

public enum DialogType
{
    Box,
    Speech,
    Choice,

}

[Serializable]
public class TutorialSequence
{
    [Header("배경 설정")]
    public Sprite BackgroundImage;
    
    [Header("창 설정")]
    public DialogType DialogType;
    
    [Header("텍스트 설정")]
    [TextArea(3, 10)]
    public string MessageText;
    
    [Header("선택창 전용 (Choice일 때만 사용)")]
    public string YesButton1Text = "Yes";
    public string YesButton2Text = "Yes";
    [Header("선택창 전용 (SelectMode일 때만 사용)")]
    public string LevelModeText = "레벨 모드";
    public string ChallengeModeText = "도전 모드";

    [Header("진동 설정")] // 추가
    public bool UseVibration = false;
    public VibrationPattern VibrationPattern = VibrationPattern.None;
    public float VibrationDelay = 0f;
    
    [Header("커스텀 진동 패턴 (Custom 선택 시)")]
    [Tooltip("밀리초 단위 배열: {대기, 진동, 대기, 진동, ...}")]
    public long[] CustomVibrationPattern;


   
    

}