# TestPlayrights

Projet de tests automatisés Playwright (.NET / NUnit) pour les applications Groupe-E :
**Portail my-q.groupe-e.ch**, **IVY-Qual** et **Nestor**.

Les tests sont organisés en deux familles :
- **Smoke** — vérifications rapides post-déploiement (connexion, navigation, ouverture des composants critiques).
- **Fonctionnels** — scénarios métier complets (création, mutation, consultation de données).

## Socle commun — étape 1 de la reconstruction

Les éléments utilisés par plusieurs sites sont regroupés dans `Tests/00-General` :

- `Models` : modèles de données partagés, comme `Partenaire`.
- `Data` : lecture, recherche, mise à jour et sauvegarde des données de test.
- `Configuration` : accès aux paramètres et aux secrets des tests.
- `Localization` : ressources et textes localisés utilisés par les pages.
- `Reporting` : génération des rapports propres à certains tests.
- `TestTools.cs` et `TestEvidence.cs` : opérations partagées par les tests.

Les fichiers de données et de traduction (`.csv` et `.resx`) restent dans `data/` : ce dossier
contient les ressources utilisées à l'exécution, pas le code qui les manipule. Cette étape est
un déplacement d'organisation sans changement des noms de classes ni des comportements.

## Séparation par site — étape 2 de la reconstruction

Les éléments propres à chaque application sont regroupés sous `Tests/Portail` et `Tests/Ivy`.
Les pages Portail et les tests Copilot sont regroupés sous `Tests/Portail`; les tests historiques
Portail restent dans ce dossier. Les pages IVY sont sous `Tests/Ivy/Pages`.

## Pages par domaine — étape 3 de la reconstruction

Les Page Objects sont classés par domaine fonctionnel dans `Tests/Portail/Pages` :

- `Authentication` : connexion et inscription.
- `Dashboard` : accueil et tableau de bord.
- `Account` : profil et compte utilisateur.
- `Billing` : factures et plan de paiement.
- `Consumption` : données de consommation.
- `Contracts` : produits et contrats.
- `Moving` : déménagement.
- `Public` : pages publiques du Portail.
- `Shared` : parsing et utilitaires propres aux pages Portail.

Les Page Objects Copilot sont intégrés dans ces catégories, plutôt que dupliqués dans la suite
de tests. Côté IVY, `Tests/Ivy/Pages` distingue `Authentication`, `Navigation` et `Partners`
(avec ses composants). Les noms de classes et les namespaces sont conservés pour que cette
réorganisation ne change pas les appels existants.

## Tests par catégorie — étape 4 de la reconstruction

Les tests sont classés séparément des pages, sous le site concerné :

- `Functional/Smoke` : parcours courts qui vérifient les fonctions essentielles.
- `Functional/Scenarios` : scénarios métier complets.
- `Functional/Regression` : parcours de non-régression (TNR).
- `Technical/Accessibility` : contrôles d’accessibilité.
- `Technical/Performance` : mesures de performance et contrôles non fonctionnels.
- `Technical/Administration` : opérations techniques qui peuvent modifier les données; ces tests
  ne sont pas des smoke tests à lancer automatiquement.
- `Sync` (Portail uniquement) : synchronisation entre le portail et les fichiers de données.

Les dossiers IVY `Technical` contiennent les catégories `Accessibility`, `Compatibility`,
`Performance` et `Reliability`, prêtes à accueillir des tests.

### Catégories d'exécution Portail

Les catégories NUnit définissent également quels tests sont inclus dans la smoke CI :

- `CI_SMOKE` : scénarios V2 connectés et non-fonctionnels validés, sans mutation de données.
- `PORTAIL_PUBLIC_EXTERNAL` : contrôles qui quittent le Portail vers `www.groupe-e.ch`; séparés
  car l'accès automatisé au site public peut être bloqué par son service anti-robot.
- `PORTAIL_ACCESSIBILITY` : audit à lancer séparément, hors de la smoke CI courante.
- `PORTAIL_LEGACY_SMOKE` : ancien smoke à revalider; exclu de la smoke V2 par défaut.
- `PORTAIL_DATA_SYNC` : synchronisation V2, en aperçu par défaut; l'écriture complète demande
  l'activation explicite de `PORTAIL_V2_APPLY_SYNC=true`.

Les squelettes TNR (`Portail_TNR`, `Ivy_TNR`), le smoke historique Portail, le smoke interactif
Nestor, les outils `ConstructionsPages` et l'ancienne synchronisation `Portail_SyncInscription`
sont marqués NUnit `Explicit`. Ils ne partent pas lors d'une exécution générale : chaque test reste
disponible pour une exécution délibérée pendant sa reprise ou son inspection.

### Triage des scénarios legacy

Les catégories NUnit rendent le risque visible sans supprimer les scénarios :

- `LEGACY_READ_ONLY` : consultation ou téléchargement sans changement de données métier.
- `LEGACY_MUTATION` : changement d'un compte, contrat, tarif ou opération métier; ces cas sont
  `Explicit` pour éviter leur exécution accidentelle.
- `LEGACY_FINANCIAL` : sous-catégorie des mutations qui touchent les paiements, IBAN, tarifs ou
  relevés pouvant influencer la facturation.
- `LEGACY_EXTERNAL_EFFECT` : envoi ou demande externe, par exemple un e-mail; ces cas sont
  `Explicit`.
- `LEGACY_CSV_WRITE` : ancienne synchronisation qui réécrit le CSV local, sans aperçu ni garde
  d'application.
- `LEGACY_A_REPRENDRE` : scénario incomplet ou dont les vérifications ne sont pas fiables.

Le détail par scénario se trouve dans `doc/index.html`. Les inscriptions Portail commentées sont
du code inactif, pas des tests exécutables. Aucun scénario legacy n'a été supprimé. Les scénarios
de mutation identifiés sont isolés; les consultations en lecture seule peuvent être ciblées sans
déclencher ces mutations.

Depuis la racine, la smoke CI actuelle peut être exécutée avec :

```powershell
.\scripts\Run-TestsWithReport.ps1 -Filter "TestCategory=CI_SMOKE" -Configuration Release
```

Cette séparation ne supprime aucun test : elle évite de mélanger les résultats des suites externes
et historiques avec la validation V2 courante. L'audit d'accessibilité n'est pas lancé dans cette
étape.

## Suite de la reconstruction

1. Construire une stratégie de données dédiée pour les scénarios retenus, puis remplacer les
   données codées en dur par des comptes de test isolés.
2. Alimenter progressivement `Autonomous/FR`, `Autonomous/DE` et `Autonomous/EN` avec des cas
   indépendants issus des parcours métier retenus.
3. Confirmer avec les responsables les scénarios à supprimer; rien n'est retiré avant cette
   confirmation.

Les contrôles d'accessibilité sont laissés de côté jusqu'à demande explicite.

## Tests autonomes par langue — étape 5 de la reconstruction

Les dossiers `Autonomous/FR` et `Autonomous/DE` sont préparés pour le Portail; IVY dispose en plus
de `Autonomous/EN`. Un test autonome doit pouvoir être lancé seul, créer son propre navigateur
et contexte, préparer ou choisir ses données sans dépendre de l'ordre d'exécution, puis vérifier
son résultat. Chaque langue doit apparaître comme un cas de test distinct, pas comme une suite
d'actions qui dépend de la langue précédente.

Les tests existants restent rangés par catégorie et génèrent déjà des cas NUnit indépendants par
langue. Le Portail active FR/DE par défaut; IVY active FR/DE par défaut et EN reste disponible
sur demande. Les variables `LNG_FR`, `LNG_DE` et `LNG_EN` permettent de choisir les langues
exécutées.

## Référence Portail V2 Copilot

Documentation de référence en une page (scénarios smoke, procédure de chronométrage manuel et
calcul du gain de temps) : [doc/index.html](./doc/index.html).

## Rapports d'exécution et preuves

Depuis la racine du dépôt, lancer toutes les suites du projet et créer un rapport HTML consolidé :

```powershell
.\scripts\Run-TestsWithReport.ps1
```

Le rapport est enregistré dans `REPORTS/Executions/<date-heure>/index.html`. Chaque test
Playwright produit une capture de fin de test, réussie ou échouée; le rapport affiche les images
avec le résultat, la durée et les détails d'échec. Le dossier contient également les fichiers de
preuve, les résultats NUnit au format TRX et les rapports PDF historiques éventuels. Pour filtrer
les tests ou sélectionner la configuration :

```powershell
.\scripts\Run-TestsWithReport.ps1 -Filter "TestCategory=CI_SMOKE" -Configuration Release
```

Le script nettoie au début de l'exécution uniquement les dossiers de rapports consolidés âgés de
plus de trois mois. Ce nettoyage peut être lancé seul avec
`.\scripts\Remove-OldTestReports.ps1`. Les tests lancés directement avec `dotnet test` produisent
leurs captures en pièces jointes NUnit, mais le rapport HTML consolidé est créé par le script
d'exécution.

Les captures de tests connectés peuvent montrer des informations de compte ou de facturation.
Les rapports doivent rester dans un emplacement protégé; ne pas les committer ni les partager
sans vérifier et masquer les données personnelles. Le workflow CI ne téléverse pas le rapport
ni ses captures.

---

## Tests IVY

Les tests IVY couvrent l'application interne `ivy-qua.cce24x7.net`. Toutes les actions s'exécutent
via SSO Windows/Entra et sont rejouables en FR, DE ou EN selon les variables d'environnement
`LNG_FR`, `LNG_DE`, `LNG_EN`.

### Identifiants SSO et exécution CI

Tous les tests IVY utilisent le parcours partagé `Tests/Ivy/Authentication/IvyAuthentication.cs`
et le Page Object `Tests/Ivy/Pages/Authentication/PageLoginWindows.cs`. Les identifiants ne sont
pas écrits dans les tests :

- En local, définir `IVY_EMAIL` et `IVY_PASSWORD`, ou les placer dans le fichier ignoré par Git
  `Config/secrets.json`, sous `Ivy.Email` et `Ivy.Password`.
- Dans GitHub, créer/utiliser l'environnement `qual` dans **Settings > Environments**, puis ajouter
  `IVY_EMAIL` et `IVY_PASSWORD` dans ses secrets d'environnement. Le workflow manuel IVY est lié à
  `qual` et ne les injecte que dans l'étape d'exécution des tests.

Utiliser un compte de test autorisé sur IVY-Qual, limiter ses droits au nécessaire et renouveler
son secret selon la politique de l'organisation. Ne jamais le mettre dans le code, un fichier suivi
par Git, une commande de test ou un rapport. Le workflow IVY est manuel : il n'exécute pas de
connexion SSO planifiée sans supervision.

Après le SSO, les tests sélectionnent le tenant **Groupe-E**, démarrent l'interaction, puis ferment
les interactions laissées ouvertes. L'outil explicite
`Tests/Ivy/Tools/NettoyerInteractionsOuvertesIvy.cs` est disponible dans la liste des tests pour
faire ce nettoyage à la demande. Il boucle jusqu'à disparition du bandeau, avec une limite de
50 interactions par exécution; si cette limite est atteinte, il échoue explicitement.

### Smoke

#### ChangementDeLangue

**Objectif** : Vérifier que le sélecteur de langue d'IVY traduit correctement l'interface principale après chaque bascule. Rotation complète FR → DE → EN → FR.

**Pré-requis** :
- Compte SSO IVY valide

**Langues testées** : FR, DE, EN (rotation dans le même test)

**Étapes et résultats attendus** :

| # | Étape | Résultat attendu |
|---|---|---|
| 1 | Authentification SSO sur IVY-Qual en FR | Connexion réussie, interface principale en FR |
| 2 | Ouverture des Paramètres utilisateur + lecture du titre du panneau | Titre affiché : « Paramètres utilisateur » |
| 3 | Bascule de la langue vers DE | Interface rechargée en allemand |
| 4 | Ouverture des Paramètres + lecture du titre | Titre affiché : « Benutzereinstellungen » |
| 5 | Bascule de la langue vers EN | Interface rechargée en anglais |
| 6 | Ouverture des Paramètres + lecture du titre | Titre affiché : « User Settings » |
| 7 | Bascule de la langue vers FR | Interface rechargée en français |
| 8 | Ouverture des Paramètres + lecture du titre | Titre affiché : « Paramètres utilisateur » — retour à l'état initial confirmé |

**Critère de succès global** : à chacune des 4 étapes de vérification (FR initial, DE, EN, FR final), le titre du panneau Paramètres utilisateur est affiché dans la langue attendue. Aucune étape de bascule n'échoue.

> **Pourquoi ce point d'ancrage ?** Le panneau « Paramètres utilisateur » est toujours accessible depuis IVY (icône engrenage du header) et son titre est traduit côté serveur. C'est donc une cible de vérification stable, indépendante de la page sur laquelle on se trouve après login.

---

#### TrouverPartenaire

**Objectif** : La fonction de recherche IVY trouve le 1er partenaire du CSV et le header affiche correctement ses informations (Nr. SAP, nom, email) ainsi que ses boutons d'action.

**Pré-requis** :
- Compte SSO IVY valide
- Au moins un partenaire dans `data/Partenaires.csv`

**Langues testées** : FR, DE, EN

**Étapes et résultats attendus** :

| # | Étape | Résultat attendu |
|---|---|---|
| 1 | Authentification SSO sur IVY-Qual | Connexion réussie |
| 2 | Sélection de la langue | UI traduite dans la langue choisie |
| 3 | Recherche du 1er partenaire du CSV | Dashboard du partenaire affiché |
| 4 | Lecture du Nr. SAP dans le header | Le numéro SAP correspond à `NumeroPartenaire` du CSV |
| 5 | Lecture du nom dans le header | Le nom correspond à `Nom` du CSV |
| 6 | Lecture de l'email dans le header | L'email correspond à `Email` du CSV |
| 7 | Vérification de la présence des 4 boutons du header | Les boutons « édition contact », « édition partenaire », « Historique » et « Espace client » sont visibles |

**Critère de succès global** : le partenaire recherché est bien chargé et toutes ses données (3 champs + 4 boutons) sont affichées dans le header conformément au CSV.

---

#### OuverturePortailDepuisIvy

**Objectif** : Depuis IVY, le bouton « Espace client » du header doit ouvrir le portail SAP dans un nouvel onglet.

**Pré-requis** :
- Compte SSO IVY valide
- Au moins un partenaire dans `data/Partenaires.csv`

**Langues testées** : FR, DE, EN

**Étapes et résultats attendus** :

| # | Étape | Résultat attendu |
|---|---|---|
| 1 | Authentification SSO sur IVY-Qual | Connexion réussie, page d'ouverture IVY affichée |
| 2 | Sélection de la langue | UI traduite dans la langue choisie |
| 3 | Recherche du 1er partenaire du CSV | Dashboard du partenaire affiché |
| 4 | Clic sur le bouton « Espace client » (icône carte ID verte, dans le header) | Un nouvel onglet s'ouvre |
| 5 | Lecture de l'URL du nouvel onglet | URL contient `qu1.web.cce24x7.net` |

**Critère de succès global** : le nouvel onglet pointe bien vers le domaine SAP `qu1.web.cce24x7.net`.

---

### Fonctionnels

#### HistoriquePartenaire

**Objectif** : L'historique d'un partenaire est accessible depuis le header IVY et propose les vues simple, avancée et archive.

**Pré-requis** :
- Compte SSO IVY valide
- Partenaire ayant un historique non vide (1er du CSV par défaut)

**Langues testées** : FR, DE, EN

**Étapes et résultats attendus** :

| # | Étape | Résultat attendu |
|---|---|---|
| 1 | Login SSO IVY | Connexion réussie |
| 2 | Recherche du partenaire | Dashboard partenaire affiché |
| 3 | Clic sur le bouton « Historique » du header | Un popup/onglet historique s'ouvre |
| 4 | Lecture des lignes du tableau d'historique | Au moins 1 ligne présente (sinon échec : « Pas d'historique ») |
| 5 | Clic sur « Historique simple » | Vue simple affichée |
| 6 | Clic sur « Historique avancé » | Vue avancée affichée |
| 7 | Activation du toggle « Archive » | Vue archive affichée |

**Critère de succès global** : l'historique s'ouvre, contient des données, et chaque vue (simple / avancée / archive) bascule correctement.

---

#### ChangementNumeroTel

**Objectif** : La modification du numéro de téléphone d'un partenaire dans IVY est persistée dans IVY **ET** synchronisée dans le CSV local (`data/Partenaires.csv`).

**Pré-requis** :
- Compte SSO IVY avec droits d'édition
- Partenaire modifiable
- Accès en écriture sur `data/Partenaires.csv`

**Langues testées** : FR, DE, EN

**Étapes et résultats attendus** :

| # | Étape | Résultat attendu |
|---|---|---|
| 1 | Login SSO IVY + recherche partenaire | Dashboard partenaire affiché |
| 2 | Génération d'un numéro de téléphone aléatoire (format `079 XXX XX XX`) | Nouvelle valeur prête en mémoire |
| 3 | Ouverture du dialog « Mutation du partenaire » via le crayon d'édition | Dialog ouvert, formulaire visible |
| 4 | Saisie de la nouvelle valeur dans le champ Téléphone + clic « Enregistrer » | Dialog se ferme, modification appliquée |
| 5 | Réouverture du dialog | Dialog ouvert à nouveau |
| 6 | Lecture du champ Téléphone | Valeur identique à celle saisie à l'étape 4 |
| 7 | Relecture de `data/Partenaires.csv` depuis le disque | Fichier rechargé |
| 8 | Lecture de la propriété `Telephone` du partenaire dans le CSV | Valeur identique à celle saisie à l'étape 4 |

**Critère de succès global** : la valeur du téléphone est identique dans **IVY** (champ persisté) et dans **le CSV** (relu depuis le disque).

> **Note** : à la première exécution, la colonne `Telephone` est ajoutée automatiquement au CSV (les autres colonnes existantes sont préservées).
