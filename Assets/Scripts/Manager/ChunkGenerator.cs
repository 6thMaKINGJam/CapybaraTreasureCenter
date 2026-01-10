using System.Collections.Generic;
using UnityEngine;

public class ChunkGenerator : MonoBehaviour
{
    /// <summary>
    /// 계산된 난이도 값으로 청크 생성
    /// </summary>
    /// <param name="boxCount">생성할 상자 개수</param>
    /// <param name="gemTypeCount">사용할 보석 종류 수 (1~5)</param>
    /// <param name="maxRequiredPerBox">상자당 최대 요구량</param>
    public ChunkData GenerateAllChunks(int boxCount, int gemTypeCount, int maxRequiredPerBox)
    {
        int totalBoxCount = boxCount;
        int chunkCount = Mathf.CeilToInt(totalBoxCount / 10f);
        
        ChunkData chunkData = new ChunkData();
        chunkData.AllBoxes = new List<Box>();
        chunkData.MergedBundlePool = new List<GemBundle>();
        chunkData.TotalRemainingGems = new Dictionary<GemType, int>();
        
        for(int i = 0; i < gemTypeCount; i++)
        {
            chunkData.TotalRemainingGems[(GemType)i] = 0;
        }
        
        for(int chunkIdx = 0; chunkIdx < chunkCount; chunkIdx++)
        {
            int startBoxIndex = chunkIdx * 10;
            int endBoxIndex = Mathf.Min(startBoxIndex + 10, totalBoxCount);
            int boxesInThisChunk = endBoxIndex - startBoxIndex;
            
            Chunk chunk = GenerateSingleChunk(gemTypeCount, maxRequiredPerBox, chunkIdx, boxesInThisChunk, startBoxIndex);
            
            chunkData.AllBoxes.AddRange(chunk.Boxes);
            chunkData.MergedBundlePool.AddRange(chunk.BundlePool);
            
            foreach(var kvp in chunk.RemainingGems)
            {
                chunkData.TotalRemainingGems[kvp.Key] += kvp.Value;
            }
        }
        
        ShuffleList(chunkData.MergedBundlePool);
        
        Debug.Log($"[ChunkGenerator] 총 {chunkCount}개 청크 생성 완료. 총 상자: {totalBoxCount}개, 총 묶음: {chunkData.MergedBundlePool.Count}개");
        
        return chunkData;
    }
    
    private Chunk GenerateSingleChunk(int gemTypeCount, int maxRequiredPerBox, int chunkIndex, int boxCount, int startBoxIndex)
    {
        Chunk chunk = new Chunk();
        chunk.Boxes = new List<Box>();
        chunk.BundlePool = new List<GemBundle>();
        chunk.RemainingGems = new Dictionary<GemType, int>();
        
        for(int i = 0; i < gemTypeCount; i++)
        {
            chunk.RemainingGems[(GemType)i] = 0;
        }
        
        // 1단계: 상자 생성
        for(int i = 0; i < boxCount; i++)
        {
            Box box = new Box();
            box.TargetType = (GemType)Random.Range(0, gemTypeCount);
            
            int minRequired = gemTypeCount;
            box.RequiredAmount = Random.Range(minRequired, maxRequiredPerBox + 1);
            
            box.CurrentAmount = 0;
            box.isCompletedBox = false;
            
            chunk.Boxes.Add(box);
        }
        
        // 2단계: 각 상자에 보석 역산 배정
        foreach(Box box in chunk.Boxes)
        {
            box.SolutionBundles = new List<GemBundle>();
            
            Dictionary<GemType, int> boxGemCount = new Dictionary<GemType, int>();
            for(int i = 0; i < gemTypeCount; i++)
            {
                boxGemCount[(GemType)i] = 0;
            }
            
            // 2-1: 모든 종류 1개씩 필수
            int allocated = 0;
            for(int i = 0; i < gemTypeCount; i++)
            {
                boxGemCount[(GemType)i] = 1;
                allocated++;
            }
            
            // 2-2: 남은 개수 랜덤 분배
            int remaining = box.RequiredAmount - allocated;
            for(int i = 0; i < remaining; i++)
            {
                GemType randomType = (GemType)Random.Range(0, gemTypeCount);
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
                    int pieceSize = Random.Range(1, Mathf.Min(6, gemCount + 1));
                    
                    GemBundle bundle = new GemBundle();
                    bundle.BundleID = System.Guid.NewGuid().ToString();
                    bundle.GemType = gemType;
                    bundle.GemCount = pieceSize;
                    
                    chunk.BundlePool.Add(bundle);
                    box.SolutionBundles.Add(bundle);
                    
                    gemCount -= pieceSize;
                }
            }
        }
        
        return chunk;
    }
    
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

[System.Serializable]
public class ChunkData
{
    public List<Box> AllBoxes;
    public List<GemBundle> MergedBundlePool;
    public Dictionary<GemType, int> TotalRemainingGems;
}

[System.Serializable]
public class Chunk
{
    public List<Box> Boxes;
    public List<GemBundle> BundlePool;
    public Dictionary<GemType, int> RemainingGems;
}