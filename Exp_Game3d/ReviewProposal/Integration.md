# Dokładne fragmenty integracji

> WERSJA v1, historyczna propozycja. Nowe wybory użytkownika w Behaviors-v2.md zastępują reguły combo i bloku z tego dokumentu. Nie wdrażać tych fragmentów jako realizacji v2.

To projekt zmian do zastosowania łącznie z plikami *.cs.txt. Fragmenty poniżej nie są samodzielnymi skryptami. Zachować istniejące pliki .meta i referencje prefabów.

## 1. PlayerStateMachine.cs — konfiguracja i combo

Dodać wewnątrz klasy:

```csharp
[Header("Combat timing")]
[SerializeField, Min(0.01f)] private float meleeAttackDuration = 0.4f;
[SerializeField, Min(0.01f)] private float weaponAttackDuration = 0.4f;
[SerializeField, Min(0f)] private float comboWindow = 0.3f;
private float _comboDeadline = float.NegativeInfinity;
private bool _comboUsesWeapon;
private bool _initialized;

public float MeleeAttackDuration => Mathf.Max(0.01f, meleeAttackDuration);
public float WeaponAttackDuration => Mathf.Max(0.01f, weaponAttackDuration);
public bool IsBlocking => isActiveAndEnabled &&
    _currentState is PlayerBlockState && _isBlockPressed &&
    _characterController.isGrounded;

public void StopHorizontalMovement()
{
    _appliedMovement.x = 0f;
    _appliedMovement.z = 0f;
    _animator.SetBool(_isWalkingHash, false);
    _animator.SetBool(_isRunningHash, false);
}

public void BeginComboAttack(bool usesWeapon)
{
    bool continueCombo = Time.time <= _comboDeadline &&
        _comboUsesWeapon == usesWeapon && _attackCount == 1;
    _attackCount = continueCombo ? 2 : 1;
    _comboUsesWeapon = usesWeapon;
    _comboDeadline = float.NegativeInfinity;
    _animator.SetInteger(_attackCountHash, _attackCount);
}

public void FinishComboAttack()
{
    if (_attackCount == 2) ResetCombo();
    else _comboDeadline = Time.time + Mathf.Max(0f, comboWindow);
}

public void ResetCombo()
{
    _attackCount = 0;
    _comboDeadline = float.NegativeInfinity;
    _animator.SetInteger(_attackCountHash, 0);
}

public void CloseDamageWindows()
{
    foreach (var dealer in GetComponentsInChildren<DamageDealer>(true))
        dealer.EndDealDamage();
}
```

Ataki i bloki celowo NIE dostają podstanu Idle. Idle potrafi przejść w Walk/Run, więc użycie go podczas zablokowanej akcji pozwoliłoby ponownie ruszać postacią. Metoda StopHorizontalMovement ustawia tylko ruch, bez uruchamiania kolejnego automatu.

W Awake usunąć dotychczasowy blok tworzenia i uruchamiania stanów (linie 135–140 w analizowanej wersji). Po inicjalizacji hashy, callbacków i setupJumpVariables dodać:

```csharp
_states = new PlayerStateFactory(this);
_currentState = _states.Grounded();
_initialized = true;
```

Zastąpić OnEnable / OnDisable; dodać OnDestroy:

```csharp
void OnEnable()
{
    if (!_initialized) return;
    _currentState.EnterStates();
    _playerInput.CharacterControls.Enable();
}

void OnDisable()
{
    if (!_initialized) return;
    _playerInput.CharacterControls.Disable();
    _currentState.ExitStates();
    StopAllCoroutines();
    _currentJumpResetRoutine = null;
    _attackDelay = null;
    _currentAttackDelay = null;
    CloseDamageWindows();
    ResetCombo();
    _activeAttack = false;
    _jumpCount = 0;
    _isJumping = false;
    _requireNewJumpPress = false;
    _requireNewAttackPress = false;
    _requireNewWeaponDrawPress = false;
    _currentMovementInput = Vector2.zero;
    _currentMovement = Vector3.zero;
    _appliedMovement = Vector3.zero;
    _cameraRelativeMovement = Vector3.zero;
    _isMovementPressed = _isRunPressed = _isJumpPressed = false;
    _isAttack1Pressed = _isBlockPressed = _isWeaponDrawPressed = false;
    _animator.SetInteger(_jumpCountHash, 0);
    _currentState = _states.Grounded();
}

void OnDestroy()
{
    _playerInput?.Dispose();
}
```

Powrót po ponownym włączeniu zachowuje informację o wyciągnięciu broni. Nie wyłącza ponownie animacji drawWeapon. Przy wyłączeniu kończymy wszystkie korutyny należące do tego kontrolera; obecnie są to opóźnienia skoków i stare opóźnienia walki.

W Update przenieść handleRotation() ZA obliczenie _cameraRelativeMovement, przed CharacterController.Move. Dodatkowo przed aktualizacją stanów:

```csharp
if (!_activeAttack && _attackCount != 0 && Time.time > _comboDeadline)
    ResetCombo();
```

W handleRotation przed Quaternion.LookRotation dodać:

```csharp
if (positionToLookAt.sqrMagnitude < 0.0001f) return;
```

Obecne kierowanie ataku w stronę kamery pozostaje. Nie zmieniamy tego na lock-on ani celowanie myszą.

## 2. PlayerIdleState.cs

Z EnterState usunąć wyłącznie:

```csharp
Ctx.Animator.SetBool(Ctx.IsMeleeAttackHash, false);
Ctx.Animator.SetBool(Ctx.IsWeaponAttackHash, false);
Ctx.Animator.SetBool(Ctx.IsBlockingHash, false);
```

Pozostałe zapisy ruchu zostają. Wybór podstanu w Grounded/Jump/Fall/Combat uporządkować do: brak ruchu → Idle; ruch + bieg → Run; pozostały ruch → Walk. Sam wciśnięty bieg bez ruchu nie powinien wybierać Run.

## 3. HealthSystem.cs — kierunkowy blok i zdrowie

Zachować istniejące nazwy serializowanych pól. Dodać pola:

```csharp
[Header("Blocking")]
[SerializeField, Range(0f, 360f)] private float blockAngle = 120f;
[SerializeField, Range(0f, 1f)] private float meleeBlockReduction = 0.5f;
[SerializeField, Range(0f, 1f)] private float weaponBlockReduction = 1f;
private PlayerStateMachine combat;
private bool isDead;
```

Zastąpić Start metodą Awake oraz zastąpić TakeDamage i Die:

```csharp
void Awake()
{
    animator = GetComponent<Animator>();
    combat = GetComponent<PlayerStateMachine>();
    maxHealth = Mathf.Max(1f, maxHealth);
    currentHealth = maxHealth;
    if (healthBar != null) healthBar.SetMaxHealth(maxHealth);
}

// Zachowanie zgodności starszych wywołań. Bez informacji o kierunku nie ma bloku.
public void TakeDamage(float damageAmount)
{
    ApplyDamage(damageAmount);
}

public bool TakeDamage(float damageAmount, Vector3 sourcePosition)
{
    if (isDead || float.IsNaN(damageAmount) ||
        float.IsInfinity(damageAmount) || damageAmount <= 0f) return false;

    Vector3 toSource = sourcePosition - transform.position;
    toSource.y = 0f;
    Vector3 forward = transform.forward;
    forward.y = 0f;
    bool fromFront = toSource.sqrMagnitude > 0.0001f &&
        forward.sqrMagnitude > 0.0001f &&
        Vector3.Angle(forward, toSource) <= blockAngle * 0.5f;
    if (combat != null && combat.IsBlocking && fromFront)
    {
        float reduction = combat.WeaponDrawed
            ? weaponBlockReduction : meleeBlockReduction;
        damageAmount *= 1f - Mathf.Clamp01(reduction);
    }
    return ApplyDamage(damageAmount);
}

private bool ApplyDamage(float amount)
{
    if (isDead || float.IsNaN(amount) || float.IsInfinity(amount) || amount <= 0f)
        return false;
    currentHealth = Mathf.Clamp(currentHealth - amount, 0f, maxHealth);
    if (healthBar != null) healthBar.SetHealth(currentHealth);
    if (currentHealth <= 0f) Die();
    else if (animator != null) animator.SetTrigger("damage");
    return true;
}

void Die()
{
    if (isDead) return;
    isDead = true;
    if (combat != null) combat.enabled = false;
    if (ragdoll != null) Instantiate(ragdoll, transform.position, transform.rotation);
    Destroy(gameObject);
}
```

W HitVFX dodać `if (hitVFX == null) return;` przed Instantiate.

Brak PlayerStateMachine oznacza brak tej mechaniki bloku — starszy kontroler Movement.cs nadal może otrzymywać obrażenia. Nie dokładamy mu automatycznie nowego sterowania.

## 4. EnemyDamageDealer.cs — przekazanie źródła ciosu

Przenieść inicjalizację z Start do Awake; dodać `private Enemy owner;` i w Awake `owner = GetComponentInParent<Enemy>();`. Zastąpić blok TryGetComponent:

```csharp
var health = hit.collider.GetComponentInParent<HealthSystem>();
if (health != null)
{
    hasDealtDamage = true;
    Vector3 source = owner != null ? owner.transform.position : transform.position;
    if (health.TakeDamage(weaponDamage, source)) health.HitVFX(hit.point);
}
```

Dodać `void OnDisable() { canDealDamage = false; }`.

To nadal raycast, nie nowy system hitboxów. Nie obiecujemy na tym etapie usunięcia pomijanych trafień przy szybkich animacjach.

## 5. DamageDealer.cs — bezpieczne okno obrażeń

Inicjalizację listy wykonać przy deklaracji: `List<GameObject> hasDealtDamage = new List<GameObject>();`. Usunąć jej ponowną inicjalizację w Start, a Start przenieść do Awake. Dodać `void OnDisable() { canDealDamage = false; }`.

W Update zamiast TryGetComponent na hit.transform:

```csharp
var enemy = hit.collider.GetComponentInParent<Enemy>();
if (enemy != null && !hasDealtDamage.Contains(enemy.gameObject))
{
    hasDealtDamage.Add(enemy.gameObject);
    enemy.TakeDamage(weaponDamage);
    enemy.HitVFX(hit.point);
}
```

Rejestrujemy cel przed wywołaniem obrażeń; wiele colliderów jednego Enemy daje jedną pozycję listy. Warstwy 8/9 pozostają zgodne z aktualnym projektem; przeniesienie do LayerMask to kolejna drobna migracja Inspectorów.

## 6. Ważne zależności i granice

- Nowy PlayerBaseState wymaga jednoczesnej zmiany Awake/OnEnable: pozostawienie ręcznego EnterState ominie obsługę aktywności.
- Stare korutyny ataków zostają usunięte wraz z zamianą obu klas ataku. Pola/properties AttackDelay i CurrentAttackDelay można usunąć dopiero po sprawdzeniu wszystkich odwołań; propozycja utrzymuje je dla małego diffu.
- PlayerCombatState pozostaje krótkim stanem przełączającym wyciągnięcie/chowanie, nie trwałym trybem walki.
- Konfiguracja docelowa: Player v3.prefab / PlayerStateMachine: Melee Attack Duration 0.4, Weapon Attack Duration 0.4, Combo Window 0.3. Na obiekcie z HealthSystem: Block Angle 120, Melee Block Reduction 0.5, Weapon Block Reduction 1.0.
- Pełne zakończenie ataku przez zdarzenie animacji wymagałoby identyfikacji akcji, aby opóźnione zdarzenie starego klipu nie kończyło nowej akcji. W tym etapie wybieramy lokalny zegar stanu i jawne zamknięcie obrażeń przy wyjściu.
- Stan Jump zachowuje dotychczasową fizykę i trzy skoki. Nie zmieniamy balansu skoku przy okazji naprawy walki.
- Nie zmieniono Assets, Packages ani scen. Przed wdrożeniem należy zweryfikować dostępne referencje i Animator w Unity oraz sprawdzić kompilację całej integracji.
