#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class SaveDataEditorWindow : EditorWindow
{
    private ProgressData progressData;
    private GameData gameData;
    private Vector2 scrollPosition;
    
    [MenuItem("Window/Save Data Manager")]
    public static void ShowWindow()
    {
        GetWindow<SaveDataEditorWindow>("Save Data Manager");
    }
    
    private void OnEnable()
    {
        LoadAllData();
    }
    
    private void OnGUI()
    {
        EditorGUILayout.LabelField("세이브 데이터 관리", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        // ========== ProgressData ==========
        DrawProgressDataSection();
        
        EditorGUILayout.Space(20);
        
        // ========== GameData ==========
        DrawGameDataSection();
        
        EditorGUILayout.Space(20);
        
        // ========== 전체 관리 버튼 ==========
        DrawGlobalActionsSection();
        
        EditorGUILayout.EndScrollView();
    }
    
    private void DrawProgressDataSection()
    {
        EditorGUILayout.LabelField("Progress Data (진행 데이터)", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        
        if(progressData != null)
        {
            EditorGUI.BeginChangeCheck();
            
            // 튜토리얼 완료 여부
            bool tutorialCompleted = EditorGUILayout.Toggle("튜토리얼 완료", progressData.TutorialCompleted);
            
            // 마지막 클리어 레벨
            int lastClearedLevel = EditorGUILayout.IntField("마지막 클리어 레벨", progressData.LastClearedLevel);
            
            // 최고 점수
            int bestScore = EditorGUILayout.IntField("최고 점수", progressData.BestScore);
            
            // 총 보석 개수
            int totalGemCount = EditorGUILayout.IntField("총 보석 개수", progressData.TotalGemCount);
            
            if(EditorGUI.EndChangeCheck())
            {
                progressData.TutorialCompleted = tutorialCompleted;
                progressData.LastClearedLevel = lastClearedLevel;
                progressData.BestScore = bestScore;
                progressData.TotalGemCount = totalGemCount;
            }
            
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            
            GUI.backgroundColor = Color.green;
            if(GUILayout.Button("ProgressData 저장", GUILayout.Height(30)))
            {
                SaveProgressData();
            }
            
            GUI.backgroundColor = Color.yellow;
            if(GUILayout.Button("ProgressData 새로고침", GUILayout.Height(30)))
            {
                LoadProgressData();
            }
            
            GUI.backgroundColor = Color.red;
            if(GUILayout.Button("ProgressData 삭제", GUILayout.Height(30)))
            {
                if(EditorUtility.DisplayDialog(
                    "ProgressData 삭제", 
                    "정말로 ProgressData를 삭제하시겠습니까?\n(튜토리얼 완료 기록 등이 모두 사라집니다)", 
                    "삭제", 
                    "취소"))
                {
                    DeleteProgressData();
                }
            }
            
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.HelpBox("ProgressData가 없습니다.", MessageType.Info);
            
            if(GUILayout.Button("새 ProgressData 생성"))
            {
                progressData = new ProgressData();
                SaveProgressData();
            }
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawGameDataSection()
    {
        EditorGUILayout.LabelField("Game Data (게임 데이터)", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        
        if(gameData != null)
        {
            EditorGUILayout.LabelField($"게임 상태: {gameData.GameState}");
            EditorGUILayout.LabelField($"상자 개수: {gameData.Boxes.Count}");
            EditorGUILayout.LabelField($"청크 개수: {gameData.Chunks.Count}");
            
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            
            GUI.backgroundColor = Color.yellow;
            if(GUILayout.Button("GameData 새로고침", GUILayout.Height(30)))
            {
                LoadGameData();
            }
            
            GUI.backgroundColor = Color.red;
            if(GUILayout.Button("GameData 삭제", GUILayout.Height(30)))
            {
                if(EditorUtility.DisplayDialog(
                    "GameData 삭제", 
                    "정말로 GameData를 삭제하시겠습니까?\n(진행 중인 게임이 삭제됩니다)", 
                    "삭제", 
                    "취소"))
                {
                    DeleteGameData();
                }
            }
            
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.HelpBox("GameData가 없습니다. (게임을 시작하면 생성됩니다)", MessageType.Info);
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawGlobalActionsSection()
    {
        EditorGUILayout.LabelField("전체 관리", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        
        GUI.backgroundColor = Color.cyan;
        if(GUILayout.Button("모든 데이터 새로고침", GUILayout.Height(35)))
        {
            LoadAllData();
        }
        
        GUI.backgroundColor = Color.red;
        if(GUILayout.Button("⚠️ 모든 저장 데이터 삭제 ⚠️", GUILayout.Height(35)))
        {
            if(EditorUtility.DisplayDialog(
                "전체 데이터 삭제", 
                "정말로 모든 저장 데이터를 삭제하시겠습니까?\n\n이 작업은 되돌릴 수 없습니다!", 
                "모두 삭제", 
                "취소"))
            {
                DeleteAllData();
            }
        }
        
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.Space();
        
        // 저장 경로 표시
        EditorGUILayout.LabelField("저장 경로:", EditorStyles.boldLabel);
        EditorGUILayout.SelectableLabel(Application.persistentDataPath, EditorStyles.textField, GUILayout.Height(18));
        
        if(GUILayout.Button("저장 폴더 열기"))
        {
            EditorUtility.RevealInFinder(Application.persistentDataPath);
        }
        
        EditorGUILayout.EndVertical();
    }
    
    // ========== 데이터 로드 함수들 ==========
    
    private void LoadAllData()
    {
        LoadProgressData();
        LoadGameData();
        Debug.Log("[SaveDataEditor] 모든 데이터를 새로고침했습니다.");
    }
    
    private void LoadProgressData()
    {
        if(SaveManager.IsSaveExist("ProgressData"))
        {
            progressData = SaveManager.LoadData<ProgressData>("ProgressData");
            Debug.Log("[SaveDataEditor] ProgressData 로드 완료");
        }
        else
        {
            progressData = null;
            Debug.Log("[SaveDataEditor] ProgressData가 존재하지 않습니다.");
        }
        Repaint();
    }
    
    private void LoadGameData()
    {
        if(SaveManager.IsSaveExist("GameData"))
        {
            gameData = SaveManager.LoadData<GameData>("GameData");
            Debug.Log("[SaveDataEditor] GameData 로드 완료");
        }
        else
        {
            gameData = null;
            Debug.Log("[SaveDataEditor] GameData가 존재하지 않습니다.");
        }
        Repaint();
    }
    
    // ========== 데이터 저장 함수들 ==========
    
    private void SaveProgressData()
    {
        if(progressData != null)
        {
            SaveManager.Save(progressData, "ProgressData");
            Debug.Log("[SaveDataEditor] ProgressData 저장 완료");
            EditorUtility.DisplayDialog("저장 완료", "ProgressData가 저장되었습니다.", "확인");
        }
    }
    
    // ========== 데이터 삭제 함수들 ==========
    
    private void DeleteProgressData()
    {
        SaveManager.DeleteSave("ProgressData");
        progressData = null;
        Debug.Log("[SaveDataEditor] ProgressData 삭제 완료");
        EditorUtility.DisplayDialog("삭제 완료", "ProgressData가 삭제되었습니다.", "확인");
        Repaint();
    }
    
    private void DeleteGameData()
    {
        SaveManager.DeleteSave("GameData");
        gameData = null;
        Debug.Log("[SaveDataEditor] GameData 삭제 완료");
        EditorUtility.DisplayDialog("삭제 완료", "GameData가 삭제되었습니다.", "확인");
        Repaint();
    }
    
    private void DeleteAllData()
    {
        SaveManager.DeleteSave("ProgressData");
        SaveManager.DeleteSave("GameData");
        progressData = null;
        gameData = null;
        Debug.Log("[SaveDataEditor] 모든 데이터 삭제 완료");
        EditorUtility.DisplayDialog("삭제 완료", "모든 저장 데이터가 삭제되었습니다.", "확인");
        Repaint();
    }
}
#endif