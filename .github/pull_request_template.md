## Objet de la modification

<!-- Ticket / besoin concerné, en une ou deux phrases. -->

## Utilisation de l'IA

- [ ] Aucune IA utilisée
- [ ] Assistance ponctuelle (complétion, explication)
- [ ] Code généré par IA puis relu et adapté
- [ ] Agent de code (Claude Code, Codex…) sur plusieurs fichiers

Outil(s) et usage : <!-- ex. Claude Code — génération des tests unitaires de RuleEngine -->

## Checklist de validation (charte IA, §4)

- [ ] J'ai relu et je comprends **chaque ligne** générée ; je suis capable de l'expliquer en revue
- [ ] Aucune donnée client, secret ou code propriétaire sensible n'a été envoyé à un outil non autorisé
- [ ] AI Code Guard : score ≥ 80 et aucun problème critique
- [ ] Les dépendances ajoutées existent sur NuGet et sont validées par l'équipe
- [ ] Tests unitaires ajoutés ou mis à jour, et ils passent
- [ ] Cas limites vérifiés (null, liste vide, valeurs négatives, erreurs réseau…)
- [ ] Documentation mise à jour si nécessaire
