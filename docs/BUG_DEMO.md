# Hibás és javított változat: a teszt elbukik, majd átmegy

> **Ez szándékosan hibássá tett változat, nem valódi fejlesztési hiba.** A követelmény megengedi: „Ha saját
> munkádban nem találtál megfelelő hibát, használhatsz egy egyértelműen megjelölt, szándékosan hibássá tett
> változatot.” A hibát az MI-ügynök (Claude Code) tette a kódba, kizárólag a bemutatás céljából.

## Az elvárt működés (AC3)
> „A rendszer a legutóbbi locsolás vagy tápozás dátuma és a megadott intervallum alapján kiszámítja a következő
> esedékességet. **Ha a teendő az aktuális napra esik, azt esedékesként jelöli.**”

Példa: 7 naponta locsolt növény, utoljára 7 napja locsolták → a következő locsolás **ma** van → státusz: **Esedékes**.

## A hiba
[`CareSchedule.StatusOf`](../backend/src/PlantCare.Api/Domain/CareSchedule.cs) első feltételében `<` helyett `<=` áll:

```diff
-        if (nextDue < today) return CareStatus.Overdue;
+        if (nextDue <= today) return CareStatus.Overdue;
         if (nextDue == today) return CareStatus.Due;
```

Így a mai napra eső teendőt már az első ág „Lejárt”-nak minősíti, a `Due` ág soha nem fut le. Klasszikus
határeset-hiba (off-by-one): a felhasználó piros „Lejárt” jelzést kap egy olyan teendőre, ami nincs is késésben.

## Megőrzött változatok
| | Hol |
|---|---|
| Hibás változat | `demo/szandekos-hiba` git ág (egyetlen commit a `main` fölött), illetve [`bug-demo/buggy.patch`](bug-demo/buggy.patch) |
| Javított változat | `main` ág |
| Futás a hibás változaton | [`bug-demo/run-buggy.txt`](bug-demo/run-buggy.txt) |
| Futás a javított változaton | [`bug-demo/run-fixed.txt`](bug-demo/run-fixed.txt) |

## Eredmény
`dotnet test backend --filter "AC=AC3"` (13 teszt):

| Változat | Eredmény | Elbukó tesztek |
|---|---|---|
| Hibás | **2 bukott**, 11 sikeres | `CareScheduleTests.Task_falling_on_today_is_due` (Expected: `Due`, Actual: `Overdue`), `PlantsApiTests.List_shows_due_overdue_upcoming_and_ok_tasks_most_urgent_first` (`(2026-10-18, 0, Due)` helyett `Overdue`) |
| Javított | **13/13 sikeres** | – |

Mindkét szinten elkapjuk a hibát: a unit teszt közvetlenül a szabályt, az integrációs teszt a valódi
API-választ és PostgreSQL-t (a növénylistát) ellenőrzi. A többi AC3 teszt (pl. a „tegnap → Lejárt” és a
„2 nap → Közeledik”) a hibás változaton is átmegy, mert a hiba csak a pontos határon jelentkezik. Ezért kell a
határesetet külön teszttel lefedni.

## Reprodukálás
```bash
docker compose up -d            # nem kötelező, a Testcontainers saját DB-t indít; Docker viszont fusson
git switch demo/szandekos-hiba
dotnet test backend --filter "AC=AC3"    # 2 bukott
git switch main
dotnet test backend --filter "AC=AC3"    # 13 sikeres
```
Ág nélkül: `git apply docs/bug-demo/buggy.patch`, majd teszt, végül `git checkout backend/src/PlantCare.Api/Domain/CareSchedule.cs`.
