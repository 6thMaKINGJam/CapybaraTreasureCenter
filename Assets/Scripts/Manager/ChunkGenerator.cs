using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// GenerateChunk(LevelConfig, chunkIndex) 구현

public class ChunkGenerator : MonoBehaviour
{
    // 새로운 청크 데이터 생성
    public Chunk GenerateChunk(LevelConfig Config, int ChunkIndex)
    {
        // 1. 새로운 청크 바구니 생성
        Chunk NewChunk = new Chunk();
        NewChunk.isUsed = false; // 아직 화면에 배치되지 않음

        // 2. 이번 레벨에서 사용 가능한 보석 색상 리스트
        int AvailableTypeCount = Config.GemTypeCount;

        // 3. (미정) 한 청크당 3개의 GemBundle을 담음
        for(int i = 0; i < 3; i++)
        {
            GemBundle NewBundle = new GemBundle();
            NewBundle.BundleID = $"ID_{ChunkIndex}_{i}";

            // GemType = 랜덤 선택 (0부터 설정된 종류의 수)
            int RandomTypeIndex = Random.Range(0, AvailableTypeCount);
            NewBundle.GemType = (GemType) RandomTypeIndex;

            // GemCount = 상자당 최대 요구량(MaxGemCountPerBox) 내에서 랜덤 선택
            NewBundle.GemCount = Random.Range(1, 5);

            // 청크 바구니에 번들 추가
            NewChunk.BundlesInChunk.Add(NewBundle);
        }

        Debug.Log($"[ChunkGenerator] {ChunkIndex}번 청크 생성 완료 (보석 종류: {AvailableTypeCount})");

        return NewChunk;
    }
}
