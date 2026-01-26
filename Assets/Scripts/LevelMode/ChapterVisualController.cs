using UnityEngine;
using UnityEngine.UI;

public class ChapterVisualController : MonoBehaviour
{
    [System.Serializable]
    public class ConfigVisuals
    {
        [Tooltip("LevelConfig의 파일명 (LevelConfig_1, LevelConfig_2, LevelConfig_3, LevelConfig_34)")]
        public string configName;
        
        [Header("GameObject Management")]
        [Tooltip("이 Config가 활성화될 때 켜질 GameObject들")]
        public GameObject[] gameObjects;
        
        [Header("Color Settings")]
        [Tooltip("Hex 색상 코드 (예: #FF5733, #00FF00)")]
        public string hexColor = "#FFFFFF";
    }

    [SerializeField] private ConfigVisuals[] configVisualsList;

    [Header("Shared Color Target Graphics")]
    [Tooltip("모든 Config에서 공유하는 색상 변경 대상 (Image, RawImage 등 - 항상 활성화 상태)")]
    [SerializeField] private Graphic[] sharedColorGraphics;

    /// <summary>
    /// 현재 로드된 LevelConfig에 맞는 GameObject들만 활성화하고, 공유 Graphic들의 색상 변경
    /// </summary>
    public void ApplyVisuals(LevelConfig currentConfig)
    {
        if (currentConfig == null)
        {
            Debug.LogWarning("[ChapterVisualController] currentConfig가 null입니다.");
            return;
        }
        
         string currentConfigName = currentConfig.name;

       if  (currentConfig.name  == "LevelConfig_34")
        {
            currentConfigName = "LevelConfig_1"; // level34테마는 level1과 동일 
        }

        
        Color targetColor = Color.white;
        bool colorFound = false;

        // 1. GameObject 활성화/비활성화 + 해당 Config의 색상 찾기
        foreach (var configVisual in configVisualsList)
        {
            bool shouldActivate = (configVisual.configName == currentConfigName);

            // GameObject 활성화/비활성화
            foreach (var obj in configVisual.gameObjects)
            {
                if (obj != null)
                {
                    obj.SetActive(shouldActivate);
                }
            }

            // 현재 Config의 색상 저장
            if (shouldActivate)
            {
                if (ColorUtility.TryParseHtmlString(configVisual.hexColor, out targetColor))
                {
                    colorFound = true;
                    Debug.Log($"[ChapterVisualController] {currentConfigName}의 색상: {configVisual.hexColor}");
                }
                else
                {
                    Debug.LogWarning($"[ChapterVisualController] {currentConfigName}: 잘못된 Hex 색상 '{configVisual.hexColor}'. 기본 색상(흰색) 사용.");
                }
            }
        }

        // 2. 공유 Graphic들에 색상 적용 (Image, RawImage 모두 포함)
        if (sharedColorGraphics != null && sharedColorGraphics.Length > 0)
        {
            foreach (var graphic in sharedColorGraphics)
            {
                if (graphic != null)
                {
                    graphic.color = targetColor;
                }
            }

            if (colorFound)
            {
                Debug.Log($"[ChapterVisualController] {sharedColorGraphics.Length}개의 공유 Graphic에 색상 적용 완료");
            }
        }

        Debug.Log($"[ChapterVisualController] {currentConfigName}에 맞는 Visuals 적용 완료");
    }

    /// <summary>
    /// 모든 GameObject 비활성화 (씬 초기화용)
    /// </summary>
    public void DeactivateAll()
    {
        foreach (var configVisual in configVisualsList)
        {
            foreach (var obj in configVisual.gameObjects)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                }
            }
        }

        // 공유 Graphic은 기본 색상으로 리셋 (선택사항)
        if (sharedColorGraphics != null)
        {
            foreach (var graphic in sharedColorGraphics)
            {
                if (graphic != null)
                {
                    graphic.color = Color.white;
                }
            }
        }
    }
}
