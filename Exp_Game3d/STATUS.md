# Combat V2 — stan wdrożenia, 2026-10-20

## Wdrożone

- Cykl życia hierarchicznej maszyny stanów: pełne wejście/wyjście podstanów, bez aktualizacji opuszczonego poddrzewa.
- Gracz: zatrzymanie podczas ataku, trzy równe szybkie ciosy, jeden bufor końcowego naciśnięcia, brak powtarzania przy trzymaniu przycisku. Obrażenia nie przerywają własnego zamachu; śmierć i spadanie go kończą.
- Celowanie: ostatnio zaatakowany przeciwnik w zasięgu; inaczej kierunek kamery. Kierunek ustalany na początku każdego ciosu.
- Kontakt: objętość broni/pięści i jej droga między klatkami, wiele celów, wspólna rejestracja jednego trafienia na Enemy na cios (także między dwiema pięściami), przeszkody blokują obrażenia.
- Cel gardy: zachowanie poprzedniego celu, jeśli trafiony i żywy; inaczej najbliższy żywy trafiony. Bez losowego wyboru na podstawie kolejności colliderów.
- Garda: 40% chodu, obrót do celu niezależnie od kamery, kierunkowa redukcja, stamina, przełamanie na 1,2 s. Przełamujący cios nadal korzysta z redukcji. Po przełamaniu wymagane puszczenie przycisku i odzyskanie staminy.
- Reakcja gracza poza atakiem: 0,25 s i wygaszane odepchnięcie do 0,30 m z kolizjami. Kolejne trafienia nie odnawiają reakcji.
- AI: współdzielony limit dwóch napastników, przygotowanie 0,4 s, zamach bez naprowadzania, odpoczynek; trafienie przerywa przygotowanie, nie zamach. Lekki odpychany, ciężki bez przesunięcia. Wybrane typy blokują z opóźnieniem, limitem gardy i otwarciem na kontrę.
- AI: widoczność i utrata celu, ostatnia widziana pozycja, sprawdzenie i powrót na miejsce startowe; ponowne wykrycie wznawia pościg.
- Osobna warstwa CombatV2 w kopiach Animatorów, zsynchronizowana z czasami akcji; okna obrażeń są konfigurowane jako część czasu ciosu. Historyczne zdarzenia animacji nie mogą nadpisać aktualnej akcji.
- Jedna instancja wyposażonej broni, przenoszona między dłonią i pochwą, z przypisanym właścicielem trafień.
- Prosty skalowany HUD staminy, przełamania i celu gardy; kolor efektu cząsteczkowego odróżnia blok i przełamanie od zwykłego trafienia.
- Pakiet Scene Reference Attribute osadzony w Packages z zachowaną licencją MIT; brak zależności od Downloads.

## Gdzie konfigurować

| Obiekt / asset | Komponent i parametry |
| --- | --- |
| Assets/Game/Prefab/Player v3.prefab | PlayerStateMachine: Walk Speed, Run Multiplier, Rotation Speed, Melee Combo / Weapon Combo (3 wpisy: Animation State, Duration, Hit Start/End, Buffer), Combo Window, Melee/Weapon Aim Range |
| Ten sam prefab | Guard (Max Stamina, Regeneration, Regeneration Delay, Angle, Minimum To Block, Guard Damage Multiplier, Break Duration), Melee/Weapon Block Reduction, Block Move Multiplier, Hit Reaction Duration, Hit Push Distance |
| Ten sam prefab | Fist Damage, Fist Radius; CombatHud: Position, Size, Stamina Color |
| Prefab wyposażonej broni | DamageDealer: Weapon Length, Weapon Damage, Contact Radius, Target Layers; oś broni to lokalne -up |
| Assets/Game/CombatV2/SkeletonGuard.prefab | Enemy: Can Block = true, Heavy = false; parametry gardy i opóźnienia obrony |
| Assets/Game/CombatV2/SkeletonHeavy.prefab | Enemy: Heavy = true, Can Block = false; ciężki nadal reaguje na przerwane przygotowanie |
| Bazowy PT_Skeleton_Male_Modular.prefab | Lekki, bez bloku; zachowane dotychczasowe referencje modelu, VFX, ragdolla i wyposażenia |
| Enemy na scenie Assets/Game/Scenes/Game.unity | Scena zawiera jednego wroga; ustawiono go jako lekki typ blokujący. Dodatkowe typy przygotowano jako prefaby, bez dodawania kolejnych przeciwników do sceny |
| Enemy | Attack Range, Attack CD, Windup/Swing/Recovery Duration, Hit Start/End, Aggro/Lose Target Range, Lost Sight Delay, Search Duration, Navigation Timeout, Waiting Radius |
| Enemy | Block Reaction Delay 0.15, Block Hold Duration 0.6, Block Opening Duration 0.35; ustawienia staminy per typ |

Hit Start/End to wartości 0–1 w obrębie całej akcji, nie sekundy. Domyślnie 0.25–0.7. Przy czasie 0.3 s oznacza to 0.075–0.21 s. Bufor 0.2 s otwiera się po 0.1 s. Dobór okien wymaga obejrzenia kontaktu w animacjach.

## Weryfikacja

- Unity 2022.3.6f1: kompilacja i konfiguracja assetów wykonane pomyślnie.
- Końcowy przebieg: PASS 44 automatycznych sprawdzeń Play Mode, w tym synchronizacja szybkości animacji, kontakty fizyczne, combo, przerwania stanów, garda gracza/AI, wybór celu, limit napastników, blokada buforowania skoku, wyłączenie skaczącej postaci i wczytanie faktycznej sceny Game. Brak błędów runtime w tym przebiegu. Obsługa skoku sprawdzona przez ten sam handler co InputAction; fizyczne urządzenie nie było testowane.
- Końcowy build Windows (Development, Mono, Menu + Game): Succeeded, 0 błędów, 2 ostrzeżenia, 18,20 s. Program: Builds/CombatV2/ExpGame3d.exe. Nie uruchamiano samodzielnego exe — sukces budowania nie oznacza sprawdzenia jego interaktywnej rozgrywki.
- Dwa ostrzeżenia buildu dotyczą aktualizacji Ambient/Reflection Probes bez urządzenia graficznego przy `-nographics`; oświetlenie należy obejrzeć w normalnym edytorze. Log licencjonowania zawiera także komunikaty środowiskowe, ale Unity zakończyło kompilację, testy i build kodem 0.
- Dowody: Tools/CombatV2/Results/playmode.txt (44 sprawdzenia), build.txt (podsumowanie buildu), verify.log i build.log (lokalne logi ignorowane przez Git).
- Testy są izolowane i programowe; końcowe wczytanie Game to smoke test, nie ocena pełnej rozgrywki.

## Do sprawdzenia ręcznie

- Wygląd i tempo trzech ciosów, okna kontaktu, retargetowanie animacji na szkielecie, blending gardy i reakcji, zachowanie kamery oraz HUD przy różnych rozdzielczościach.
- Trzeci cios pięścią korzysta z lustrzanego wariantu istniejącego lewego ciosu — nie dodano nowej autorskiej animacji. Broń korzysta z trzech istniejących klipów slash.
- Fizyka na schodach, zboczach, przy krawędziach i podczas dużych zmian pozycji; pościg i powrót AI w docelowym terenie, obejścia przeszkód i zachowanie większej grupy na istniejącym NavMesh.
- Fizyczne wejście z klawiatury i pada. Odradzanie i menu nie zostały rozbudowane w ramach tego wdrożenia.
- HUD jest funkcjonalnym widokiem IMGUI, nie docelową stylizacją UI. Nie dodano nowych nagrań dźwięków walki.

## Powtarzanie kontroli

PowerShell z katalogu projektu: `./Tools/CombatV2/Run.ps1 -Action Verify` (Play Mode), `-Action Build` (Windows), `-Action Setup` (konfiguracja assetów). Unity działa w tle; nie uruchamiać drugiej instancji na tym samym projekcie podczas pracy edytora.

Nie wykonano commit ani push. Historyczne szkice w ReviewProposal pozostają materiałem z etapu projektowania, nie instalatorem.
