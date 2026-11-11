# Bibliothèque de prompts pour développeurs .NET

Prompts testés et réutilisables. Remplacer les éléments entre `<chevrons>`.
Règle d'or : **ne jamais coller de données clients, de secrets ou de chaînes de connexion.**

---

## 1. Générer une méthode

```text
Contexte : application ASP.NET Core 8 MVC, C# 12, injection de dépendances, ILogger<T>.
Conventions : async/await de bout en bout, aucune nouvelle dépendance NuGet,
requêtes SQL paramétrées uniquement, exceptions spécifiques (pas de catch générique).

Tâche : écris la méthode <signature> dans la classe <classe> qui <comportement attendu>.

Contraintes :
- gère les cas limites suivants : <null, liste vide, ...>
- ne tronque pas le code : donne l'implémentation complète
- liste à la fin tes hypothèses et ce dont tu n'es pas sûr
```

## 2. Générer des tests unitaires

```text
Écris des tests xUnit pour la méthode ci-dessous.
- Un test par comportement, nommés Methode_Scenario_ResultatAttendu
- Couvre : cas nominal, cas limites, cas d'erreur
- Utilise [Theory] + [InlineData] quand c'est pertinent
- N'invente aucune API : utilise uniquement ce qui est visible dans le code fourni

<code>
```

## 3. Expliquer un code legacy

```text
Explique ce code à un développeur qui découvre le projet :
1. ce qu'il fait, en 3 phrases
2. son flux pas à pas
3. les risques ou dettes techniques que tu repères
4. ce que tu ne peux pas déduire sans voir le reste du code
Ne propose pas encore de refactorisation.

<code>
```

## 4. Analyser un ticket de maintenance (niveau 3)

```text
Voici une stack trace anonymisée et le code concerné.
1. Formule 2 ou 3 hypothèses de cause, classées par probabilité
2. Pour chacune, indique comment la confirmer (log, point d'arrêt, requête)
3. Propose un test unitaire qui reproduit le problème
Ne propose de correctif qu'une fois la cause la plus probable identifiée.

<stack trace>
<code>
```

## 5. Revue critique

```text
Tu es relecteur senior C#/.NET. Relis ce code comme si tu devais refuser la PR.
Cherche en priorité : erreurs de logique, cas limites, sécurité (OWASP), performances,
API obsolètes, asynchronisme mal géré. Pour chaque point : ligne, problème, correction.
Si le code est correct, dis-le simplement.

<code>
```

## 6. Refactorisation encadrée

```text
Refactorise ce code pour <objectif : lisibilité / testabilité / suppression de duplication>.
Contraintes : comportement strictement identique, signatures publiques inchangées,
pas de nouvelle dépendance. Explique chaque changement en une ligne.

<code>
```

---

### Pour les agents (Claude Code, Codex)

Les agents lisent un fichier d'instructions à la racine du dépôt (`AGENTS.md`, `CLAUDE.md`).
Voir l'exemple de ce dépôt : [../AGENTS.md](../AGENTS.md).
