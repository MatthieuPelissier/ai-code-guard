# Charte d'utilisation de l'IA dans le développement logiciel

> Document de cadrage proposé pour une équipe de développement .NET.
> Objectif : profiter des gains de productivité de l'IA générative **sans dégrader la qualité, la sécurité ni la maintenabilité** du produit.

---

## 1. Principe fondamental

**L'IA propose, le développeur dispose.**
Un code généré par IA est un code écrit par un développeur junior très rapide, très sûr de lui et parfois faux. Le développeur qui le commite en est **l'auteur et le responsable**, exactement comme s'il l'avait tapé lui-même.

## 2. Niveaux d'usage

| Niveau | Exemple | Contrôle exigé |
|---|---|---|
| 1 — Assistance | Explication d'un code existant, complétion d'une ligne, aide à l'écriture d'une requête | Relecture habituelle |
| 2 — Génération | Écriture d'une méthode, d'une classe, de tests unitaires | Relecture ligne à ligne + AI Code Guard + tests |
| 3 — Agent | Claude Code / Codex modifiant plusieurs fichiers | Niveau 2 + revue par un pair + périmètre défini à l'avance |

## 3. Ce qui est autorisé / interdit

### ✅ Autorisé
- Générer du code, des tests, de la documentation, des scripts SQL de lecture.
- Faire expliquer un code legacy ou une stack trace (après anonymisation).
- Demander une revue critique d'un code que l'on a écrit.
- Prototyper plusieurs approches pour comparer.

### ⛔ Interdit
- Envoyer à un outil non validé par l'entreprise : **données clients, données personnelles (RGPD), secrets, chaînes de connexion, code source propriétaire du socle**.
- Commiter du code que l'on ne comprend pas ou que l'on ne saurait pas expliquer en revue.
- Ajouter une dépendance NuGet proposée par l'IA sans vérifier qu'elle existe, qu'elle est maintenue et sous licence compatible.
- Laisser l'IA modifier la configuration de production, les scripts de migration ou les droits d'accès sans validation humaine explicite.

## 4. Règles de validation (Definition of Done « IA »)

Tout code généré à plus de quelques lignes doit, avant fusion :

1. être **relu ligne à ligne** par son auteur ;
2. passer **AI Code Guard** avec un score ≥ 80 et **aucun problème critique** ;
3. être **couvert par des tests unitaires** (niveau 1) qui passent ;
4. avoir ses **cas limites vérifiés** : null, collections vides, valeurs extrêmes, concurrence, erreurs réseau ;
5. être **tracé** : case « Utilisation de l'IA » cochée dans la Pull Request (cf. modèle de PR).

## 5. Erreurs typiques des LLM à surveiller

| Catégorie | Symptôme | Règle associée |
|---|---|---|
| Hallucination | Méthode, package ou paramètre qui n'existe pas | AICG008 |
| Troncature | `// ... reste du code`, méthodes vides, `NotImplementedException` | AICG006 |
| Sécurité | SQL interpolé, secrets en dur, MD5 | AICG001, AICG002, AICG009 |
| Robustesse | `catch {}` vide, `catch (Exception)` partout | AICG003, AICG004 |
| Asynchronisme | `async void`, `.Result`, `.Wait()` | AICG005, AICG007 |
| Obsolescence | API dépréciées (.NET Framework) proposées en .NET 8 | revue humaine |
| Sur-ingénierie | Abstractions inutiles, patterns plaqués | revue humaine |
| Logique | Code plausible qui ne répond pas au besoin réel | tests + revue humaine |

Les catégories sans règle automatique restent sous la responsabilité de la revue humaine : **l'outil réduit le risque, il ne le supprime pas.**

## 6. Bonnes pratiques de prompt

- Donner le **contexte** : version (.NET 8), conventions de l'équipe, contraintes (pas de nouvelle dépendance, async obligatoire…).
- Demander **un petit périmètre** à la fois plutôt qu'un module complet.
- Exiger les **tests unitaires** en même temps que le code.
- Demander à l'IA de **lister ses hypothèses et ses incertitudes**.
- Pour les agents (Claude Code, Codex) : maintenir un fichier `AGENTS.md` / `CLAUDE.md` décrivant l'architecture et les règles du dépôt.

Voir [BIBLIOTHEQUE-PROMPTS.md](BIBLIOTHEQUE-PROMPTS.md).

## 7. Amélioration continue

- Chaque erreur d'IA passée en production ou détectée en revue est **analysée** (cause, prompt utilisé, pourquoi non détectée).
- Si l'erreur est détectable automatiquement, elle devient une **nouvelle règle** AI Code Guard, avec ses tests.
- La charte est revue **chaque trimestre** avec l'équipe.
