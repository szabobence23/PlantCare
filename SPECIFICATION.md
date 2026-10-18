# PlantCare – specifikáció (v1.0)

Szabó Bence – TE6YK0

## Cél
Webes növénygondozó alkalmazás: a felhasználó nyilvántartja szobanövényeit, látja a locsolási/tápozási teendőket, és képekkel követi a növények fejlődését. Frontend: React + TypeScript; backend: C# ASP.NET Core Web API; adatbázis: PostgreSQL.

## Szerepkör
**Regisztrált felhasználó:** regisztrál, bejelentkezik, növényt hoz létre és töröl, képet tölt fel, gondozási eseményt rögzít. Csak a saját növényeit látja; más növényére hivatkozva „nem található” választ kap.

## Fő forgatókönyv
Regisztráció és bejelentkezés után új növényt ad meg (név, faj, locsolási és tápozási gyakoriság napban, legutóbbi locsolás és tápozás dátuma, opcionálisan fénykép). A képből a Pl@ntNet API fajt javasol, amit a felhasználó elfogad vagy átír. A listán látja, mi esedékes vagy közeleg; a „Meglocsoltam” / „Megtápoztam” gomb rögzíti az eseményt és újraszámolja az esedékességet. Később további képeket tölt fel, és visszanézi őket.

## Elfogadási feltételek
1. **AC1 Regisztráció és bejelentkezés:** érvényes e-mail és min. 8 karakteres jelszó → fiók létrejön, a felhasználó bejelentkezik. Hibás e-mail, rövid jelszó, foglalt e-mail vagy rossz jelszó → mezőnkénti vagy általános hibaüzenet, fiók/belépés nincs.
2. **AC2 Növény létrehozása:** bejelentkezve nem üres névvel és pozitív gyakoriságokkal a növény mentődik, és megjelenik a saját listában (másik felhasználóéban nem).
3. **AC3 Esedékesség:** következő esedékesség = legutóbbi dátum + gyakoriság. Ha ez a mai nap → „Esedékes”; korábbi → „Lejárt”; 1–2 napon belül → „Közeledik”; később → „Rendben”.
4. **AC4 Gondozási esemény:** „Meglocsoltam” / „Megtápoztam” → a mai dátummal esemény mentődik, a következő esedékesség ma + gyakoriság lesz, az esemény megjelenik az előzményekben. A másik teendő nem változik.
5. **AC5 Hibás gyakoriság:** 0 vagy negatív (vagy hiányzó, nem szám) gyakoriság → a növény nem mentődik, a mező mellett hibaüzenet jelenik meg.
6. **AC6 Növényfelismerés:** érvényes JPEG/PNG kép kiválasztásakor az API-javaslat (faj, közönséges név, valószínűség) megjelenik; a faj mező csak „Elfogadom” után töltődik ki, és szerkeszthető marad. Nem kép fájl → hibaüzenet, API-hívás nincs. Kép nélkül vagy elérhetetlen API esetén a faj kézzel adható meg.
7. **AC7 Növekedés követése:** a létrehozáskor és később feltöltött képek mentődnek, és a növény oldalán időrendben visszanézhetők.
8. **AC8 Törlés:** megerősítés után a növény eltűnik a listából, és az adatbázisból is törlődik minden eseményével és képével együtt.

## Bizonytalanságok és választott értelmezés
- **„Közeledik”**: 1–2 napon belüli esedékesség. **Lejárt** teendő külön státuszt kap (a spec csak a mai napot nevezi meg).
- **„Mai nap”**: Europe/Budapest szerinti naptári nap, nem UTC.
- **Faj**: nem kötelező (kép nélkül „manuálisan adható meg”).
- **Legutóbbi dátumok**: kötelezők, nem lehetnek a jövőben.
- **Gyakoriság felső korlát**: 365 nap (a „pozitív” mellé, a túlcsordulás ellen).
- **Esemény dátuma**: mindig a rögzítés napja; egy napon többször is rögzíthető.
- **Létrehozáskori kép**: az első fejlődési képként is mentődik.
- **Képformátum**: JPEG/PNG, max. 10 MB (a Pl@ntNet ezeket fogadja).
- **Képek tárolása**: PostgreSQL-ben, így a törlés egy tranzakcióban viszi az összes adatot.

## Változásnapló
- v1.0 – a beadott specifikáció számozott, mérhető feltételekkel és a fenti értelmezésekkel.
