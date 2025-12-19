using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//GemType/GemCount/GemBundle/Box/
//Chunk/CompletedBox/GameState 직렬화 데이터 구조

// 1. 게임 상태(GameState) 정의
public enum GameState { Ready, Playing, Paused, GameOver, Win }
// 2. 보석 종류(GemType) 정의
public enum GemType { Red, Blue, Green, Yellow, Purple }

// 3. 보석 묶음(GemBundle) 데이터
[Serializable]
public class GemBundle
{
    public string BundleID; // 묶음의 고유 ID
    public GemType GemType; // 보석 색깔
    public int GemCount; // 보석 개수 
}

// 4. 상자(Box/CompletedBox) 데이터
[Serializable]
public class Box
{
    public GemType TargetType; // 상자가 받아야 할 보석 종류
    public int RequiredAmount; // 목표 개수
    public int CurrentAmount; // 현재 담겨 있는 개수
    public bool isCompletedBox; // 상자가 다 찼는지 여부
}

// 5. 묶음 단위 생성(Chunk) 데이터
[Serializable]
public class Chunk
{
    public List<GemBundle> BundlesInChunk = new List<GemBundle>();
    public bool isUsed; // 이미 사용된 청크인지 여부
}

// 6. 게임 데이터(GameData) 직렬화
[Serializable]
public class GameData
{
    public GameState GameState; // 현재 게임 상태
    public List<Box> Boxes = new List<Box>(); // 상자들의 정보
    public List<Chunk> Chunks = new List<Chunk>(); // 보석 묶음들의 정보
}