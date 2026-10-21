# Zachowania walki v2 — projekt do omówienia

> Aktualizacja 2026-10-20: ustalenia wdrożono do kodu; parametry robocze przyjęto jako konfigurowalne wartości startowe. Bieżący zakres, różnice względem planu, dowody weryfikacji i ograniczenia opisuje STATUS.md w katalogu projektu. Poniższy tekst zachowuje rozróżnienie ustaleń i propozycji z rozmowy.

## Wybrane przez użytkownika

1. Postać zatrzymuje się podczas ataku; atak trwa do końca.
2. Maksymalnie trzy ciosy; każde kolejne naciśnięcie, bufor przy końcu ciosu.
3. Kierunkowy blok zużywa staminę i może zostać przełamany.
4. Cios przełamujący nadal podlega redukcji bloku; dopiero po rozliczeniu tego trafienia następuje 1,2 s ogłuszenia.
5. Podczas bloku postać chodzi z 40% prędkości chodu i jest zwrócona do przeciwnika, niezależnie od kierunku kamery.
6. Cel gardy to ostatni przeciwnik zaatakowany przez gracza; nie wybieramy automatycznie najbliższego.
7. Zwykłe trafienie poza własnym atakiem powoduje reakcję 0,25 s i niewielkie odepchnięcie.
8. Combo jest szybkie: trzy krótkie ciosy mają równe obrażenia i równy nacisk na gardę. Trzeci cios nie ma premii do siły ani dodatkowego spowolnienia.
9. Każdy cios kieruje się do ostatnio zaatakowanego przeciwnika, jeśli cel jest w zasięgu. Bez poprawnego celu w zasięgu kierunek wynika z kamery. Kierunek ustalamy na początku każdego ciosu i zachowujemy przez cały zamach.
10. Broń i pięści mogą trafić kilku przeciwników podczas jednego ciosu, każdego najwyżej raz.
11. Cel gardy po trafieniu grupy: przeciwnik najbliższy lub wcześniej atakowany. Przyjęta interpretacja priorytetu do oceny: zachować poprzedni cel, jeśli znalazł się wśród trafionych tym ciosem; inaczej wybrać najbliższego spośród trafionych.
12. Maksymalnie dwóch przeciwników wykonuje jednocześnie atak na gracza.
13. Przeciwnik przygotowuje cios przez 0,4 s, a podczas końcowego zamachu nie śledzi gracza obrotem.
14. Po utracie gracza przeciwnik sprawdza ostatnią widzianą pozycję, a następnie wraca na swoje miejsce.
15. Niezablokowane trafienie przerywa przygotowanie ciosu przeciwnika. Rozpoczęty zamach trwa do końca; śmierć nadal go przerywa.
16. Tylko wybrane typy przeciwników blokują: kierunkowo, ze staminą i możliwością przełamania.
17. Reakcja przeciwnika zależy od typu: lekki jest odpychany, ciężki reaguje bez odepchnięcia. Dotyczy niezablokowanych trafień poza zamachem odpornym na przerwanie.
18. Przyjęta obrona AI: opóźniona reakcja na przygotowanie ataku gracza, ograniczony czas trzymania gardy i krótkie otwarcie na kontrę po jej opuszczeniu. Konkretne czasy poniżej pozostają wartościami startowymi do strojenia.

To są ustalenia. Wszystkie wartości i dodatkowe reguły poniżej to propozycje, jeszcze nie zatwierdzone ani zaprogramowane w grze.

## Proponowane reguły ataku

- Ruch poziomy i zmiana broni zablokowane od rozpoczęcia do zakończenia ciosu. Grawitacja oraz kolizje nadal obowiązują; zatrzymanie nie oznacza zamrożenia postaci w powietrzu.
- Ustalone: przy rozpoczęciu każdego ciosu kierunek wskazuje ostatnio zaatakowanego przeciwnika w zasięgu; gdy cel nie istnieje lub jest poza zasięgiem, używamy poziomego kierunku kamery. Potem kierunek jest zablokowany na czas tego ciosu. Zerowy kierunek docelowy zachowuje obecny obrót. Nie przełączamy celu w trakcie zamachu, także gdy przeciwnik się przemieści lub zginie.
- Propozycja techniczna: osobny parametr Aim Assist Range dla pięści i broni, dobierany do rzeczywistego zasięgu ich trafień po oględzinach prefabów. Zasięg wyboru kierunku nie zwiększa zasięgu obrażeń; nie przesuwa postaci ani nie gwarantuje trafienia. Konkretna wartość i sposób pomiaru zasięgu wymagają dopracowania.
- Broń i pięści mogą trafić wielu przeciwników, każdego najwyżej raz na cios. W kolejnych ciosach combo ten sam wróg może zostać trafiony ponownie. Liczy się faktyczny kontakt podczas okna obrażeń, nie sama obecność w promieniu.
- Skok, blok i zwykłe otrzymanie obrażeń nie przerywają ciosu. Podczas ataku odejmujemy HP, lecz nie uruchamiamy animacji damage przerywającej zamach. Śmierć kończy akcję. Propozycja wyjątku: utrata podłoża przełącza w spadanie i zamyka okno obrażeń.
- Próby ruchu/skoku/zmiany broni podczas ciosu nie są buforowane. Trzymany blok może wejść dopiero po zakończeniu ostatniego ciosu.
- Proponowany priorytet na końcu ciosu: śmierć / spadanie → zaakceptowany następny cios combo → trzymany blok → powrót do ruchu. Nie ma uniku w tym zakresie.

## Proponowane reguły combo

- Ustalony profil: mnożnik obrażeń 1 / 1 / 1 oraz mnożnik nacisku na gardę 1 / 1 / 1 w obrębie danego zestawu ataków. Pięści i broń mogą mieć różne bazowe obrażenia; równość dotyczy kolejnych ciosów zestawu.
- Proponowany punkt startowy: 0,30 s na każdy cios, osobno konfigurowalne dla każdego klipu i rodzaju walki. Ostateczne czasy i szybkości odtwarzania wymagają oględzin animacji w Unity; nie należy obcinać klipu samym timerem bez dopasowania przejść i okien trafienia. Przy takim czasie bufor 0,20 s otwiera się po 0,10 s od startu ciosu.
- Każdy cios ma osobną konfigurację: klip, czas trwania akcji i długość końcowego okna bufora. Okno obrażeń nadal otwierają/zamykają zdarzenia animacji.
- Bufor otwiera się 0,2 s przed końcem akcji (najwcześniej na początku ciosu, gdy cały cios jest krótszy). Przechowuje najwyżej jedno naciśnięcie.
- Naciśnięcie przed otwarciem bufora jest ignorowane. Spam nie kolejkuje kilku ataków. Przytrzymanie nie powtarza ciosów.
- Zaakceptowane naciśnięcie uruchamia kolejny cios po końcu poprzedniego. Może to być nietrafiony cios: nie wymagamy trafienia celu.
- Bez bufora powrót do swobody. Kolejne naciśnięcie przez 0,3 s po zakończeniu daje następny numer combo; później znów pierwszy cios.
- Po trzecim ciosie bezwarunkowy reset do pierwszego; naciśnięcia podczas trzeciego nie kolejkują nowego łańcucha. Nowy łańcuch wymaga naciśnięcia po zakończeniu trzeciego ciosu.
- Zmiana broni, śmierć, wyłączenie kontrolera oraz przerwanie spadaniem zerują combo i bufor. Czasy liczone w czasie gry: pauza ich nie zużywa.
- W projekcie są dwa pliki ciosów pięścią i kilka animacji broni. Wybór trzeciego klipu oraz dopasowanie Animatora są częścią następnej implementacji; sama obecność plików nie potwierdza ich przydatności. Nie obiecujemy gotowego trzeciego ciosu bez oględzin klipów.

## Reakcja na otrzymane trafienie

- Ustalone: poza własnym atakiem zwykłe trafienie wywołuje reakcję 0,25 s z niewielkim odepchnięciem. Własny atak nadal trwa do końca.
- Propozycja odepchnięcia: 0,30 m w ciągu reakcji, poziomo od źródła ciosu, z wygaszaniem prędkości. To maksymalna droga na wolnej przestrzeni, nie teleport: CharacterController.Move respektuje przeszkody, a grawitacja pozostaje aktywna. Przy braku poprawnego kierunku nie stosujemy przesunięcia.
- Propozycja: podczas własnego ataku nie stosujemy ani hitstunu, ani odpychania; odejmujemy HP i pokazujemy efekt trafienia. Zapobiega to przesuwaniu zatrzymanej postaci w trakcie zamachu. Śmierć nadal kończy akcję.
- Propozycja: pełny blok nie powoduje zwykłej reakcji ani odpychania. Częściowo zablokowany cios zmniejsza HP, ale zachowuje gardę bez zwykłego hitstunu; przełamanie uruchamia wyłącznie GuardBroken na 1,2 s. Trafienie poza kątem bloku traktujemy jako niezablokowane i przerywamy gardę reakcją.
- Propozycja: przez 0,25 s reakcji nie można rozpocząć ataku, skoku, bloku ani zmiany broni. Nie kolejkujemy tych wejść. Po końcu trzymany blok może wrócić, jeśli pozwala stamina; atak wymaga świeżego naciśnięcia.
- Propozycja: następne trafienia podczas trwającej reakcji odejmują HP, ale nie odnawiają czasu ani nie sumują odpychania. Nie ma nietykalności. GuardBroken ma pierwszeństwo i nie jest skracany ani przedłużany zwykłym hitstunem.
- Propozycja: odepchnięcie za krawędź przełącza ruch w spadanie; nie odnawiamy podłoża ani nie zatrzymujemy postaci w powietrzu. Śmierć ma najwyższy priorytet.
- Parametry docelowo w Inspectorze: Hit Reaction Duration = 0.25 (ustalone), Hit Push Distance = 0.30 (propozycja), czasy ciosów = 0.30 każdy (propozycja). Żaden z tych parametrów nie został jeszcze dodany do aktywnych skryptów.

## Proponowane reguły staminy i bloku

| Parametr w Inspectorze | Startowa wartość | Znaczenie |
| --- | --- | --- |
| Max Stamina | 100 | Pojemność; start z pełną wartością |
| Stamina Regen Per Second | 20 | Regeneracja na sekundę |
| Stamina Regen Delay | 1,0 s | Od ostatniego zużycia albo puszczenia bloku; późniejszy termin wygrywa |
| Block Angle | 120° | Pełny kąt stożka: po 60° od przodu |
| Block Hold Cost Per Second | 0 | Samo trzymanie nie zużywa staminy, ale blokuje regenerację |
| Melee Damage Reduction | 50% | Redukcja HP przy udanym bloku pięściami |
| Weapon Damage Reduction | 100% | Redukcja HP przy udanym bloku bronią |
| Guard Damage Multiplier | 1 | Koszt staminy = bazowe obrażenia ciosu × mnożnik |
| Guard Break Duration | 1,2 s | Czas bez ataku i bloku po przełamaniu |
| Minimum Stamina To Block | 20 | Próg ponownego rozpoczęcia bloku |
| Block Move Speed Multiplier | 0,4 | Mnożnik zwykłego chodu; sprint podczas bloku niedostępny |

- Stamina dotyczy na razie wyłącznie obrony: nie zmieniamy kosztu biegu, skoku ani ataku.
- Podczas bloku ruch poziomy wynosi 40% prędkości zwykłego chodu, bez sprintu. Obrót śledzi przeciwnika w poziomie, a nie kierunek kamery ani ruchu. Można przemieszczać się bokiem i wycofywać, utrzymując gardę do celu. Blok działa tylko na ziemi. Cel to ostatni przeciwnik zaatakowany przez gracza; szczegóły rejestracji i utraty celu poniżej.

### Cel gardy — ustalenie i proponowane przypadki brzegowe

- Ustalone: ostatni przeciwnik zaatakowany przez gracza, bez automatycznego wybierania najbliższego.
- Proponowana definicja techniczna „zaatakowany”: przeciwnik, na którym system trafień potwierdził kontakt podczas aktywnego okna ataku. Sam zamach w powietrze nie zmienia celu. Kontakt z obroną przeciwnika również powinien liczyć się jako zaatakowanie, jeśli AI otrzyma blok w przyszłości. Ta definicja wymaga oceny użytkownika; nie wynika jednoznacznie z samego wyboru celu.
- Rejestracja na poziomie komponentu Enemy, nie pojedynczego collidera. Użytkownik wybrał cel najbliższy lub wcześniej atakowany. Interpretacja do oceny: na początku ciosu zapamiętujemy poprzedni cel; jeśli został trafiony tym ciosem i żyje, zachowuje priorytet. W przeciwnym razie wybieramy najbliższego żywego i aktywnego przeciwnika spośród rzeczywiście trafionych tym ciosem. Nie wybieramy pobliskiego wroga, którego zamach nie dosięgnął.
- Proponowany pomiar bliskości: pozioma odległość od gracza w momencie pierwszego kontaktu z danym przeciwnikiem. Zbieramy kandydatów przez cały cios; ostateczny cel zatwierdzamy przed wyborem kierunku następnego ciosu combo. Dotychczasowy cel ma stały priorytet, niezależny od kolejności sprawdzania colliderów. Przy identycznych odległościach rozstrzygamy stałym identyfikatorem celu w danym uruchomieniu.
- Pusty zamach zachowuje poprzedni cel, o ile jest nadal poprawny. Gdy wszystkie trafione cele zginęły, nie wybieramy martwego celu ani automatycznie nie namierzamy nietrafionego przeciwnika. Referencję do martwego/dezaktywowanego celu czyścimy.
- Propozycja: bez wcześniejszego trafienia blok zachowuje bieżący kierunek postaci. Nadal działa kąt obrony i ruch 40%; kamera i ruch nie obracają gardy.
- Propozycja: po śmierci, dezaktywacji lub zniszczeniu celu postać zachowuje ostatni kierunek gardy, bez przełączania na innego wroga. Odnowienie życia gracza czyści cel.
- Propozycja: cel zachowujemy między kolejnymi wejściami w blok i aktualizujemy przy następnym trafieniu. Nie dodajemy ograniczenia zasięgu namierzania w tym etapie; blok nie powoduje ruchu ani pościgu w stronę celu.
- Przy zerowej odległości poziomej do celu zachowujemy obrót, zamiast wywoływać LookRotation dla zerowego wektora. Obrót jest płynny, ze współczynnikiem konfigurowanym w Inspectorze; kierunkowość bloku jest sprawdzana względem faktycznego obrotu postaci w chwili trafienia.
- Koszt staminy liczony jest od obrażeń PRZED redukcją HP, tylko raz na cel i zamach. Bronią nie można blokować bez końca dlatego, że końcowe obrażenia wynoszą zero.
- Trafienie od tyłu/poza stożkiem zadaje pełne obrażenia i nie zabiera staminy obrony. Pełny blok nie wywołuje animacji damage.
- Jeśli aktualna stamina jest większa od kosztu, blok działa i odejmujemy koszt. Jeśli koszt jest równy lub większy od staminy, ten cios NADAL podlega normalnej redukcji bloku. Następnie ustawiamy staminę na zero i uruchamiamy GuardBroken na 1,2 s. Przykład: przy 10 staminy i ciosie 30 obrażeń broń redukująca 100% przepuszcza 0 HP, pięści redukujące 50% przepuszczają 15 HP; w obu przypadkach po trafieniu następuje przełamanie. Wartości redukcji pozostają propozycją balansu.
- Podczas GuardBroken nie ma regeneracji, ataku ani bloku; proponujemy również zatrzymanie ruchu. Kolejne trafienia zadają HP, ale nie wydłużają automatycznie tego stanu. Śmierć i spadanie mają pierwszeństwo; koniec ogłuszenia nie może wyczyścić stanu spadania.
- Po przełamaniu potrzebne puszczenie przycisku bloku i osiągnięcie progu 20. Samo trzymanie przycisku nie wznawia obrony. Po upływie GuardBreak regeneracja rusza, gdy spełniony jest także RegenDelay.
- Zwykłe puszczenie bloku nie zeruje staminy ani combo. Wyłączenie komponentu nie odnawia staminy; odnowienie następuje przy nowym życiu/respawnie.
- Potrzebny osobny pasek staminy oraz czytelny efekt przełamania; reakcja animowana wymaga wybrania klipu i przejścia Animatora. Brak klipu nie może blokować końca logicznego ogłuszenia.

## Zmiany techniczne względem v1

- Nowe komponenty/logika: PlayerStamina, PlayerGuardBrokenState i wejście fabryki stanu przełamania.
- Dodatkowo PlayerHitReactionState i kontrolowane przesunięcie CharacterController dla zwykłych trafień. Rozstrzygnięcie obrażeń musi rozróżniać: pełny blok, częściowy blok, przełamanie, trafienie podczas ataku, zwykłą reakcję i śmierć; samo ustawienie triggera damage nie wystarczy.
- Oba zestawy ataków potrzebują wielocelowego wykrywania kontaktów obejmującego objętość i drogę broni/pięści między klatkami oraz zbioru trafionych Enemy na cios. Pojedynczy Raycast z v1 nie realizuje tego wymagania. Ściany i inne przeszkody muszą ograniczać trafienia; kontakt nie może być zastąpiony zadawaniem obrażeń wszystkim w promieniu.
- Blok wymaga referencji do ostatniego zaatakowanego Enemy i osobnego sterowania obrotem. Zwykłe handleRotation nie może nadpisywać obrotu podczas bloku; ruch i orientacja są liczone oddzielnie. Propozycja: ruch nadal względem kamery, orientacja względem przeciwnika. DamageDealer przekazuje potwierdzony kontakt do kontrolera właściciela broni; referencję właściciela trzeba podać jawnie przy wyposażaniu broni, ponieważ broń jest tworzona dynamicznie.
- Trafienie rozliczamy w kolejności: stan obrony i kąt → redukcja HP dla aktualnego ciosu → koszt staminy → przełamanie, o ile postać przeżyła. Następne trafienia po przełamaniu nie korzystają z redukcji bloku, także w tej samej klatce.
- PlayerStateMachine rejestruje zbocza naciśnięcia ataku do pojedynczego bufora zamiast wykorzystywać sam bool IsAttack1Pressed. Ataki mają konfigurację dla indeksów 1–3 i osobne ustawienia dla pięści/broni.
- Rozstrzygnięcie trafienia zwraca wynik: przyjęte / zablokowane / przełamanie / martwy cel. Na tej podstawie dobieramy HP, staminę, animację i VFX; nie wyprowadzamy wyniku wyłącznie z wartości bool.
- HealthSystem nie uruchamia automatycznie damage, gdy bieżący atak ma trwać do końca. GuardBroken nie jest uruchamiany przez zwykły otrzymany cios.
- Zachować poprawkę cyklu stanów z v1. Kod ataku, combo, bloku i integracji obrażeń z v1 trzeba rozszerzyć przed wdrożeniem v2.

## Przeciwnicy — ustalenia i szczegóły do oceny

Ustalone przez użytkownika: dwóch napastników jednocześnie, 0,4 s przygotowania, brak śledzenia podczas końcowego zamachu, sprawdzenie ostatniej widzianej pozycji i powrót. Poniższe szczegóły są propozycją implementacji i balansu.

### Koordynacja grupy

- Wspólny koordynator przydziela maksymalnie dwa miejsca napastników na atakowanego gracza, również gdy przeciwnicy pochodzą z różnych grup/spawnerów. Sam niezależny cooldown każdego Enemy nie zapewni tego limitu.
- Miejsce zajmowane od początku przygotowania przez zamach i odpoczynek po ciosie. Proponowany odpoczynek 0,6 s. Pozostali nie uruchamiają okien obrażeń, ale mogą szukać pozycji i się przemieszczać.
- Przydział wśród gotowych i będących w zasięgu: najdłużej oczekujący, potem dystans. Bez rezerwowania miejsca przez wroga, który dopiero musi dobiec. Czas gotowości zeruje się po ataku lub utracie warunków do ataku.
- Miejsce zwalniane dokładnie raz po odpoczynku, śmierci, dezaktywacji, przerwaniu lub utracie celu przed zamachem. Brak celu i zniszczenie koordynatora nie mogą zostawić otwartego okna obrażeń.
- Limit dwóch nie oznacza wymuszonego rozłożenia trafień w czasie: obaj mogą trafić niemal równocześnie. Reguły obrażeń gracza nadal obowiązują.
- Oczekujący zajmują osiągalne miejsca wokół gracza, nie identyczny punkt jego pozycji. Odstępy, promień oczekiwania i częstotliwość przeliczania pozycji konfigurowane; ściany i NavMesh mogą ograniczyć dostępne miejsca.

### Przebieg ciosu AI

- Przygotowanie 0,4 s: agent zatrzymuje ruch, przeciwnik może obracać się do gracza; animacja czytelnie zapowiada atak. Po końcu przygotowania zapisujemy kierunek i rozpoczynamy zamach bez dalszego naprowadzania.
- Ucieczka poza zasięg podczas przygotowania nie wydłuża ciosu ani nie przyciąga gracza. Propozycja: jeśli cel nadal istnieje, przeciwnik kończy zamach, który może chybić. Śmierć celu kończy akcję i zamyka okno obrażeń.
- Obrażenia wyłącznie przy kontakcie w aktywnym oknie animacji, raz na cel na cios. Koniec/przerwanie akcji zawsze zamyka to okno, nawet jeśli pominięto Animation Event.
- Odpoczynek 0,6 s po trafieniu i po pudle; brak natychmiastowego ponownego ataku w tej samej klatce. Sam czas trwania zamachu wymaga dopasowania do klipu.
- Ustalone: niezablokowane trafienie w AI przerywa przygotowanie, ale nie aktywny zamach; śmierć przerywa wszystko. Propozycja: podczas odpoczynku trafienie także uruchamia reakcję, a przerwanie zwalnia miejsce napastnika i zamyka okno obrażeń. Podczas aktywnego zamachu odejmujemy HP i pokazujemy VFX bez hitstunu oraz odpychania.
- Nawigacja i kod obrotu nie mogą jednocześnie obracać postaci: podczas przygotowania/zamachu steruje logika walki, z przywróceniem agent.updateRotation po akcji.

### Wykrywanie i utrata gracza

- Propozycja: wykrycie wymaga dystansu aggro oraz widoczności bez przeszkód. Ostatnią widzianą pozycję aktualizujemy tylko przy potwierdzonej widoczności, nie przez odczytywanie ukrytej pozycji gracza za ścianą.
- Proponowane wartości początkowe: Detection Range 6 m (zgodne z obecnym aggroRange), Lose Target Range 9 m, Lost Sight Delay 1 s, Search Duration 2 s. To wartości niezatwierdzone; większy promień utraty niż wykrycia zapobiega migotaniu stanów na granicy.
- Po utracie celu zamykamy możliwość nowego ataku, idziemy do ostatniej widzianej osiągalnej pozycji, sprawdzamy ją przez 2 s i wracamy do pozycji startowej zapisanej przy spawnie. Rozpoczęty zamach może się dokończyć bez śledzenia celu.
- Ponowne zobaczenie żywego gracza podczas sprawdzania lub powrotu wznawia pościg; nowe miejsce napastnika wymaga ponownego przydziału.
- Gdy ścieżka jest niepełna/niedostępna lub agent utknie, nie czekamy bez końca: kończymy poszukiwanie po konfigurowanym limicie. Brak osiągalnej drogi powrotu oznacza zatrzymanie w ostatniej poprawnej pozycji, bez teleportowania przez przeszkody.
- Po śmierci gracza przeciwnicy kończą walkę i wracają; nie utrzymują celu ragdolla. Granica pościgu od punktu startowego pozostaje do ustalenia, jeśli potrzebne będzie zapobieganie wyciąganiu AI przez całą mapę.

### Zakres kodu AI do przygotowania

Zastąpienie niezależnych warunków Enemy.Update jawnymi fazami: Idle, Chase, WaitForAttack, Windup, Swing, Recovery, HitReaction, Block, GuardBroken, Investigate, Return i Dead. Osobny EnemyAttackCoordinator odpowiada wyłącznie za przydział miejsc, bez zarządzania zdrowiem i animacją. Zmiany pozostają projektem — nie wdrożono nowych klas ani prefabów.

### Typy przeciwników i obrona

- Ustalone: lekki wróg jest odpychany po niezablokowanym trafieniu, ciężki nie. Brak odepchnięcia nie oznacza odporności ciężkiego na przerwanie przygotowania ani na przełamanie gardy.
- Propozycja startowa: czas reakcji obu typów 0,25 s; odpychanie lekkiego 0,30 m, ciężkiego 0 m. Te liczby dla AI nie zostały osobno zatwierdzone. Przesunięcie lekkiego respektuje kolizje; podczas reakcji sterowanie NavMeshAgent nie może nadpisywać odpychania. Po reakcji agent synchronizuje pozycję i wznawia nawigację tylko na poprawnym NavMesh, bez teleportu przez przeszkody.
- Propozycja: kolejne trafienia podczas reakcji zadają HP, lecz nie odnawiają czasu ani nie kumulują przesunięcia. Brak nietykalności; zwykły hitstun nie skraca ani nie przedłuża GuardBroken.
- Zdolność do blokowania jest osobnym ustawieniem Can Block, niezależnym od masy/odpychania. Możliwy jest lekki wróg blokujący i ciężki wróg bez bloku. Nie włączamy bloku automatycznie wszystkim ciężkim.
- Ustalone: blok wybranych typów jest kierunkowy, ma staminę i może zostać przełamany. Propozycja symetrii z graczem: cios przełamujący korzysta jeszcze z redukcji gardy, potem następuje 1,2 s GuardBroken. Konkretne parametry staminy, kąta i redukcji konfigurowane per typ; nie kopiujemy bezwarunkowo balansu gracza.
- Propozycja: pełny i częściowy blok nie uruchamiają zwykłej reakcji na trafienie; trafienie z boku/tyłu poza kątem gardy zadaje pełne obrażenia i wywołuje właściwą reakcję danego typu. Podczas zamachu AI nie blokuje i nie przełącza się w gardę po nadejściu obrażeń.
- Przyjęte: AI reaguje na przygotowanie ataku gracza z opóźnieniem, trzyma gardę przez ograniczony czas i odsłania się po jej opuszczeniu. Nie może wstecznie aktywować bloku w chwili wykrycia trafienia. Stan bloku i orientacja muszą istnieć przed rozliczeniem kontaktu.
- Propozycja: blokujący przeciwnik nie zajmuje miejsca napastnika, dopóki nie zacznie przygotowania ataku. Po przełamaniu nie wolno mu natychmiast atakować; wraca do wyboru zachowania dopiero po końcu ogłuszenia i ponownie ubiega się o miejsce napastnika.
- W Inspectorze/profilu typu: Can Block, Hit Reaction Duration, Hit Push Distance, Max Stamina, Stamina Regen, Block Angle, Damage Reduction, Guard Break Duration. Konkretne przypisanie profili do prefabów wymaga przeglądu przeciwników w scenie.

### Decyzja o bloku AI — przyjęta zasada i parametry startowe

- Sygnał do rozważenia obrony to rozpoczęcie ciosu gracza widoczne dla przeciwnika w skonfigurowanym zasięgu reakcji. Nie wykrywamy dopiero nadchodzącego wyniku obrażeń. Każdy cios combo może dostarczyć nowy sygnał, ale nie omija blokady ponownego wejścia w gardę.
- Startowe parametry do regulacji per typ: Block Reaction Delay 0,15 s; Max Block Hold Duration 0,60 s; Block Opening Duration 0,35 s. To propozycje do sprawdzenia, a nie zatwierdzone czasy ani zmierzone działanie gry. Faktyczna możliwość obrony zależy od chwili kontaktu klipu, nie samego całkowitego czasu ciosu.
- AI podejmuje reakcję tylko, gdy typ ma Can Block, wystarczającą staminę, widoczność i poprawny zasięg, a jego stan pozwala na obronę. Nie anuluje własnego przygotowania/zamachu ataku, hitstunu, GuardBroken ani otwarcia po gardzie. Samo oczekiwanie na możliwość bloku nie zatrzymuje już rozpoczętego trafienia.
- Po upływie opóźnienia ponownie sprawdzamy warunki. Śmierć lub utrata widoczności/celu anuluje oczekującą decyzję. Nie przenosimy starego opóźnionego żądania na innego gracza ani kolejną akcję.
- Na początek nie dokładamy losowej szansy bloku: spełnione warunki pozwalają wejść w obronę po opóźnieniu. Zróżnicowanie typów opiera się na konfiguracji, a nie nieopisanym losowaniu.
- Nowe sygnały ataku nie przedłużają bieżącego limitu 0,60 s gardy. Po normalnym opuszczeniu następuje 0,35 s otwarcia bez ataku i ponownego bloku; proponujemy możliwość wycofania się. Nie przyznajemy automatycznej premii do obrażeń kontry.
- Przełamanie zastępuje zwykłe otwarcie stanem GuardBroken: nie sumujemy automatycznie 1,2 s oraz 0,35 s. Trafienie podczas otwarcia może wywołać zwykłą reakcję właściwą typowi; jej koniec nie skraca pozostałego czasu otwarcia.
- Po otwarciu AI ponownie ocenia sytuację. Stary sygnał ataku nie jest przechowywany do natychmiastowego wejścia w blok; wymagany jest nowy sygnał. Wejście w atak nadal wymaga wolnego miejsca napastnika.
- Zakres implementacji: sygnał rozpoczęcia ataku gracza z identyfikatorem ciosu, lokalne terminy reakcji i otwarcia w AI oraz unieważnianie decyzji przy zmianie stanu. Wymaga dopasowania animacji gardy i czytelnego jej opuszczenia.

### Przypadki odbioru AI

Trzech i więcej przeciwników przy graczu; dwie grupy na jednym graczu; śmierć/dezaktywacja napastnika z zajętym miejscem; kolejka bez głodzenia oczekujących; atak po pudle; wyjście z zasięgu podczas 0,4 s przygotowania; brak obrotu podczas zamachu; przeszkoda zasłaniająca gracza; ponowne wykrycie podczas powrotu; niedostępna ostatnia pozycja; utrata ścieżki; śmierć gracza w trakcie przygotowania i zamachu. To przyszłe testy, nie wyniki.

Obrona AI: kontakt przed upływem reakcji bez wstecznego bloku; anulowanie reakcji po utracie celu; brak przedłużania gardy sygnałami combo; brak ponownego bloku w otwarciu; przełamanie bez dodawania zwykłego otwarcia; brak reakcji przez ściany; hitstun nie skraca terminu otwarcia; typ bez Can Block nigdy nie uruchamia gardy.

## Dodatkowe przypadki odbioru

Bufor dokładnie na początku/końcu okna; kilka kliknięć w jednej klatce; trzeci cios; trzymanie ataku; puszczenie i ponowne naciśnięcie; graniczna stamina równa kosztowi; blok od tyłu; kilka trafień w jednej klatce; trzymany blok po przełamaniu; pauza; śmierć podczas combo i ogłuszenia; spadanie podczas przełamania; ponowne włączenie bez odnowienia staminy. To lista przyszłych testów, nie wyniki.

Dla reakcji dodatkowo: odpychanie przy ścianie i krawędzi; zerowy kierunek źródła; trafienie podczas własnego ataku; kolejne trafienia podczas hitstunu; częściowy blok bez utraty gardy; brak skrócenia 1,2 s przełamania przez reakcję 0,25 s; równe obrażenia i nacisk na gardę wszystkich trzech ciosów.
