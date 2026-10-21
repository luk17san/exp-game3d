using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CombatV2Setup
{
    const string Folder = "Assets/Game/CombatV2";
    [MenuItem("Tools/Combat V2/Apply configuration")]
    public static void Apply()
    {
        Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
        var player = CopyController("Assets/Game/player_controller.controller",Folder+"/Player.controller");
        AddLayer(player,new[] { "Melee1", "Melee2", "Melee3", "Weapon1", "Weapon2", "Weapon3", "Block", "Hit", "GuardBreak" },new[] {
            "Assets/Game/Animation/Fight/Y Bot@Punching L.fbx",
            "Assets/Game/Animation/Fight/Y Bot@Punching R.fbx",
            "Assets/Game/Animation/Fight/Y Bot@Punching L.fbx",
            "Assets/Game/Animation/ybotWeaponAnimation/sword and shield slash.fbx",
            "Assets/Game/Animation/ybotWeaponAnimation/sword and shield slash (3).fbx",
            "Assets/Game/Animation/ybotWeaponAnimation/sword and shield slash (2).fbx",
            "Assets/Game/Animation/Block/Body Block.fbx",
            "Assets/Game/Animation/Fight/Y Bot@Hit Reaction.fbx",
            "Assets/Game/Animation/Fight/Y Bot@Stunned.fbx" },"Player");
        var enemy = CopyController("Assets/Game/Animation/EnemyAnimator.controller",Folder+"/Enemy.controller");
        AddLayer(enemy,new[]{"Windup","Attack","Block","Hit","GuardBreak"},new[]{
            AssetDatabase.GUIDToAssetPath("f1613afb92638e743bace9a9392b5ee7"),
            AssetDatabase.GUIDToAssetPath("f1613afb92638e743bace9a9392b5ee7"),
            "Assets/Game/Animation/ybotWeaponAnimation/sword and shield block idle.fbx",
            AssetDatabase.GUIDToAssetPath("b02430a054524da49b8a1b7470eb4b58"),
            "Assets/Game/Animation/Fight/Y Bot@Stunned.fbx" },"Enemy");
        ConfigurePlayer("Assets/Game/Prefab/Player v3.prefab",player);
        string skeleton = "Assets/Polytope Studio/Lowpoly_Characters/Prefabs/Modular_NPC/Skeleton/PT_Skeleton_Male_Modular.prefab";
        ConfigureEnemy(skeleton,enemy,false,false);
        CreateVariant(skeleton,Folder+"/SkeletonGuard.prefab",enemy,false,true);
        CreateVariant(skeleton,Folder+"/SkeletonHeavy.prefab",enemy,true,false);
        ConfigureScene();
        AssetDatabase.SaveAssets();
        Debug.Log("COMBAT_V2_SETUP_PASS: controllers, Player v3, skeleton, guard/heavy variants configured.");
    }
    static AnimatorController CopyController(string original,string path)
    {
        if (!File.Exists(path) && !AssetDatabase.CopyAsset(original,path)) throw new Exception("Cannot copy "+original);
        return AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
    }
    static void ConfigureScene()
    {
        var previous=EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var scene=EditorSceneManager.OpenScene("Assets/Game/Scenes/Game.unity",OpenSceneMode.Single);
            var enemies=UnityEngine.Object.FindObjectsOfType<Enemy>(true).OrderBy(e=>e.transform.position.x).ThenBy(e=>e.transform.position.z).ToArray();
            for(int i=0;i<enemies.Length;i++)
            {
                var config=new SerializedObject(enemies[i]);
                config.FindProperty("canBlock").boolValue=i%3==0;
                config.FindProperty("heavy").boolValue=i%3==1;
                config.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.RecordPrefabInstancePropertyModifications(enemies[i]);
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Debug.Log("COMBAT_V2_SCENE: configured "+enemies.Length+" enemies in Game.unity (guard / heavy / light).");
        }
        finally
        {
            if(previous.Any(s=>s.isLoaded && s.isActive)) EditorSceneManager.RestoreSceneManagerSetup(previous);
        }
    }
    static void AddLayer(AnimatorController controller,string[] names,string[] paths,string prefix)
    {
        if (!controller.parameters.Any(p=>p.name=="actionSpeed")) controller.AddParameter("actionSpeed",AnimatorControllerParameterType.Float);
        var parameters = controller.parameters;
        foreach(var p in parameters) if(p.name=="actionSpeed") p.defaultFloat=1;
        controller.parameters=parameters;
        if (!controller.layers.Any(l=>l.name=="CombatV2")) controller.AddLayer("CombatV2");
        var layers=controller.layers; var layer=layers.First(l=>l.name=="CombatV2"); layer.defaultWeight=0; layer.blendingMode=AnimatorLayerBlendingMode.Override;
        if(layer.stateMachine.defaultState==null) layer.stateMachine.defaultState=layer.stateMachine.AddState("Empty");
        for(int i=0;i<names.Length;i++)
        {
            var existing=layer.stateMachine.states.FirstOrDefault(s=>s.state.name==names[i]).state;
            var source=AssetDatabase.LoadAllAssetsAtPath(paths[i]).OfType<AnimationClip>().FirstOrDefault(c=>!c.name.StartsWith("__preview__"));
            if(source==null) throw new Exception("Missing clip: "+paths[i]);
            string assetPath=Folder+"/"+prefix+"_"+names[i]+".anim";
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
            if(clip==null)
            {
                clip=UnityEngine.Object.Instantiate(source); clip.name="V2_"+names[i];
                AnimationUtility.SetAnimationEvents(clip,Array.Empty<AnimationEvent>());
                var settings=AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime=names[i]=="Block";
                AnimationUtility.SetAnimationClipSettings(clip,settings);
                AssetDatabase.CreateAsset(clip,assetPath);
            }
            var state=existing!=null ? existing : layer.stateMachine.AddState(names[i]); state.motion=clip;
            state.writeDefaultValues=true;
            state.speed=1;
            if(names[i]!="Block") { state.speedParameter="actionSpeed"; state.speedParameterActive=true; }
            // Third fist strike reuses the left punch as a mirrored variation.
            if(names[i]=="Melee3") state.mirror=true;
        }
        controller.layers=layers; EditorUtility.SetDirty(controller);
    }
    static void ConfigurePlayer(string path,AnimatorController controller)
    {
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            root.GetComponent<Animator>().runtimeAnimatorController=controller;
            var player=root.GetComponent<PlayerStateMachine>();
            // Missing fields on older prefabs take the source defaults; persist them now.
            EditorUtility.SetDirty(player);
            if(root.GetComponent<CombatHud>()==null) root.AddComponent<CombatHud>();
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    static void ConfigureEnemy(string path,AnimatorController controller,bool heavy,bool blocking)
    {
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            root.GetComponent<Animator>().runtimeAnimatorController=controller;
            var config=new SerializedObject(root.GetComponent<Enemy>());
            config.FindProperty("heavy").boolValue=heavy; config.FindProperty("canBlock").boolValue=blocking;
            config.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    static void CreateVariant(string source,string path,AnimatorController controller,bool heavy,bool blocking)
    {
        if(File.Exists(path)) return;
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(source));
        try
        {
            instance.name=Path.GetFileNameWithoutExtension(path);
            var config=new SerializedObject(instance.GetComponent<Enemy>());
            config.FindProperty("heavy").boolValue=heavy; config.FindProperty("canBlock").boolValue=blocking;
            config.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(instance,path);
        }
        finally { UnityEngine.Object.DestroyImmediate(instance); }
    }
}
