using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

internal class AnimDef {
    public string name { get; set; }
    public int frameCount { get; set; }
    public bool loop { get; set; }
    
    public AnimDef(string name, int frameCount, bool loop=false) {
        this.name = name;
        this.frameCount = frameCount;
        this.loop = loop;
    }
}

public class GoatSpriteSheetAnimationGenerator : EditorWindow {
    
    private List<Sprite> sprites = new List<Sprite>();
    private string savePath = "Assets/Game/Animations/";
    private float frameRate = 10f;
    private string color = "";
    
    // Animation definitions
    private AnimDef[] animations = { 
        new AnimDef("Idle", 4, true), 
        new AnimDef("Walk", 6, true), 
        new AnimDef("Hit", 1), 
        new AnimDef("Jump", 3), 
        new AnimDef("Dash", 1), 
        new AnimDef("Run", 6, true) 
    };
    private int totalFrameCount => animations.Sum(a => a.frameCount);
    
    private Vector2 scrollPosition;
    
    [MenuItem("Tools/Generate Goat Animations")]
    static void Init() {
        var window = GetWindow<GoatSpriteSheetAnimationGenerator>("Goat Anim Generator");
        window.Show();
    }
    
    void OnGUI() {
        GUILayout.Label("Sprite Sheet Animation Generator", EditorStyles.boldLabel);
        
        GUILayout.Space(10);
        
        frameRate = EditorGUILayout.FloatField("Frame Rate (FPS)", frameRate);
        color = EditorGUILayout.TextField("Color", color);
        savePath = EditorGUILayout.TextField("Save Path", savePath);
        
        GUILayout.Space(10);
        
        // Drag and drop area
        GUILayout.Label("Drag ALL sprites here (in order!):", EditorStyles.boldLabel);
        
        Rect dropArea = GUILayoutUtility.GetRect(0f, 50f, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "Drop Sprites Here");
        
        Event evt = Event.current;
        
        if (dropArea.Contains(evt.mousePosition)) {
            if (evt.type == EventType.DragUpdated) {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                evt.Use();
            }
            else if (evt.type == EventType.DragPerform) {
                DragAndDrop.AcceptDrag();
                
                foreach (Object draggedObject in DragAndDrop.objectReferences) {
                    if (draggedObject is Sprite) 
                        sprites.Add(draggedObject as Sprite);
                }
                evt.Use();
            }
        }
        
        GUILayout.Space(10);
        
        // Show sprite count
        GUILayout.Label($"Sprites loaded: {sprites.Count} (need {totalFrameCount} total)", EditorStyles.helpBox);
        
        // Show sprite list
        if (sprites.Count > 0) {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(150));
            for (int i = 0; i < sprites.Count; i++) {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"{i}: {sprites[i].name}");
                if (GUILayout.Button("X", GUILayout.Width(30))) {
                    sprites.RemoveAt(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Clear All Sprites"))
            sprites.Clear();
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Generate Animations", GUILayout.Height(40))) {
            GenerateAnimations();
        }
    }
    
    void GenerateAnimations() {
        if (sprites.Count != totalFrameCount) {
            EditorUtility.DisplayDialog("Error", $"Need exactly {totalFrameCount} sprites, but have {sprites.Count}", "OK");
            return;
        }
        
        if (string.IsNullOrEmpty(color)) {
            EditorUtility.DisplayDialog("Error", "Please enter a color name", "OK");
            return;
        }
        
        savePath = savePath + color + "/";
        
        // Create save directory if it doesn't exist
        if (!Directory.Exists(savePath))
            Directory.CreateDirectory(savePath);
        
        int spriteIndex = 0;
        
        for (int row = 0; row < animations.Length; row++) {
            var animation = animations[row];
            int frameCount = animation.frameCount;
            string animName = animation.name;
            
            // Create animation clip
            AnimationClip clip = new AnimationClip();
            clip.frameRate = frameRate;
            clip.wrapMode = (animName == "Jump") ? WrapMode.ClampForever : WrapMode.Loop;

            // Set loop based on animation type
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = animation.loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            
            // Create keyframes
            EditorCurveBinding spriteBinding = new EditorCurveBinding();
            spriteBinding.type = typeof(SpriteRenderer);
            spriteBinding.path = "";
            spriteBinding.propertyName = "m_Sprite";
            
            ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[animation.loop ? frameCount + 1 : frameCount];
            
            // Only add loop-back frame for looping animations
            for (int frame = 0; frame < frameCount; frame++) {
                keyframes[frame] = new ObjectReferenceKeyframe();
                keyframes[frame].time = frame / frameRate;
                keyframes[frame].value = sprites[spriteIndex];
                spriteIndex++;
            }
            
            // Loop back to first frame
            if (animation.loop) {
                keyframes[frameCount] = new ObjectReferenceKeyframe();
                keyframes[frameCount].time = frameCount / frameRate;
                keyframes[frameCount].value = sprites[spriteIndex - frameCount];
            }
            
            AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyframes);
            
            // Save the clip with color in name: Goat[color]Idle.anim
            string clipPath = savePath + color + "Goat" + animName + ".anim";
            AssetDatabase.CreateAsset(clip, clipPath);
            Debug.Log("Created: " + clipPath);
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("Success!", "All 6 animations created successfully!", "OK");
    }
}