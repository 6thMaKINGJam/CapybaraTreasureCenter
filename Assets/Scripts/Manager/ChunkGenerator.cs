using System.Collections.Generic;
using UnityEngine;

public class ChunkGenerator : MonoBehaviour
{
    // BoxCount 기반으로 여러 청크 생성 후 병합된 bundlePool 반환
    public ChunkData GenerateAllChunks(LevelConfig config)
    {
        int totalBoxCount = config.BoxCount;
        int chunkCount = Mathf.CeilToInt(totalBoxCount / 10f); // 10개 단위로 청크 나눔
        
        ChunkData chunkData = new ChunkData();
        chunkData.AllBoxes = new List<Box>();
        chunkData.MergedBundlePool = new List<GemBundle>();
        chunkData.TotalRemainingGems = new Dictionary<GemType, int>();
        
        // 각 보석 종류별 초기화
        for(int i = 0; i < config.GemTypeCount; i++)
        {
            chunkData.TotalRemainingGems[(GemType)i] = 0;
        }
        
        // 청크별 생성
        for(int chunkIdx = 0; chunkIdx < chunkCount; chunkIdx++)
        {
            int startBoxIndex = chunkIdx * 10;
            int endBoxIndex = Mathf.Min(startBoxIndex + 10, totalBoxCount);
            int boxesInThisChunk = endBoxIndex - startBoxIndex;
            
            Chunk chunk = GenerateSingleChunk(config, chunkIdx, boxesInThisChunk, startBoxIndex);
            
            // 상자들 추가
            chunkData.AllBoxes.AddRange(chunk.Boxes);
            
            // bundlePool 병합
            chunkData.MergedBundlePool.AddRange(chunk.BundlePool);
            
            // 보석 총량 합산
            foreach(var kvp in chunk.RemainingGems)
            {
                chunkData.TotalRemainingGems[kvp.Key] += kvp.Value;
            }
        }
        
        // bundlePool 셔플
        ShuffleList(chunkData.MergedBundlePool);
        
        Debug.Log($"[ChunkGenerator] 총 {chunkCount}개 청크 생성 완료. 총 상자: {totalBoxCount}개, 총 묶음: {chunkData.MergedBundlePool.Count}개");
        
        return chunkData;
    }
    
    // 단일 청크 생성 (10개 이하 상자)
    private Chunk GenerateSingleChunk(LevelConfig config, int chunkIndex, int boxCount, int startBoxIndex)
    {
        Chunk chunk = new Chunk();
        chunk.Boxes = new List<Box>();
        chunk.BundlePool = new List<GemBundle>();
        chunk.RemainingGems = new Dictionary<GemType, int>();
        
        // 각 보석 종류별 초기화
        for(int i = 0; i < config.GemTypeCount; i++)
        {
            chunk.RemainingGems[(GemType)i] = 0;
        }
        
        // 1단계: 상자 생성
        for(int i = 0; i < boxCount; i++)
        {
            Box box = new Box();
            box.TargetType = (GemType)Random.Range(0, config.GemTypeCount); // 일단 랜덤 (나중에 로직 개선 가능)
            
            // 요구량: 최소값은 보석 종류 수 이상, 최대값은 설정값
            int minRequired = config.GemTypeCount;
            int maxRequired = config.MaxRequiredPerBox;
            box.RequiredAmount = Random.Range(minRequired, maxRequired + 1);
            
            box.CurrentAmount = 0;
            box.isCompletedBox = false;
            
            chunk.Boxes.Add(box);
        }
        
        // 2단계: 각 상자에 보석 역산 배정
        foreach(Box box in chunk.Boxes)
        {
            box.SolutionBundles = new List<GemBundle>(); // 정답 묶음 저장
            
            Dictionary<GemType, int> boxGemCount = new Dictionary<GemType, int>();
            for(int i = 0; i < config.GemTypeCount; i++)
            {
                boxGemCount[(GemType)i] = 0;
            }
            
            // 2-1: 모든 종류 1개씩 필수
            int allocated = 0;
            for(int i = 0; i < config.GemTypeCount; i++)
            {
                boxGemCount[(GemType)i] = 1;
                allocated++;
            }
            
            // 2-2: 남은 개수 랜덤 분배
            int remaining = box.RequiredAmount - allocated;
            for(int i = 0; i < remaining; i++)
            {
                GemType randomType = (GemType)Random.Range(0, config.GemTypeCount);
                boxGemCount[randomType]++;
            }
            
            // 2-3: 보석별 총량 집계
            foreach(var kvp in boxGemCount)
            {
                chunk.RemainingGems[kvp.Key] += kvp.Value;
            }
            
            // 2-4: 이 상자의 보석들을 묶음으로 쪼개기
            foreach(var kvp in boxGemCount)
            {
                int gemCount = kvp.Value;
                GemType gemType = kvp.Key;
                
                while(gemCount > 0)
                {
                    int pieceSize = Random.Range(1, Mathf.Min(6, gemCount + 1)); // 1~5개로 쪼갬
                    
                    GemBundle bundle = new GemBundle();
                    bundle.BundleID = System.Guid.NewGuid().ToString();
                    bundle.GemType = gemType;
                    bundle.GemCount = pieceSize;
                    
                    chunk.BundlePool.Add(bundle);
                    box.SolutionBundles.Add(bundle); // 정답에 추가
                    
                    gemCount -= pieceSize;
                }
            }
        }
        
        return chunk;
    }
    
    // List 셔플
    private void ShuffleList<T>(List<T> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while(n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T temp = list[k];
            list[k] = list[n];
            list[n] = temp;
        }
    }
}

// ChunkData: 모든 청크 병합 결과
[System.Serializable]
public class ChunkData
{
    public List<Box> AllBoxes;
    public List<GemBundle> MergedBundlePool;
    public Dictionary<GemType, int> TotalRemainingGems;
}

// Chunk: 단일 청크 (10개 이하 상자)
[System.Serializable]
public class Chunk
{
    public List<Box> Boxes;
    public List<GemBundle> BundlePool;
    public Dictionary<GemType, int> RemainingGems;
}