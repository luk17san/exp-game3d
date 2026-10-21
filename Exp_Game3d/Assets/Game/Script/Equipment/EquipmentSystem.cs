using UnityEngine;
public class EquipmentSystem : MonoBehaviour
{
    [SerializeField] GameObject weaponHolder;
    [SerializeField] GameObject weapon;
    [SerializeField] GameObject weaponSheath;
    GameObject instance;
    PlayerStateMachine owner;
    DamageDealer[] dealers;
    bool inHand;
    void Awake() { owner = GetComponent<PlayerStateMachine>(); }
    void Start() { EnsureWeapon(); if (owner != null && owner.WeaponDrawed) DrawWeapon(); else SheathWeapon(); }
    void EnsureWeapon()
    {
        if (instance != null || weapon == null || weaponSheath == null) return;
        instance = Instantiate(weapon,weaponSheath.transform);
        dealers = instance.GetComponentsInChildren<DamageDealer>(true);
        foreach (var dealer in dealers) dealer.Bind(owner);
    }
    public void DrawWeapon()
    {
        if (owner != null && !owner.WeaponDrawed) return; // ignore stale animation events
        EnsureWeapon(); if (instance == null || weaponHolder == null) return;
        SetDamageWindow(false); instance.transform.SetParent(weaponHolder.transform,false); inHand = true;
    }
    public void SheathWeapon()
    {
        if (owner != null && owner.WeaponDrawed) return;
        EnsureWeapon(); if (instance == null || weaponSheath == null) return;
        SetDamageWindow(false); instance.transform.SetParent(weaponSheath.transform,false); inHand = false;
    }
    // CombatV2 owns timing; legacy Animation Events are ignored for its controller.
    public void StartDealDamage() { if (owner == null) SetDamageWindow(true); }
    public void EndDealDamage() { if (owner == null) SetDamageWindow(false); }
    public void SetDamageWindow(bool enabled)
    {
        if (dealers == null) return;
        foreach (var dealer in dealers) { if (enabled && inHand) dealer.StartDealDamage(); else dealer.EndDealDamage(); }
    }
    void OnDisable() { SetDamageWindow(false); }
}
