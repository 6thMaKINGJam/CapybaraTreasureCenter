using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class DialogueEditorWindow : EditorWindow
{
    private DialogueDatabase database;
    private Vector2 scrollPosition;
    private Dictionary<DialogueType, bool> foldouts = new Dictionary<DialogueType, bool>();
    private Dictionary<DialogueType, string> textAreaContents = new Dictionary<DialogueType, string>();
    
    private const string DATABASE_PATH = "Assets/Resources/Dialogues/DialogueDatabase.asset";
    private const string BACKUP_FOLDER = "Assets/Resources/Dialogues/Backups";
    
    [MenuItem("Tools/Dialogue Editor")]
    public static void ShowWindow()
    {
        var window = GetWindow<DialogueEditorWindow>("Dialogue Editor");
        window.minSize = new Vector2(600, 400);
    }
    
    private void OnEnable()
    {
        LoadDatabase();
        InitializeFoldouts();
    }
    
    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        
        if (GUILayout.Button("저장", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            SaveDatabase();
        }
        
        if (GUILayout.Button("불러오기", EditorStyles.toolbarButton, GUILayout.Width(80)))
        {
            LoadDatabase();
        }
        
        if (GUILayout.Button("백업 복원 ▼", EditorStyles.toolbarButton, GUILayout.Width(100)))
        {
            ShowBackupMenu();
        }
        
        GUILayout.FlexibleSpace();
        
        // if (GUILayout.Button("현재 씬 데이터 가져오기", EditorStyles.toolbarButton, GUILayout.Width(150)))
        // {
        //     ImportFromScene();
        // }
        
        EditorGUILayout.EndHorizontal();
        
        if (database == null)
        {
            EditorGUILayout.HelpBox("데이터베이스가 없습니다. '불러오기' 또는 '현재 씬 데이터 가져오기'를 눌러주세요.", MessageType.Warning);
            return;
        }
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        // DialogueType별 섹션
        foreach (DialogueType type in System.Enum.GetValues(typeof(DialogueType)))
        {
            DrawDialogueSection(type);
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    private void DrawDialogueSection(DialogueType type)
    {
        var entry = database.Entries.Find(e => e.Type == type);
        if (entry == null)
        {
            entry = new DialogueDatabase.DialogueEntry { Type = type };
            database.Entries.Add(entry);
        }
        
        // Foldout 초기화
        if (!foldouts.ContainsKey(type))
            foldouts[type] = false;
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        // 헤더
        foldouts[type] = EditorGUILayout.Foldout(
            foldouts[type], 
            $"{type} ({entry.Dialogues.Count}개)", 
            true, 
            EditorStyles.foldoutHeader
        );
        
        if (foldouts[type])
        {
            EditorGUI.indentLevel++;
            
            // 텍스트 에리어 초기화
            if (!textAreaContents.ContainsKey(type))
            {
                textAreaContents[type] = string.Join("\n\n", entry.Dialogues);
            }
            
            EditorGUILayout.LabelField("대사 입력 (빈 줄로 구분)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("문단을 빈 줄로 구분하면 각각 다른 대사로 분리됩니다.", MessageType.Info);
            
            textAreaContents[type] = EditorGUILayout.TextArea(
                textAreaContents[type], 
                GUILayout.MinHeight(100)
            );
            
            EditorGUILayout.Space(5);
            
            // 설정
            EditorGUILayout.BeginHorizontal();
            entry.IsPersistent = EditorGUILayout.ToggleLeft("IsPersistent (계속 표시)", entry.IsPersistent, GUILayout.Width(200));
            entry.IsLoop = EditorGUILayout.ToggleLeft("IsLoop (반복 표시)", entry.IsLoop, GUILayout.Width(200));
            EditorGUILayout.EndHorizontal();
            
            if (entry.IsLoop)
            {
                entry.LoopInterval = EditorGUILayout.FloatField("Loop Interval (초)", entry.LoopInterval);
            }
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }
    
    private void SaveDatabase()
    {
        if (database == null)
        {
            Debug.LogError("저장할 데이터베이스가 없습니다.");
            return;
        }
        
        // 텍스트 에리어 → DialogueEntry 변환
        foreach (var type in textAreaContents.Keys.ToList())
        {
            var entry = database.Entries.Find(e => e.Type == type);
            if (entry == null) continue;
            
            entry.Dialogues = ParseTextArea(textAreaContents[type]);
        }
        
        // 백업 생성
        CreateBackup();
        
        // 메인 파일 저장
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"[DialogueEditor] 저장 완료: {DATABASE_PATH}");
        EditorUtility.DisplayDialog("저장 완료", "대사 데이터가 저장되었습니다.", "확인");
    }
    
    private void LoadDatabase()
    {
        // 기존 데이터베이스 로드
        database = AssetDatabase.LoadAssetAtPath<DialogueDatabase>(DATABASE_PATH);
        
        if (database == null)
        {
            Debug.LogWarning($"[DialogueEditor] 데이터베이스를 찾을 수 없습니다: {DATABASE_PATH}");
            
            // 폴더 생성
            string folderPath = Path.GetDirectoryName(DATABASE_PATH);
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                AssetDatabase.Refresh();
            }
            
            // 새로 생성
            database = ScriptableObject.CreateInstance<DialogueDatabase>();
            AssetDatabase.CreateAsset(database, DATABASE_PATH);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"[DialogueEditor] 새 데이터베이스 생성: {DATABASE_PATH}");
        }
        
        // 텍스트 에리어 초기화
        textAreaContents.Clear();
        foreach (var entry in database.Entries)
        {
            textAreaContents[entry.Type] = string.Join("\n\n", entry.Dialogues);
        }
        
        Repaint();
    }
    
    private void CreateBackup()
    {
        if (!Directory.Exists(BACKUP_FOLDER))
        {
            Directory.CreateDirectory(BACKUP_FOLDER);
            AssetDatabase.Refresh();
        }
        
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string backupPath = $"{BACKUP_FOLDER}/DialogueDatabase_{timestamp}.asset";
        
        AssetDatabase.CopyAsset(DATABASE_PATH, backupPath);
        Debug.Log($"[DialogueEditor] 백업 생성: {backupPath}");
    }
    
    private void ShowBackupMenu()
    {
        if (!Directory.Exists(BACKUP_FOLDER))
        {
            EditorUtility.DisplayDialog("백업 없음", "백업 파일이 없습니다.", "확인");
            return;
        }
        
        string[] backupFiles = Directory.GetFiles(BACKUP_FOLDER, "*.asset");
        
        if (backupFiles.Length == 0)
        {
            EditorUtility.DisplayDialog("백업 없음", "백업 파일이 없습니다.", "확인");
            return;
        }
        
        GenericMenu menu = new GenericMenu();
        
        foreach (var backupFile in backupFiles.OrderByDescending(f => f))
        {
            string fileName = Path.GetFileName(backupFile);
            menu.AddItem(new GUIContent(fileName), false, () => RestoreBackup(backupFile));
        }
        
        menu.ShowAsContext();
    }
    
    private void RestoreBackup(string backupPath)
    {
        bool confirm = EditorUtility.DisplayDialog(
            "백업 복원", 
            $"현재 데이터를 다음 백업으로 복원하시겠습니까?\n{Path.GetFileName(backupPath)}\n\n현재 데이터는 자동으로 백업됩니다.", 
            "복원", 
            "취소"
        );
        
        if (!confirm) return;
        
        // 현재 데이터 백업
        CreateBackup();
        
        // 백업 복원
        AssetDatabase.DeleteAsset(DATABASE_PATH);
        AssetDatabase.CopyAsset(backupPath, DATABASE_PATH);
        AssetDatabase.Refresh();
        
        // 다시 로드
        LoadDatabase();
        
        Debug.Log($"[DialogueEditor] 백업 복원 완료: {backupPath}");
        EditorUtility.DisplayDialog("복원 완료", "백업이 복원되었습니다.", "확인");
    }
    
    // private void ImportFromScene()
    // {
    //     CapyDialogue capy = FindObjectOfType<CapyDialogue>();
        
    //     if (capy == null)
    //     {
    //         EditorUtility.DisplayDialog("오류", "씬에서 CapyDialogue를 찾을 수 없습니다.", "확인");
    //         return;
    //     }
        
    //     if (capy.DialogueDatas == null || capy.DialogueDatas.Count == 0)
    //     {
    //         EditorUtility.DisplayDialog("오류", "CapyDialogue에 데이터가 없습니다.", "확인");
    //         return;
    //     }
        
    //     bool confirm = EditorUtility.DisplayDialog(
    //         "씬 데이터 가져오기", 
    //         $"씬의 CapyDialogue 데이터를 가져오시겠습니까?\n({capy.DialogueDatas.Count}개 항목)\n\n현재 에디터 내용은 덮어씌워집니다.", 
    //         "가져오기", 
    //         "취소"
    //     );
        
    //     if (!confirm) return;
        
    //     // 데이터베이스 초기화
    //     if (database == null)
    //     {
    //         database = ScriptableObject.CreateInstance<DialogueDatabase>();
    //     }
        
    //     database.Entries.Clear();
        
    //     // 씬 데이터 변환
    //     foreach (var oldData in capy.DialogueDatas)
    //     {
    //         var newEntry = new DialogueDatabase.DialogueEntry
    //         {
    //             Type = oldData.Type,
    //             Dialogues = new List<string>(oldData.Dialogues),
    //             IsPersistent = oldData.IsPersistent,
    //             IsLoop = oldData.IsLoop,
    //             LoopInterval = oldData.LoopInterval
    //         };
            
    //         database.Entries.Add(newEntry);
    //     }
        
    //     // 텍스트 에리어 초기화
    //     textAreaContents.Clear();
    //     foreach (var entry in database.Entries)
    //     {
    //         textAreaContents[entry.Type] = string.Join("\n\n", entry.Dialogues);
    //     }
        
    //     Debug.Log($"[DialogueEditor] 씬 데이터 가져오기 완료: {database.Entries.Count}개 항목");
    //     EditorUtility.DisplayDialog("가져오기 완료", $"{database.Entries.Count}개 항목을 가져왔습니다.\n'저장' 버튼을 눌러 파일로 저장하세요.", "확인");
        
    //     Repaint();
    // }
    
    private List<string> ParseTextArea(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();
        
        // 빈 줄로 분리
        var paragraphs = text.Split(new[] { "\n\n", "\r\n\r\n" }, System.StringSplitOptions.RemoveEmptyEntries);
        
        return paragraphs
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();
    }
    
    private void InitializeFoldouts()
    {
        foreach (DialogueType type in System.Enum.GetValues(typeof(DialogueType)))
        {
            if (!foldouts.ContainsKey(type))
                foldouts[type] = false;
        }
    }
}