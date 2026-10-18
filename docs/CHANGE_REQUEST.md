# Változtatási igény

> **Állapot: még nem érkezett meg.** Az oktató az első működő verzió (2. mérföldkő) után adja ki.
> A kiindulópont a `v1.0` git tag. A végrehajtás menete a [`change-request` skillben](../.claude/skills/change-request/SKILL.md) van leírva.

## Az igény
_(az oktató szövege szó szerint)_

## Hatáselemzés
| Elfogadási feltétel | Változik? | Hogyan |
|---|---|---|
| AC1 … AC8 | | |

Érintett kódrészek:
- …

## Döntés és indoklás
- Választott megoldás:
- Elvetett alternatívák és indokuk:

## Változások
`git diff --stat v1.0..HEAD` kimenete:
```
```

## Ellenőrzés
| Ellenőrzés | Teszt / lépés | Eredmény |
|---|---|---|
| Új szabály, normál eset | | |
| Új szabály, határeset | | |
| Új szabály, hibás bemenet | | |
| Az új teszt a régi kódon elbukik | `docs/test-results/cr-red.txt` | |
| Teljes regresszió (backend, frontend, E2E) | `docs/test-results/*` | |
| Tárolt adatok megmaradnak a migráció után | | |
