# Portail V2 Copilot

Cette suite isole les nouveaux tests smoke du Portail des tests historiques.
La référence visuelle (scénarios, mesures automatiques et chronométrage manuel) est disponible dans
[`doc/index.html`](../../../doc/index.html).

## Structure

Les Page Objects sont partagés avec le reste du Portail et rangés par domaine dans
`../Pages/` :

- `Authentication/ConnexionPage.cs` : interactions avec le formulaire de connexion.
- `Dashboard/AccueilPage.cs` : vérifications de la page après connexion.
- `Account/ProfilPage.cs` : lecture des coordonnées du profil, sans les modifier.

Les scénarios de cette suite sont maintenant rangés avec les autres tests Portail :

- `../Functional/Smoke/ConnexionSmokeTest.cs` : un scénario par fichier de test.
- `../Functional/Smoke/FooterPagesSmokeTest.cs` : contrôle du contenu des liens du footer.
- `../Functional/Smoke/NavigationConnecteeSmokeTest.cs` : parcours en lecture seule après connexion.
- `../Technical/Performance/PerformanceEtNonFonctionnelSmokeTest.cs` : mesures de parcours et contrôle mobile.
- `../Technical/Accessibility/PortailAccessibiliteSmokeTest.cs` : audit axe-core du Portail connecté.
- `../Sync/SynchroniserProfilsCsvTest.cs` : lecture des profils inscrits et synchronisation vers le CSV.

## Smoke actuel

`ConnexionAvecPartenaireInscrit_AfficheLaPageAccueil` sélectionne un partenaire marqué
`InscritSurLePortail=true` dans `data/Partenaires.csv`, se connecte au portail de qualification,
puis vérifie qu'un onglet du tableau de bord est visible. La langue demandée est sélectionnée sur
l'écran de connexion, avant la saisie des identifiants. L'interface après authentification peut
reprendre la langue préférée du compte; la vérification d'accueil reste donc indépendante de cette
préférence. Le scénario est généré en français et en allemand par défaut. Les variables `LNG_FR`
et `LNG_DE` permettent de choisir les langues exécutées. Les libellés de connexion proviennent des
ressources localisées existantes. Le mot de passe est lu via le gestionnaire de secrets existant;
aucun secret ne doit être ajouté au dépôt.

Depuis la racine du dépôt, exécuter le test avec :

```powershell
dotnet test TestPlayrights.csproj --filter "FullyQualifiedName~ConnexionSmokeTest"
```

Le navigateur est visible en local par défaut et headless en CI, comme les autres tests du projet.

## Smoke tests après connexion

`NavigationConnecteeSmokeTest` vérifie, en français et sans modifier les données du portail :

- que « Mon compte » affiche les données client et le numéro du partenaire ;
- que les contrats sont affichés, et qu'au moins un contrat actif présente son détail lorsqu'il est déplié ;
- que la page Consommation ne présente aucune erreur, même si les données de consommation
  sont absentes de l'environnement de qualification ;
- que l'onglet Factures et les comptes de contrat sont accessibles.

Les scénarios utilisent des partenaires inscrits adaptés à chaque parcours et ignorent ceux dont
les fenêtres d'avertissement bloqueraient la navigation. Le smoke Factures n'ouvre, ne télécharge
et ne paie aucune facture. Les scénarios sont en français car les parseurs actuels des pages
Contrats et Consommation attendent les libellés français.

```powershell
dotnet test TestPlayrights.csproj --filter "FullyQualifiedName~NavigationConnecteeSmokeTest"
```

## Pages publiques ouvertes depuis le footer

`FooterPagesSmokeTest` ouvre le Portail de qualification sans se connecter, sélectionne la langue
et suit chaque lien du footer vers Contact, les conditions générales internet et les conditions
générales du groupe. Il vérifie le contenu de ces pages publiques de `www.groupe-e.ch` en français
et en allemand (six scénarios par défaut). Ce test vérifie les liens et contenus externes; il ne
sert pas à auditer l'accessibilité du Portail my-groupe. Il ferme, si présente, la boîte de dialogue
de cookies sans accepter les cookies. Aucune donnée n'est modifiée.
Les variables `LNG_FR` et `LNG_DE` permettent de limiter les langues exécutées.

La page « Conditions générales de Groupe E SA » renvoie actuellement vers les conditions d'achat
du groupe; le test vérifie ce contenu tel qu'il est publié.

## Accessibilité du Portail my-groupe

`PortailAccessibiliteSmokeTest` se connecte au Portail, attend la fin du chargement, puis analyse
le tableau de bord de l'application `my-q.groupe-e.ch` en FR et DE avec axe-core pour les règles
WCAG 2.2 AA. Les pages
`www.groupe-e.ch` ouvertes par les liens de footer ne sont pas incluses dans cet audit.

Lorsqu'une règle échoue, le test produit une capture de la page entière et, pour chaque élément
signalé, une autre capture de la page entière avec le sélecteur indiqué en rouge et l'élément visible
encadré en rouge. Pour les métadonnées non visibles, le sélecteur apparaît dans le repère rouge.
Le rapport indique explicitement l'URL auditée, l'identifiant et la gravité de la règle, le sélecteur
CSS, le HTML observé et les vérifications axe qui ont échoué. Ces informations permettent de retrouver la cible
dans les outils de développement et de vérifier son nom dans le panneau Accessibilité; la capture
montre où elle se trouve avec son contexte visuel.

Les preuves sont jointes au résultat NUnit et au rapport consolidé produit par
`scripts/Run-TestsWithReport.ps1`.

```powershell
dotnet test TestPlayrights.csproj --filter "FullyQualifiedName~PortailAccessibiliteSmokeTest"
```

## Performance et qualité non fonctionnelle

`PerformanceEtNonFonctionnelSmokeTest` chronomètre les quatre parcours connectés, une fois la
connexion terminée, jusqu'à la vérification du contenu attendu. Le budget de 20 secondes est un
seuil d'alerte initial pour QUAL, pas un SLA; les durées mesurées sont écrites dans la sortie du
test. Il est préférable de comparer les tendances sur plusieurs exécutions headless plutôt que
d'interpréter un seul résultat.

Première mesure locale : Mon compte 1,0 s; Produits & contrats 4,7 s; Consommation 1,4 s;
Factures 0,6 s. Ces temps couvrent le parcours après connexion et une seule exécution; ils ne
remplacent pas la moyenne historique de 8,5 s par test complet affichée dans la documentation.

Le contrôle mobile vérifie que le tableau de bord ne déborde pas horizontalement à 390 × 844 et
qu'aucune exception JavaScript non interceptée n'est émise pendant la connexion et l'affichage.
Ces scénarios sont en lecture seule.

Depuis la racine, lancer uniquement cette classe (sans les tests historiques du projet) :

```powershell
dotnet test TestPlayrights.csproj --filter "FullyQualifiedName~PerformanceEtNonFonctionnelSmokeTest"
```

## Synchronisation des profils vers le CSV

`SynchroniserProfilsInscritsVersCsv` parcourt les lignes où `InscritSurLePortail=true`
(136 lignes actuellement), se connecte avec l'e-mail de connexion et lit le nom, le prénom
et les coordonnées exposées dans le profil. Les lignes non inscrites sont ignorées.
Si un compte n'a pas de formulaire de correspondance, le test conserve les coordonnées existantes
et synchronise tout de même les informations disponibles sur le tableau de bord.

Le dépôt contient le CSV `data/Partenaires.csv`, pas de classeur Excel. La colonne `Email` reste
l'identifiant de connexion et n'est jamais remplacée. L'e-mail de correspondance est stocké
séparément dans `EmailCorrespondance`. Si nécessaire, `EmailCorrespondance` et `Mobile` sont
ajoutées à l'en-tête CSV. Les valeurs vides ou absentes du profil ne remplacent pas les données
déjà présentes.

Quand la fenêtre « Vos factures d'électricité par e-mail » apparaît, la ligne est marquée
`AvertissementFactureParMail=True`. La fenêtre n'est ni validée ni modifiée sur le Portail. Les
autres champs de cette ligne ne sont pas lus tant que cette fenêtre bloque la navigation.

Par défaut, le test fonctionne en aperçu et ne modifie aucun fichier. Pour valider le parcours
sur un seul partenaire :

```powershell
$env:PORTAIL_V2_SYNC_LIMIT = "1"
dotnet test TestPlayrights.csproj --filter "FullyQualifiedName~SynchroniserProfilsCsvTest"
Remove-Item Env:\PORTAIL_V2_SYNC_LIMIT
```

Pour tester une ligne CSV précise en aperçu seulement (numérotation à partir de l'en-tête en ligne 1) :

```powershell
$env:PORTAIL_V2_SYNC_ROW = "77"
dotnet test TestPlayrights.csproj --filter "FullyQualifiedName~SynchroniserProfilsCsvTest"
Remove-Item Env:\PORTAIL_V2_SYNC_ROW
```

Pour appliquer la synchronisation à tous les partenaires inscrits, définir explicitement
`PORTAIL_V2_APPLY_SYNC=true` puis lancer le test sans limite. Les profils lus sans erreur sont
écrits; les lignes en erreur restent inchangées. Le test signale tout de même les erreurs à la fin,
et une sauvegarde du CSV original est créée avant toute écriture :

```powershell
$env:PORTAIL_V2_APPLY_SYNC = "true"
dotnet test TestPlayrights.csproj --filter "FullyQualifiedName~SynchroniserProfilsCsvTest"
Remove-Item Env:\PORTAIL_V2_APPLY_SYNC
```

Une copie de sauvegarde du CSV est créée sous `REPORTS/<date>/PortailV2Copilot/Backups`, relatif
au répertoire de travail du processus de test (en local, par exemple sous
`bin/Debug/net8.0/REPORTS`). Ce test utilise la catégorie `PORTAIL_DATA_SYNC`, et n'est donc pas
lancé par le workflow smoke `CI_SMOKE`.

## Vérification des factures téléchargeables

`SynchroniserFacturesTelechargeablesVersCsv` parcourt les comptes inscrits regroupés par partenaire
et compte de contrat. Il vérifie dans QUAL si un bouton de téléchargement de facture est actif.
Le test est `Explicit` et fonctionne en aperçu par défaut : il ne modifie pas le CSV. Le filtre par
ligne permet de valider le parcours sans lancer le scan complet :

```powershell
$env:PORTAIL_FACTURES_SYNC_ROW = "7"
Remove-Item Env:\PORTAIL_FACTURES_APPLY_SYNC -ErrorAction SilentlyContinue
dotnet test TestPlayrights.csproj --filter "FullyQualifiedName~SynchroniserFacturesTelechargeablesCsvTest" -- NUnit.ExplicitMode=Relaxed
Remove-Item Env:\PORTAIL_FACTURES_SYNC_ROW
```

Pour parcourir tous les comptes inscrits en aperçu, ne définir ni `PORTAIL_FACTURES_SYNC_ROW`, ni
`PORTAIL_FACTURES_SYNC_LIMIT`, ni `PORTAIL_FACTURES_APPLY_SYNC`. Le test refuse toute écriture si
un compte n'a pas pu être vérifié. Pour écrire après un aperçu complet sans erreur, définir
explicitement `PORTAIL_FACTURES_APPLY_SYNC=true` et relancer sans limite. Une sauvegarde du CSV
original est créée avant son remplacement.

Dernier scan complet corrigé : 71 comptes de contrat analysés, 8 avec factures PDF actives,
56 sans facture téléchargeable et 7 en erreur (4 consentements aux factures par e-mail,
3 délais d'attente). Quarante-deux lignes auraient changé; le mode aperçu n'a pas modifié le CSV.
Résoudre les 7 erreurs avant toute synchronisation générale. Une vérification ciblée de la ligne
CSV 7 a confirmé deux factures téléchargeables; le scénario fonctionnel FR sur Chrome a téléchargé
les deux PDF et confirmé qu'ils ne sont pas vides (2/2 réussis).
