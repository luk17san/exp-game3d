using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class CombatV2Verification
{
    const string Key="CombatV2Verification.running";
    static IEnumerator routine;
    static readonly List<string> checks=new List<string>();
    static readonly List<string> errors=new List<string>();
    static double timeout;
    static int lastFrame = -1;
    static CombatV2Verification()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key,false))
            { checks.Clear(); lastFrame=-1; routine=Tests(); timeout=EditorApplication.timeSinceStartup+90; EditorApplication.update+=Tick; }
        };
    }
    [MenuItem("Tools/Combat V2/Run isolated Play Mode checks")]
    public static void PrepareAndRun() { CombatV2Setup.Apply(); Run(); }
    public static void Run()
    {
        SessionState.SetBool(Key,true);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }
    public static void Build()
    {
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes=new[]{"Assets/Game/Scenes/Menu.unity","Assets/Game/Scenes/Game.unity"},
            locationPathName="Builds/CombatV2/ExpGame3d.exe",
            target=BuildTarget.StandaloneWindows64,
            options=BuildOptions.Development
        });
        Directory.CreateDirectory("Tools/CombatV2/Results");
        File.WriteAllText("Tools/CombatV2/Results/build.txt",report.summary.result+"\nErrors: "+report.summary.totalErrors+"\nWarnings: "+report.summary.totalWarnings+"\nDuration: "+report.summary.totalTime);
        if(report.summary.result!=BuildResult.Succeeded) throw new Exception("Combat V2 player build failed");
        Debug.Log("COMBAT_V2_BUILD_PASS");
    }
    static void Tick()
    {
        try
        {
            if(EditorApplication.timeSinceStartup>timeout) throw new Exception("Play Mode verification timeout");
            if(lastFrame==Time.frameCount) return;
            lastFrame=Time.frameCount;
            if(routine.MoveNext()) return;
            Finish(true,null);
        }
        catch(Exception ex) { Finish(false,ex.ToString()); }
    }
    static void Finish(bool success,string failure)
    {
        Application.logMessageReceived-=CaptureError;
        EditorApplication.update-=Tick; SessionState.SetBool(Key,false);
        Directory.CreateDirectory("Tools/CombatV2/Results");
        File.WriteAllText("Tools/CombatV2/Results/playmode.txt",(success?"PASS ":"FAIL ")+checks.Count+" checks\n"+string.Join("\n",checks)+"\n"+failure);
        Debug.Log("COMBAT_V2_PLAYMODE_"+(success?"PASS":"FAIL")+": "+checks.Count+"\n"+failure);
        EditorApplication.ExitPlaymode();
        if(Application.isBatchMode) EditorApplication.Exit(success?0:1);
    }
    static void Check(bool condition,string message) { if(!condition) throw new Exception(message); checks.Add(message); }
    static void CaptureError(string message,string stack,LogType type) { if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert) errors.Add(message); }
    static void Set(object obj,string name,object value) => obj.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(obj,value);
    static object Call(object obj,string name,params object[] args) => obj.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(obj,args);
    static T Spawn<T>(string path,Vector3 position) where T:Component
    {
        var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path),position,Quaternion.identity);
        return root.GetComponent<T>();
    }
    static IEnumerator Tests()
    {
        Time.timeScale=1; Time.captureFramerate=60;
        errors.Clear(); Application.logMessageReceived+=CaptureError;
        var guard=new CombatGuard(); guard.Initialize();
        Check(guard.Current==100,"Guard initializes full");
        Check(!guard.Spend(30,0) && guard.Current==70,"Guard spends base damage");
        guard.Tick(1,.5f,false); Check(guard.Current==70,"Guard regeneration delay");
        guard.Tick(1,2,false); Check(guard.Current==90,"Guard regeneration resumes");
        guard.Tick(1,3,true); Check(guard.Current==90,"No regeneration while blocking");
        Check(guard.Spend(90,3) && guard.Current==0,"Exact stamina cost breaks guard");

        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube); floor.name="Verification ground";
        floor.transform.position=new Vector3(0,-.5f,0); floor.transform.localScale=new Vector3(50,1,50);
        var cam=new GameObject("Verification camera").AddComponent<Camera>(); cam.tag="MainCamera"; cam.transform.position=new Vector3(0,3,-5);
        var player=Spawn<PlayerStateMachine>("Assets/Game/Prefab/Player v3.prefab",Vector3.zero);
        var health=player.GetComponent<HealthSystem>();
        Physics.SyncTransforms(); player.CharacterController.Move(Vector3.down*.1f);
        yield return null;
        float until=Time.time+.15f; while(Time.time<until) yield return null;
        Check(player.CharacterController.isGrounded,"Player fixture grounded: position="+player.transform.position+", state="+player.CurrentState.GetType().Name);
        Check(player.Animator.GetLayerIndex("CombatV2")>=0,"Player combat animation layer installed");
        Check(player.GetComponentsInChildren<DamageDealer>(true).Length>=2,"Two hand contact volumes installed");
        var trace=new List<string>();
        Probe parent=new Probe(player,trace,"parent",true), child=new Probe(player,trace,"child",false), next=new Probe(player,trace,"next",true);
        parent.child=child; parent.next=next; player.ChangeState(parent); parent.UpdateStates();
        Check(string.Join(",",trace)=="parent.enter,child.enter,parent.update,child.exit,parent.exit,next.enter","State transition closes child before parent without stale update");
        player.ChangeState(new PlayerStateFactory(player).Grounded());

        Set(player,"attackEdge",true);
        until=Time.time+.04f; while(Time.time<until) yield return null;
        Check(player.ActiveAttack && player.AttackCount==1,"Fresh press starts first strike");
        Check(player.Animator.GetLayerWeight(player.Animator.GetLayerIndex("CombatV2"))==1,"Action owns full body layer");
        float expectedSpeed=0;
        foreach(var clip in player.Animator.runtimeAnimatorController.animationClips)
            if(clip.name=="Player_Melee1") expectedSpeed=clip.length/player.meleeCombo[0].duration;
        Check(expectedSpeed>0 && Mathf.Abs(player.Animator.GetFloat("actionSpeed")-expectedSpeed)<.001f,"Animation speed matches configured strike duration");
        Call(player,"SetJumpInput",true);
        Check(player.RequireNewJumpPress,"Jump pressed during strike requires release rather than buffering");
        Call(player,"SetJumpInput",false);
        Check(!player.RequireNewJumpPress && !player.IsJumpPressed,"Releasing jump clears the press requirement");
        Set(player,"attackEdge",true); // deliberately early, must be discarded
        until=Time.time+.05f; while(Time.time<until) yield return null;
        until=Time.time+.27f; while(Time.time<until) yield return null;
        Check(!player.ActiveAttack,"Early press does not buffer a second strike");
        player.ResetCombo(); Set(player,"attackEdge",true);
        until=Time.time+.17f; while(Time.time<until) yield return null;
        Set(player,"attackEdge",true);
        until=Time.time+.17f; while(Time.time<until) yield return null;
        Check(player.ActiveAttack && player.AttackCount==2,"Late press buffers second strike");
        float hp=health.currentHealth;
        health.ReceiveDamage(1,player.transform.position+Vector3.forward);
        Check(player.ActiveAttack && health.currentHealth==hp-1,"Incoming hit reduces HP without interrupting own strike");
        until=Time.time+.13f; while(Time.time<until) yield return null;
        Set(player,"attackEdge",true);
        until=Time.time+.19f; while(Time.time<until) yield return null;
        Check(player.ActiveAttack && player.AttackCount==3,"Late press buffers third strike");
        until=Time.time+.35f; while(Time.time<until) yield return null;
        Check(!player.ActiveAttack && player.AttackCount==0,"Third strike ends and resets combo");

        // Add a small runtime NavMesh before instantiating agents in the physics fixtures.
        var settings=NavMesh.GetSettingsByIndex(0);
        var source=new NavMeshBuildSource { shape=NavMeshBuildSourceShape.Box, transform=Matrix4x4.TRS(new Vector3(0,-.1f,0),Quaternion.identity,new Vector3(40,.2f,40)), size=Vector3.one, area=0 };
        var data=NavMeshBuilder.BuildNavMeshData(settings,new List<NavMeshBuildSource>{source},new Bounds(Vector3.zero,Vector3.one*50),Vector3.zero,Quaternion.identity);
        var handle=NavMesh.AddNavMeshData(data);

        player.WeaponDrawed=true; Set(player,"block",true);
        until=Time.time+.05f; while(Time.time<until) yield return null;
        Check(player.IsBlocking,"Held block enters guard");
        hp=health.currentHealth;
        var result=health.ReceiveDamage(100,player.transform.position+player.transform.forward);
        Check(result==HitResult.GuardBroken && health.currentHealth==hp,"Guard-breaking strike is still reduced");
        Check(player.IsGuardBroken && player.guard.Current==0,"Guard break starts stun with zero stamina");
        health.ReceiveDamage(1,player.transform.position+player.transform.forward);
        Check(health.currentHealth==hp-1,"Subsequent strike bypasses broken guard");
        until=Time.time+1.25f; while(Time.time<until) yield return null;
        Check(!player.IsGuardBroken && !player.IsBlocking,"Held block does not automatically resume after break");
        Set(player,"block",false);
        until=Time.time+.05f; while(Time.time<until) yield return null;
        Vector3 before=player.transform.position;
        health.ReceiveDamage(1,player.transform.position+Vector3.forward);
        Check(player.IsReacting,"Ordinary hit starts reaction outside own strike");
        until=Time.time+.3f; while(Time.time<until) yield return null;
        Check(Vector3.Distance(before,player.transform.position)>.15f,"Hit reaction applies collision-aware push");
        player.ChangeState(new PlayerStateFactory(player).Jump());
        player.gameObject.SetActive(false);
        Check(player.CurrentJumpResetRoutine==null,"Disabling a jumping object leaves no reset coroutine");
        player.gameObject.SetActive(true);
        Check(!player.ActiveAttack && player.JumpCount==0,"Reactivation clears interrupted action state");

        // Physics contacts: two targets, each with multiple colliders, one strike.
        player.enabled=false; player.ActiveAttack=true;
        var first=Spawn<Enemy>("Assets/Game/CombatV2/SkeletonGuard.prefab",new Vector3(0,0,2));
        var second=Spawn<Enemy>("Assets/Game/CombatV2/SkeletonHeavy.prefab",new Vector3(.15f,0,2));
        first.enabled=false; second.enabled=false;
        var c1=first.gameObject.AddComponent<SphereCollider>(); c1.radius=.3f; c1.center=Vector3.up;
        var c2=second.gameObject.AddComponent<SphereCollider>(); c2.radius=.3f; c2.center=Vector3.up;
        first.gameObject.layer=second.gameObject.layer=9;
        var contact=new GameObject("Verification contact").AddComponent<DamageDealer>();
        contact.transform.position=new Vector3(0,1,2); contact.ConfigureFist(player,10,.5f);
        player.StartStrike(false); contact.StartDealDamage(); Physics.SyncTransforms();
        float hp1=first.currentHealth,hp2=second.currentHealth;
        Call(contact,"LateUpdate"); Call(contact,"LateUpdate");
        Check(first.currentHealth==hp1-10 && second.currentHealth==hp2-10,"One fist strikes multiple enemies exactly once each");
        first.enabled=true; second.enabled=true;
        Call(first,"SetPhase",Enemy.Phase.GuardBroken,999f); Call(second,"SetPhase",Enemy.Phase.GuardBroken,999f);
        player.FinishStrike(); Check(player.LastTarget==first,"Nearest contacted enemy becomes guard target");
        second.transform.position=new Vector3(0,0,1);
        player.StartStrike(false); player.RegisterHit(first); player.RegisterHit(second); player.FinishStrike();
        Check(player.LastTarget==first,"Previously attacked target wins over a nearer contacted enemy");
        second.transform.position=new Vector3(.15f,0,2);
        var wall=GameObject.CreatePrimitive(PrimitiveType.Cube); wall.transform.position=new Vector3(0,1,1); wall.transform.localScale=new Vector3(3,3,.2f);
        player.StartStrike(false); contact.EndDealDamage(); contact.StartDealDamage(); Physics.SyncTransforms();
        hp1=first.currentHealth; Call(contact,"LateUpdate");
        Check(first.currentHealth==hp1,"World wall prevents contact damage through obstacles");
        UnityEngine.Object.Destroy(wall);

        Call(first,"SetPhase",Enemy.Phase.Windup,.4f);
        first.ReceiveDamage(1,player.transform.position);
        Check(first.State==Enemy.Phase.HitReaction,"Incoming hit interrupts enemy windup");
        Call(first,"SetPhase",Enemy.Phase.Swing,.45f);
        first.ReceiveDamage(1,player.transform.position);
        Check(first.State==Enemy.Phase.Swing,"Incoming hit does not interrupt enemy swing");
        first.guard.Initialize(); Call(first,"SetPhase",Enemy.Phase.Block,.6f);
        hp1=first.currentHealth;
        var blocked=first.ReceiveDamage(100,first.transform.position+first.transform.forward);
        Check(blocked==HitResult.GuardBroken && first.currentHealth==hp1,"Enemy guard-breaking hit retains damage reduction");
        Check(first.State==Enemy.Phase.GuardBroken,"Enemy enters guard break phase");

        // Shared coordinator test uses real readiness, not a mocked slot counter.
        player.transform.position=Vector3.zero; Physics.SyncTransforms();
        var attackers=new List<Enemy>();
        for(int i=0;i<3;i++)
        {
            var enemy=Spawn<Enemy>("Assets/Game/CombatV2/SkeletonGuard.prefab",new Vector3((i-1)*.6f,0,1));
            Set(enemy,"player",health); Set(enemy,"nextAttack",0f); Call(enemy,"SetPhase",Enemy.Phase.Chase,0f); attackers.Add(enemy);
        }
        Check(EnemyAttackCoordinator.RequestAttack(attackers[0],player.transform),"Coordinator grants first attacker");
        Check(EnemyAttackCoordinator.RequestAttack(attackers[1],player.transform),"Coordinator grants second attacker");
        Check(!EnemyAttackCoordinator.RequestAttack(attackers[2],player.transform),"Coordinator refuses third concurrent attacker");
        EnemyAttackCoordinator.Release(attackers[0]);
        Check(EnemyAttackCoordinator.RequestAttack(attackers[2],player.transform),"Released slot allows waiting attacker");
        foreach(var enemy in attackers) UnityEngine.Object.Destroy(enemy.gameObject);
        handle.Remove(); UnityEngine.Object.Destroy(data);
        yield return null;

        // Smoke the actual configured game scene, separately from fixtures.
        yield return null;
        var operation=SceneManager.LoadSceneAsync("Assets/Game/Scenes/Game.unity",LoadSceneMode.Single);
        while(!operation.isDone) yield return null;
        until=Time.time+1; while(Time.time<until) yield return null;
        var scenePlayer=UnityEngine.Object.FindObjectOfType<PlayerStateMachine>();
        Check(scenePlayer!=null && scenePlayer.GetComponent<HealthSystem>()!=null,"Actual Game scene loads configured Player v3");
        Check(UnityEngine.Object.FindObjectsOfType<Enemy>().Length>0,"Actual Game scene has live enemies");
        Check(errors.Count==0,"No runtime errors or exceptions: "+string.Join("; ",errors));
    }
    sealed class Probe:PlayerBaseState
    {
        readonly List<string> trace; readonly string name;
        public Probe child,next;
        public Probe(PlayerStateMachine p,List<string> t,string n,bool root):base(p,null) { trace=t;name=n;IsRootState=root; }
        public override void EnterState(){trace.Add(name+".enter");if(child!=null)SetSubState(child);}
        public override void ExitState(){trace.Add(name+".exit");}
        public override void UpdateState(){trace.Add(name+".update");if(next!=null)SwitchState(next);}
        public override void CheckSwitchState(){}
        public override void InitalizeSubState(){}
    }
}
