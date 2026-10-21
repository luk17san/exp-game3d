# Propozycja zmian — wersja do analizy

> Aktualizacja 2026-10-20: użytkownik zatwierdził wdrożenie. Aktualny kod jest w Assets/Game, a stan i wyniki w STATUS.md w katalogu projektu. Ten dokument i szkice *.cs.txt pozostają historyczną wersją v1; nie kopiować ich na wdrożenie v2.

Status: NIE wdrożono do gry. Aktualne ustalenia znajdują się w Behaviors-v2.md. Poniższy opis oraz pliki *.cs.txt i Integration.md dokumentują wcześniejszą wersję v1: dwa ciosy bez bufora i blok bez staminy. Wymagają aktualizacji do v2 przed wdrożeniem; nie są gotową implementacją nowych ustaleń. Nie wykonano kompilacji ani Play Mode.

## Zakres pierwszego wdrożenia

Najpierw naprawiamy cykl życia stanów, odpowiedzialność animacji, przerywanie ataku i blokowanie. Zachowujemy istniejące klasy, fabrykę, nazwy parametrów Animatora i zdarzenia animacji. Nie przepisujemy całego projektu.

| Plik docelowy w Assets/Game/Script | Dokładna zmiana | Efekt |
| --- | --- | --- |
| StateMachine/PlayerBaseState.cs | Zamiennik PlayerBaseState.cs.txt | Wejście/wyjście całego drzewa, brak aktualizacji starego podstanu |
| StateMachine/Attack/PlayerAttackState.cs | Nowy plik PlayerAttackState.cs.txt | Wspólny atak bez korutyn kończących poprzednie akcje |
| StateMachine/Attack/MeleeAttackState.cs | Zamiennik AttackStates.cs.txt, klasa MeleeAttackState | Wspólna logika, osobny parametr animacji |
| StateMachine/Attack/AttackWeaponState.cs | Zamiennik AttackStates.cs.txt, klasa WeaponAttackState | Jak wyżej; zachować istniejącą nazwę pliku i meta |
| StateMachine/Attack/PlayerBlockState.cs | Nowy plik PlayerBlockState.cs.txt | Blok zatrzymuje ruch, kończy się po puszczeniu lub utracie podłoża |
| StateMachine/Attack/MeleeBlockState.cs, WeaponBlockState.cs | Klasy z BlockStates.cs.txt | Usunięcie wyjątków; oba bloki mają wspólne reguły |
| StateMachine/PlayerStateMachine.cs | Zmiany z Integration.md | Konfiguracja, poprawny start, combo, sprzątanie po wyłączeniu |
| StateMachine/Movement/PlayerIdleState.cs | Usunąć trzy zapisy booli walki | Idle odpowiada tylko za ruch |
| HealthSystem.cs | Integracja opisana w Integration.md | Rzeczywista redukcja obrażeń z przodu |
| EnemyDamageDealer.cs | Integracja opisana w Integration.md | Przekazanie kierunku ataku, zużycie trafienia również przy bloku |
| Enemy.cs, AttackWeaponState.cs, PlayerCombatState.cs | Usunąć using UnityEditor... | Usunięcie zależności runtime od edytora |

## Decyzje dotyczące zachowania — do Twojej oceny

- Atak zatrzymuje ruch poziomy. Nie można anulować go kolejnym atakiem, skokiem ani blokiem; zejście z podłoża przerywa atak. To utrzymuje prostą strukturę obecnych przejść.
- Combo pozostaje dwuciosowe: kolejne osobne naciśnięcie do 0,3 s po końcu ataku daje drugi cios. Przytrzymanie nie powtarza ataków. Brak bufora naciśnięcia podczas trwającej animacji.
- Domyślny czas ataku pozostaje 0,4 s, ale osobno dla pięści i broni, w Inspectorze. Jest to czas blokady akcji, nie czas okna obrażeń. Okno obrażeń nadal otwierają istniejące Animation Events.
- Blok działa tylko na ziemi, zatrzymuje ruch. Proponowane wartości: stożek 120 stopni, redukcja pięściami 50%, bronią 100%. To nowe ustawienia balansu, a nie odtworzenie istniejącej mechaniki.
- Zablokowane trafienie zużywa trafienie danego zamachu, nawet gdy odebrano 0 HP. Przy pełnym bloku nie wyzwalamy animacji damage; osobny efekt bloku można dodać później.
- Zmiana rodzaju ataku zeruje combo. Przerwanie ataku i wyłączenie kontrolera również zeruje combo.
- Nie wdrażamy jeszcze wykrywania łuku miecza ani wspólnego systemu zdrowia dla AI i gracza. To osobny etap po ustabilizowaniu sterowania.

## Dlaczego nie wszystko jednocześnie

Zmiana zdrowia przeciwnika, AI, prefabów broni i wykrywania trafień w tym samym wdrożeniu utrudniłaby sprawdzenie, skąd pochodzi zmiana zachowania. Kolejny etap: hitboxy i pojedyncza instancja broni; następny: pościg/powrót AI i wspólne zdrowie.

Lokalny pakiet z Downloads wymaga najpierw sprawdzenia jego zawartości i licencji. Propozycja: osadzić pakiet w repo albo przypiąć konkretną rewizję repozytorium. Nie proponuję zgadywanego adresu ani usuwania zależności bez sprawdzenia użyć.

## Weryfikacja przed uznaniem wdrożenia za gotowe

1. Test cyklu stanów: każde Enter/Exit raz, wyjście dziecka przed rodzicem, brak ticka starego dziecka po przejściu; ponowne wejście w te same cache'owane instancje.
2. Test combo na czasach granicznych: pierwszy/drugi/trzeci cios, przerwa >0,3 s, zmiana rodzaju ataku, przerwanie spadaniem, wyłączenie i ponowne włączenie komponentu.
3. Test obrażeń: przód/bok/tył, bez bloku, redukcja 50/100%, kilka colliderów jednego celu, jedno trafienie na zamach, brak kolejnych obrażeń po przerwaniu ataku.
4. Kompilacja w Unity 2022.3.6f1 i build gracza: brak zależności UnityEditor.
5. Ręczny Play Mode na Player v3: ruch, bieg, trzy skoki, wyciąganie/chowanie, ataki, blok, spadanie, śmierć. Osobno regresja starszego Player.prefab z Movement.cs.
6. Przegląd animacji: czy klipy i przejścia mieszczą się w domyślnych 0,4 s; czy przerwanie nie pozostawia okna obrażeń i czy oba warianty broni mają właściwy parametr isBlock.

Powyższe to kryteria odbioru, nie wykonane testy. Na obecnym etapie można przeanalizować konkretne źródła i reguły; wdrożenie oraz walidacja nastąpią po Twojej ocenie.
