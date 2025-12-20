using UnityEngine;
using System;

[CreateAssetMenu(fileName = "GemSpriteDatabase", menuName = "Game/Gem Sprite Database")]
public class GemSpriteDatabase : ScriptableObject
{
    [Serializable]
    public class GemTypeSprites
    {
        public GemType Type;
        [Tooltip("1개, 2개, 3개, 4개 순서로 할당")]
        public Sprite[] Sprites = new Sprite[4];
    }
    
    [Header("보석 스프라이트 (5종)")]
    [Tooltip("Red, Blue, Green, Yellow, Purple 순서")]
    public GemTypeSprites[] GemSpritesByType = new GemTypeSprites[5];
    
    private void OnValidate()
    {
        // Inspector에서 타입 자동 설정
        if(GemSpritesByType.Length == 5)
        {
            for(int i = 0; i < 5; i++)
            {
                if(GemSpritesByType[i] == null)
                {
                    GemSpritesByType[i] = new GemTypeSprites();
                }
                GemSpritesByType[i].Type = (GemType)i;
            }
        }
    }
    
    public Sprite GetSprite(GemType gemType, int count)
    {
        int typeIndex = (int)gemType;
        int countIndex = count - 1; // 1~4 → 0~3
        
        if(typeIndex < 0 || typeIndex >= GemSpritesByType.Length)
        {
            Debug.LogError($"[GemSpriteDatabase] 잘못된 GemType: {gemType}");
            return null;
        }
        
        var sprites = GemSpritesByType[typeIndex].Sprites;
        
        if(countIndex < 0 || countIndex >= sprites.Length)
        {
            Debug.LogError($"[GemSpriteDatabase] 잘못된 Count: {count} (1~4만 가능)");
            return null;
        }
        
        if(sprites[countIndex] == null)
        {
            Debug.LogError($"[GemSpriteDatabase] 스프라이트 미할당: {gemType} x{count}");
            return null;
        }
        
        return sprites[countIndex];
    }
}