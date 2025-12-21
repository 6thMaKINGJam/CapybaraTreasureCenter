#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SoundManager))]
public class SoundManagerEditor : Editor {
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();
        
        SoundManager manager = (SoundManager)target;
        
        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("=== 에디터 테스트 ===", EditorStyles.boldLabel);
        
        // BGM 테스트
        EditorGUILayout.LabelField("BGM 재생", EditorStyles.miniBoldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("MainHome")) {
            manager.EditorPlayBGM("MainHome");
        }
        if (GUILayout.Button("Tutorial")) {
            manager.EditorPlayBGM("Tutorial");
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Game")) {
            manager.EditorPlayBGM("Game");
        }
        if (GUILayout.Button("Ending")) {
            manager.EditorPlayBGM("Ending");
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        
        // FX 테스트
        EditorGUILayout.LabelField("효과음 재생", EditorStyles.miniBoldLabel);
        
        int buttonCount = 0;
        EditorGUILayout.BeginHorizontal();
        
        foreach (SoundType type in System.Enum.GetValues(typeof(SoundType))) {
            if (type.ToString().StartsWith("_")) continue; // 플레이스홀더 제외
            
            if (GUILayout.Button(type.ToString())) {
                manager.EditorPlayFX(type);
            }
            
            buttonCount++;
            if (buttonCount % 3 == 0) {
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
            }
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        
        // 정지 버튼
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("Stop All", GUILayout.Height(30))) {
            manager.EditorStopAll();
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.Space(10);
        
        // 볼륨 테스트
        EditorGUILayout.LabelField("볼륨 조절 테스트", EditorStyles.miniBoldLabel);
        
        float bgmVol = manager.BGMVolume;
        float newBgmVol = EditorGUILayout.Slider("BGM Volume", bgmVol, 0f, 1f);
        if (!Mathf.Approximately(bgmVol, newBgmVol)) {
            manager.BGMVolume = newBgmVol;
        }
        
        float fxVol = manager.FXVolume;
        float newFxVol = EditorGUILayout.Slider("FX Volume", fxVol, 0f, 1f);
        if (!Mathf.Approximately(fxVol, newFxVol)) {
            manager.FXVolume = newFxVol;
        }
    }
}
#endif