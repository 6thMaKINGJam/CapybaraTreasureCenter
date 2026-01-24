using UnityEngine;
using System.IO;

// 튜토리얼 타입 enum 추가
public enum TutorialType { Story, Apple, LevelTuto, ChallengeTuto }

// 튜토리얼 진행도 클래스 추가
[System.Serializable]

public class TutorialProgress
{
    public bool storyCompleted;
    public bool appleCompleted;
    public bool challengeTutoCompleted;
    
    // 아이템 튜토리얼
    public bool undo1TutorialShown;
    public bool undoTutorialShown;
    public bool refreshTutorialShown;
}

public static class SaveManager
{
    // 1. 저장 경로 설정
    private static string GetSavePath(string FileName)
    {
        return Path.Combine(Application.persistentDataPath, FileName + ".json");
    }

    // 2. 저장
    public static void Save<T>(T DataToSave, string FileName)
    {
        try
        {
            string Path = GetSavePath(FileName);
            string JsonText = JsonUtility.ToJson(DataToSave, true);
            File.WriteAllText(Path, JsonText);
            Debug.Log($"[SaveManager] 저장 성공 : {Path}");
        }
        catch (System.Exception E)
        {
            Debug.LogError($"[SaveManager] 저장 중 에러 발생 ({FileName}): {E.Message}");
        }
    }

    // 3. 로드
    public static T LoadData<T>(string FileName) where T : new()
    {
        string Path = GetSavePath(FileName);

        if(HasSaveData(FileName))
        {
            try
            {
                string JsonText = File.ReadAllText(Path);
                return JsonUtility.FromJson<T>(JsonText);
            }
            catch (System.Exception E)
            {
                Debug.LogError($"[SaveManager] 로드 중 에러 발생 ({FileName}): {E.Message}");
                return new T();
            }
        }

        Debug.LogWarning($"[SaveManager] 파일 없음. 새 데이터를 생성합니다.");
        return new T();
    }

    // 4. 파일 존재 확인
    public static bool HasSaveData(string FileName)
    {
        return File.Exists(GetSavePath(FileName));
    }

    // 5. 데이터 삭제
    public static void DeleteSave(string FileName)
    {
        if(HasSaveData(FileName))
        {
            File.Delete(GetSavePath(FileName));
            Debug.Log($"[SaveManager] 데이터 삭제 완료: {FileName}");
        }
    }
    
    // ========== 튜토리얼 관련 메서드 추가 ==========
    
    /// <summary>
    /// 튜토리얼 완료 여부 확인
    /// </summary>
    public static bool IsTutorialCompleted(TutorialType type)
    {
        TutorialProgress progress = LoadData<TutorialProgress>("TutorialProgress");
        
        return type switch
        {
            TutorialType.Story => progress.storyCompleted,
            TutorialType.Apple => progress.appleCompleted,
            TutorialType.ChallengeTuto => progress.challengeTutoCompleted,
            _ => false
        };
    }
    
    /// <summary>
    /// 튜토리얼 완료 상태 설정
    /// </summary>
    public static void SetTutorialCompleted(TutorialType type, bool value)
    {
        TutorialProgress progress = LoadData<TutorialProgress>("TutorialProgress");
        
        switch (type)
        {
            case TutorialType.Story:
                progress.storyCompleted = value;
                break;
            case TutorialType.Apple:
                progress.appleCompleted = value;
                break;
            case TutorialType.ChallengeTuto:
                progress.challengeTutoCompleted = value;
                break;
        }
        
        Save(progress, "TutorialProgress");
    }
}