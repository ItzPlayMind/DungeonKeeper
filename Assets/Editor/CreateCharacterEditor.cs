using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;

public class CreateCharacterEditor : EditorWindow
{

    private const string PLAYER_PREFAB_PATH = "Assets/Prefabs/Players/";
    private const string PLAYER_ANIMATIONS_PATH = "Assets/Animations/Player/";

    [MenuItem("Character/Create Character")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow<CreateCharacterEditor>("Create Character");
    }

    private string characterName = "";
    private Sprite characterInitialSprite = null;

    private void OnGUI()
    {
        characterName = EditorGUILayout.TextField("Character Name", characterName);
        characterInitialSprite = (Sprite)EditorGUILayout.ObjectField("Idle Sprite", characterInitialSprite, typeof(Sprite));
        if (GUILayout.Button("Create"))
        {
            Create();
        }
    }

    public void Create()
    {
        var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PLAYER_PREFAB_PATH + "Soldier.prefab");
        var instance = PrefabUtility.InstantiatePrefab(basePrefab) as GameObject;
        var prefabVariant = PrefabUtility.SaveAsPrefabAsset(instance, PLAYER_PREFAB_PATH + "/" + characterName + ".prefab");
        prefabVariant.transform.Find("GFX").GetComponent<SpriteRenderer>().sprite = characterInitialSprite;
        Directory.CreateDirectory(PLAYER_ANIMATIONS_PATH + characterName);
        AssetDatabase.Refresh();
        string[] animationClipPaths = Directory.GetFiles(PLAYER_ANIMATIONS_PATH + "Lancer", "*.anim");
        List<AnimationClip> animations = new List<AnimationClip>();
        foreach (string animationClipPath in animationClipPaths)
        {
            var animationClip = (AnimationClip)AssetDatabase.LoadAssetAtPath(animationClipPath, typeof(AnimationClip));
            CopyFile(animationClip, ".anim");
            animations.Add(animationClip);
        }
        string animatorPath = Directory.GetFiles(PLAYER_ANIMATIONS_PATH + "Lancer", "*.overrideController")[0];
        AnimatorOverrideController overrideController = (AnimatorOverrideController)AssetDatabase.LoadAssetAtPath(animatorPath, typeof(AnimatorOverrideController));
        CopyFile(overrideController, ".overrideController");
        AssetDatabase.Refresh();
    }

    private void CopyFile(Object obj, string ext)
    {
        var newName = obj.name.Replace("Lancer", characterName);
        var oldPath = PLAYER_ANIMATIONS_PATH + "Lancer/" + obj.name + ext;
        var newPath = PLAYER_ANIMATIONS_PATH + characterName + "/" + newName + ext;
        AssetDatabase.CopyAsset(oldPath, newPath);
    }
}
