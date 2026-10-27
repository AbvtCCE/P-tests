# TestPlayrights

Projet de tests automatisés Playwright (.NET / NUnit) pour les applications Groupe-E :
**Portail my-q.groupe-e.ch**, **IVY-Qual** et **Nestor**.

Les tests sont organisés en deux familles :
- **Smoke** — vérifications rapides post-déploiement (connexion, navigation, ouverture des composants critiques).
- **Fonctionnels** — scénarios métier complets (création, mutation, consultation de données).

---

## Tests IVY

Les tests IVY couvrent l'application interne `ivy-qua.cce24x7.net`. Toutes les actions s'exécutent
via SSO Windows/Entra et sont rejouables en FR, DE ou EN selon les variables d'environnement
`LNG_FR`, `LNG_DE`, `LNG_EN`.

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
