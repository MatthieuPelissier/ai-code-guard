# Instructions pour les agents de code (Claude Code, Codex…)

Ce fichier décrit les règles que tout agent IA doit respecter dans ce dépôt.
`CLAUDE.md` renvoie ici pour éviter de maintenir deux versions.

## Architecture

- `src/AiCodeGuard.Core` : moteur de règles, **aucune dépendance externe**. Toute nouvelle règle implémente `IAnalysisRule` (ou hérite de `LineRegexRule`) et est enregistrée dans `DefaultRules.CreateAll()`.
- `src/AiCodeGuard.Web` : ASP.NET Core 8 MVC + EF Core SQLite. Les contrôleurs restent fins ; la logique va dans `Services/`.
- `src/AiCodeGuard.Cli` : outil console pour la CI.
- `tests/AiCodeGuard.Tests` : xUnit.

## Règles

1. Toujours exécuter `dotnet build` puis `dotnet test` avant de considérer une tâche terminée.
2. Toute nouvelle règle doit avoir **au moins un test positif et un test de non-faux-positif**.
3. Identifiants de règle : `AICG` + 3 chiffres, sans réutiliser un identifiant existant.
4. Ne pas ajouter de package NuGet sans le signaler explicitement dans le résumé.
5. Ne jamais écrire de secret, de clé d'API ou de chaîne de connexion réelle ; utiliser `dotnet user-secrets`.
6. Textes visibles par l'utilisateur et documentation : en français.
7. Ne pas modifier `.github/workflows/` sans le mentionner.
8. Ne pas tronquer le code (`// ...`) : livrer des fichiers complets.

## Commandes

```bash
dotnet build
dotnet test
dotnet run --project src/AiCodeGuard.Web
dotnet run --project src/AiCodeGuard.Cli -- samples/ai-generated
```
