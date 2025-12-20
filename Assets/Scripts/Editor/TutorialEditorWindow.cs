#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class TutorialEditorWindow : EditorWindow
{
    private TutorialManager tutorialManager;
    private Vector2 scrollPosition;
    private int selectedSequenceIndex = -1;
    
    [MenuItem("Window/Tutorial Editor")]
    public static void ShowWindow()
    {
        GetWindow<TutorialEditorWindow>("Tutorial Editor");
    }
    
    private void OnEnable()
    {
        // Scene에서 TutorialManager 찾기
        FindTutorialManager();
    }
    
    private void OnGUI()
    {
        EditorGUILayout.LabelField("튜토리얼 에디터", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // TutorialManager 선택
        tutorialManager = (TutorialManager)EditorGUILayout.ObjectField(
            "Tutorial Manager",
            tutorialManager,
            typeof(TutorialManager),
            true
        );
        
        if(tutorialManager == null)
        {
            EditorGUILayout.HelpBox("Scene에 TutorialManager를 배치하거나 위 필드에 할당하세요.", MessageType.Warning);
            if(GUILayout.Button("Scene에서 찾기"))
            {
                FindTutorialManager();
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
        
        // 시퀀스 리스트
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        for(int i = 0; i < tutorialManager.Sequences.Count; i++)
        {
            DrawSequence(i);
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    private void DrawSequence(int index)
    {
        TutorialSequence sequence = tutorialManager.Sequences[index];
        
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
        
        // 위로/아래로 이동 버튼
        GUI.enabled = index > 0;
        if(GUILayout.Button("↑", GUILayout.Width(30)))
        {
            MoveSequence(index, -1);
        }
        GUI.enabled = index < tutorialManager.Sequences.Count - 1;
        if(GUILayout.Button("↓", GUILayout.Width(30)))
        {
            MoveSequence(index, 1);
        }
        GUI.enabled = true;
        
        // 삭제 버튼
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
        
        // 상세 정보 (Foldout)
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
            sequence.DialogType = (DialogType)EditorGUILayout.EnumPopup("창 타입", sequence.DialogType);
            
            // 메시지 텍스트
            EditorGUILayout.LabelField("메시지 텍스트");
            sequence.MessageText = EditorGUILayout.TextArea(sequence.MessageText, GUILayout.Height(60));
            
            // 선택창일 경우 버튼 텍스트
            if(sequence.DialogType == DialogType.Choice)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("버튼 텍스트", EditorStyles.boldLabel);
                sequence.YesButton1Text = EditorGUILayout.TextField("첫 번째 버튼", sequence.YesButton1Text);
                sequence.YesButton2Text = EditorGUILayout.TextField("두 번째 버튼", sequence.YesButton2Text);
            }
        }
        
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }
    
    private void FindTutorialManager()
    {
        tutorialManager = FindObjectOfType<TutorialManager>();
        if(tutorialManager != null)
        {
            Debug.Log("[TutorialEditor] TutorialManager를 찾았습니다.");
        }
    }
    
    private void AddSequence()
    {
        Undo.RecordObject(tutorialManager, "Add Tutorial Sequence");
        tutorialManager.Sequences.Add(new TutorialSequence());
        EditorUtility.SetDirty(tutorialManager);
    }
    
    private void RemoveSequence(int index)
    {
        Undo.RecordObject(tutorialManager, "Remove Tutorial Sequence");
        tutorialManager.Sequences.RemoveAt(index);
        if(selectedSequenceIndex == index)
        {
            selectedSequenceIndex = -1;
        }
        EditorUtility.SetDirty(tutorialManager);
    }
    
    private void MoveSequence(int index, int direction)
    {
        int newIndex = index + direction;
        if(newIndex < 0 || newIndex >= tutorialManager.Sequences.Count)
            return;
        
        Undo.RecordObject(tutorialManager, "Move Tutorial Sequence");
        TutorialSequence temp = tutorialManager.Sequences[index];
        tutorialManager.Sequences[index] = tutorialManager.Sequences[newIndex];
        tutorialManager.Sequences[newIndex] = temp;
        
        if(selectedSequenceIndex == index)
        {
            selectedSequenceIndex = newIndex;
        }
        
        EditorUtility.SetDirty(tutorialManager);
    }

    public override void SaveChanges()
    {
        EditorUtility.SetDirty(tutorialManager);
        EditorSceneManager.MarkSceneDirty(tutorialManager.gameObject.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[TutorialEditor] 변경사항이 저장되었습니다.");
    }
}
#endif