#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class EndingEditorWindow : EditorWindow
{
    private EndingManager endingManager;
    private Vector2 scrollPosition;
    private int selectedSequenceIndex = -1;
    
    [MenuItem("Window/Ending Editor")]
    public static void ShowWindow()
    {
        GetWindow<EndingEditorWindow>("Ending Editor");
    }
    
    private void OnEnable()
    {
        FindEndingManager();
    }
    
    private void OnGUI()
    {
        EditorGUILayout.LabelField("엔딩 에디터", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        endingManager = (EndingManager)EditorGUILayout.ObjectField(
            "Ending Manager",
            endingManager,
            typeof(EndingManager),
            true
        );
        
        if(endingManager == null)
        {
            EditorGUILayout.HelpBox("Scene에 EndingManager를 배치하거나 위 필드에 할당하세요.", MessageType.Warning);
            if(GUILayout.Button("Scene에서 찾기"))
            {
                FindEndingManager();
            }
            return;
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        
        if(GUILayout.Button("시퀀스 추가", GUILayout.Height(30)))
        {
            AddSequence();
        }
        
        if(GUILayout.Button("변경사항 저장", GUILayout.Height(30)))
        {
            SaveChanges();
        }
        
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        for(int i = 0; i < endingManager.Sequences.Count; i++)
        {
            DrawSequence(i);
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    private void DrawSequence(int index)
    {
        EndingSequence sequence = endingManager.Sequences[index];
        
        EditorGUILayout.BeginVertical("box");
        
        // 헤더
        EditorGUILayout.BeginHorizontal();
        
        bool isFoldout = selectedSequenceIndex == index;
        string foldoutText = $"시퀀스 #{index + 1} - {sequence.DialogType}";
        
        if(GUILayout.Button(isFoldout ? "▼" : "▶", GUILayout.Width(30)))
        {
            selectedSequenceIndex = isFoldout ? -1 : index;
        }
        
        EditorGUILayout.LabelField(foldoutText, EditorStyles.boldLabel);
        
        // 위로/아래로 이동
        GUI.enabled = index > 0;
        if(GUILayout.Button("↑", GUILayout.Width(30)))
        {
            MoveSequence(index, -1);
        }
        GUI.enabled = index < endingManager.Sequences.Count - 1;
        if(GUILayout.Button("↓", GUILayout.Width(30)))
        {
            MoveSequence(index, 1);
        }
        GUI.enabled = true;
        
        // 삭제
        GUI.backgroundColor = Color.red;
        if(GUILayout.Button("X", GUILayout.Width(30)))
        {
            if(EditorUtility.DisplayDialog("삭제 확인", $"시퀀스 #{index + 1}을 삭제하시겠습니까?", "삭제", "취소"))
            {
                RemoveSequence(index);
                return;
            }
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.EndHorizontal();
        
        // 상세 정보
        if(isFoldout)
        {
            EditorGUILayout.Space();
            
            // 배경 이미지
            sequence.BackgroundImage = (Sprite)EditorGUILayout.ObjectField(
                "배경 이미지",
                sequence.BackgroundImage,
                typeof(Sprite),
                false
            );
            
            // 배경 이미지 미리보기
            if(sequence.BackgroundImage != null)
            {
                Texture2D texture = AssetPreview.GetAssetPreview(sequence.BackgroundImage);
                if(texture != null)
                {
                    GUILayout.Label(texture, GUILayout.Width(200), GUILayout.Height(200));
                }
            }
            
            EditorGUILayout.Space();
            
            // 창 타입
            sequence.DialogType = (EndingDialogType)EditorGUILayout.EnumPopup("창 타입", sequence.DialogType);
            
            // 메시지 텍스트
            EditorGUILayout.LabelField("메시지 텍스트");
            sequence.MessageText = EditorGUILayout.TextArea(sequence.MessageText, GUILayout.Height(60));
            
            // 선택창일 경우 버튼 텍스트
            if(sequence.DialogType == EndingDialogType.Choice)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("버튼 텍스트", EditorStyles.boldLabel);
                sequence.YesButton1Text = EditorGUILayout.TextField("첫 번째 버튼", sequence.YesButton1Text);
                sequence.YesButton2Text = EditorGUILayout.TextField("두 번째 버튼", sequence.YesButton2Text);
            }
            
            EditorGUILayout.Space();
            
            // 진동 설정
            EditorGUILayout.LabelField("진동 설정", EditorStyles.boldLabel);
            sequence.UseVibration = EditorGUILayout.Toggle("진동 사용", sequence.UseVibration);
            
            if(sequence.UseVibration)
            {
                EditorGUI.indentLevel++;
                sequence.VibrationPattern = (VibrationPattern)EditorGUILayout.EnumPopup("진동 패턴", sequence.VibrationPattern);
                sequence.VibrationDelay = EditorGUILayout.FloatField("진동 딜레이 (초)", sequence.VibrationDelay);
                
                if(sequence.VibrationPattern == VibrationPattern.Custom)
                {
                    EditorGUILayout.LabelField("커스텀 패턴 (밀리초)", EditorStyles.miniLabel);
                    
                    int newSize = EditorGUILayout.IntField("배열 크기", sequence.CustomVibrationPattern != null ? sequence.CustomVibrationPattern.Length : 0);
                    if(newSize != (sequence.CustomVibrationPattern != null ? sequence.CustomVibrationPattern.Length : 0))
                    {
                        System.Array.Resize(ref sequence.CustomVibrationPattern, newSize);
                    }
                    
                    if(sequence.CustomVibrationPattern != null)
                    {
                        for(int i = 0; i < sequence.CustomVibrationPattern.Length; i++)
                        {
                            sequence.CustomVibrationPattern[i] = EditorGUILayout.LongField($"[{i}]", sequence.CustomVibrationPattern[i]);
                        }
                    }
                }
                
                EditorGUI.indentLevel--;
            }
        }
        
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }
    
    private void FindEndingManager()
    {
        endingManager = FindObjectOfType<EndingManager>();
        if(endingManager != null)
        {
            Debug.Log("[EndingEditor] EndingManager를 찾았습니다.");
        }
    }
    
    private void AddSequence()
    {
        Undo.RecordObject(endingManager, "Add Ending Sequence");
        endingManager.Sequences.Add(new EndingSequence());
        EditorUtility.SetDirty(endingManager);
    }
    
    private void RemoveSequence(int index)
    {
        Undo.RecordObject(endingManager, "Remove Ending Sequence");
        endingManager.Sequences.RemoveAt(index);
        if(selectedSequenceIndex == index)
        {
            selectedSequenceIndex = -1;
        }
        EditorUtility.SetDirty(endingManager);
    }
    
    private void MoveSequence(int index, int direction)
    {
        int newIndex = index + direction;
        if(newIndex < 0 || newIndex >= endingManager.Sequences.Count)
            return;
        
        Undo.RecordObject(endingManager, "Move Ending Sequence");
        EndingSequence temp = endingManager.Sequences[index];
        endingManager.Sequences[index] = endingManager.Sequences[newIndex];
        endingManager.Sequences[newIndex] = temp;
        
        if(selectedSequenceIndex == index)
        {
            selectedSequenceIndex = newIndex;
        }
        
        EditorUtility.SetDirty(endingManager);
    }

    public override void SaveChanges()
    {
        EditorUtility.SetDirty(endingManager);
        EditorSceneManager.MarkSceneDirty(endingManager.gameObject.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[EndingEditor] 변경사항이 저장되었습니다.");
    }
}
#endif