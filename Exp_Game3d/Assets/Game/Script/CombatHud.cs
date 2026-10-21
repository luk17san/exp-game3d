using UnityEngine;

// Minimal runtime HUD; all rectangles scale from a 1920x1080 reference resolution.
public class CombatHud : MonoBehaviour
{
    [SerializeField] Vector2 position = new Vector2(32,80);
    [SerializeField] Vector2 size = new Vector2(230,18);
    [SerializeField] Color staminaColor = new Color(.2f,.8f,.45f);
    PlayerStateMachine player;
    Texture2D pixel;
    void Awake() { player=GetComponent<PlayerStateMachine>(); pixel=Texture2D.whiteTexture; }
    void OnGUI()
    {
        if(player==null || !player.isActiveAndEnabled) return;
        Matrix4x4 previous=GUI.matrix;
        GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1920f,Screen.height/1080f,1));
        Color color=GUI.color;
        GUI.color=Color.black; GUI.DrawTexture(new Rect(position,size),pixel);
        GUI.color=player.IsGuardBroken ? Color.red : staminaColor;
        GUI.DrawTexture(new Rect(position.x+2,position.y+2,(size.x-4)*Mathf.Clamp01(player.StaminaNormalized),size.y-4),pixel);
        GUI.color=Color.white;
        GUI.Label(new Rect(position.x,position.y-23,300,24),player.IsGuardBroken ? "PRZEŁAMANIE GARDY" : "STAMINA");
        if(player.LastTarget!=null)
        {
            var camera=Camera.main;
            if(camera!=null)
            {
                Vector3 screen=camera.WorldToScreenPoint(player.LastTarget.transform.position+Vector3.up*2.2f);
                if(screen.z>0) GUI.Label(new Rect(screen.x*1920/Screen.width-30,(Screen.height-screen.y)*1080/Screen.height,80,24),"▼ CEL");
            }
        }
        GUI.color=color; GUI.matrix=previous;
    }
}
