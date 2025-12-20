using UnityEngine;
using System.IO; // 파일 입출력용 라이브러리
using System; 


// GameState, ProgressData 저장/로드/삭제/존재확인 + 에러 처리

public static class SaveManager
{
    // 1. 저장 경로(GetSavePath) 설정
    private static string GetSavePath(string FileName)
    {
        return Path.Combine(Application.persistentDataPath, FileName + ".json");
    }

    // 2. 저장 = 데이터를 JSON 텍스트로 바꿔서 파일로 저장
    public static void Save<T>(T DataToSave, string FileName)
    {
        try
        {
            string Path = GetSavePath(FileName);
            // 클래스 객체를 텍스트로 변환 (true는 읽기 편하게 줄바꿈 포함)
            string JsonText = JsonUtility.ToJson(DataToSave, true);

            File.WriteAllText(Path, JsonText); // 실제 파일 쓰기
            Debug.Log($"[SaveManager] 저장 성공 : {Path}");
        }
        catch (System.Exception E)
        {
            // 용량 부족이나 권한 문제 발생 시 에러 메시지 출력
            Debug.LogError($"[SaveManager] 저장 중 에러 발생 ({FileName}): {E.Message}");
        }
    }

    // 3. 로드 = 파일을 읽어서 다시 원래의 클래스 데이터로 복원
    public static T LoadData<T>(string FileName) where T : new()
    {
        string Path = GetSavePath(FileName);

        if(IsSaveExist(FileName))
        {
            try
            {
                string JsonText = File.ReadAllText(Path); // 파일 읽기
                // 텍스트를 다시 데이터 클래스로 변환
                return JsonUtility.FromJson<T>(JsonText);
            }
            catch (System.Exception E)
            {
                // 로드 중 파일이 깨졌거나 에러가 나면 콘솔에 기록
                Debug.LogError($"[SaveManager] 로드 중 에러 발생 ({FileName}): {E.Message}");
                // 실패 -> 빈 데이터 return
                return new T();
            }
        }

        // 파일이 없으므로 새 데이터 생성
        Debug.LogWarning($"[SaveManager] 파일 없음. 새 데이터를 생성합니다.");
        return new T();
    }

    // 4. 저장된 파일이 존재하는지(IsSaveExist) 여부 확인
    public static bool IsSaveExist(string FileName)
    {
        return File.Exists(GetSavePath(FileName));
    }

    // 5. 데이터 삭제 = 특정 저장 파일을 영구 삭제 (초기화용)
    public static void DeleteSave(string FileName)
    {
        if(IsSaveExist(FileName))
        {
            File.Delete(GetSavePath(FileName));
            Debug.Log($"[SaveManager] 데이터 삭제 완료: {FileName}");
        }
    }
}