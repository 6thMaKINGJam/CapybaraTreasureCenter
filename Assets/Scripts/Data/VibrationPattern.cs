using System;

// 진동 패턴 종류
public enum VibrationPattern
{
    None,           // 진동 없음
    Short,          // 짧게 1번
    Long,           // 길게 1번
    Double,         // 짧게 2번
    Triple,         // 짧게 3번
    Success,        // 성공 느낌 (짧-짧-길)
    Warning,        // 경고 느낌 (길-짧-길)
    Custom          // 커스텀 패턴
}