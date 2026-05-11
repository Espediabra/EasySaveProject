# README – EasySave

## Description

EasySave est une application de sauvegarde permettant de copier des fichiers d'un dossier source vers un dossier de destination.

L'utilisateur peut créer et exécuter des tâches de sauvegarde en choisissant :

- Un dossier source
- Un dossier de destination
- Un type de sauvegarde :
  - Complète
  - Différentielle

L'application est disponible en deux versions :

- **Version console** : interface en ligne de commande, navigation au clavier.
- **Version graphique** (branche `feature/easyprojectUI`) : interface Avalonia, multilingue (français / anglais), avec suivi visuel des sauvegardes et des journaux.

---

## Prérequis

- Windows
- .NET 6 ou supérieur

---

## Installation

```bash
git clone https://github.com/Espediabra/EasySaveProject.git
cd EasySaveProject
dotnet build
```

### Choisir la version

- **Version console** : branche `main`
- **Version graphique** : branche `feature/easyprojectUI`

```bash
git checkout feature/easyprojectUI
dotnet build
```

### Lancer l'application

```bash
dotnet run
```

---

## Utilisation – Version console

### Menu principal

```
===== EasySave =====
1 - Lancer une sauvegarde
2 - Consulter les journaux
3 - Paramètres
4 - Quitter
```

### 1. Créer une sauvegarde

```
Nom de la sauvegarde : Backup1
Chemin source : TestData/Source1
Chemin destination : Backup/Source1
Type de sauvegarde :
1 - Complète
2 - Différentielle
Choix : 1
Sauvegarde créée avec succès.
```

### 2. Lister les sauvegardes

```
Sauvegardes disponibles :
[1] Backup1
    Source : TestData/Source1
    Destination : Backup/Source1
    Type : Complète
```

### 3. Lancer une sauvegarde

```
Entrer la sélection (ex. 1, 1-3, 1;3) : 1
Exécution en cours...
Copie des fichiers...
Sauvegarde terminée avec succès.
```

### 4. Consulter les journaux

```
Journaux du jour :
10:14:02  [INFO]    Backup1  Sauvegarde démarrée
10:14:08  [INFO]    Backup1  Fichier copié : rapport.pdf
10:14:15  [INFO]    Backup1  Sauvegarde terminée
```

### Mode ligne de commande (CLI)

EasySave console peut aussi être lancé avec un argument pour exécuter des sauvegardes directement, sans menu :

```bash
dotnet run -- 1        # exécute la sauvegarde n° 1
dotnet run -- 1-3      # exécute les sauvegardes 1, 2 et 3
dotnet run -- 1;3      # exécute les sauvegardes 1 et 3
dotnet run -- 1-2;4    # exécute les sauvegardes 1, 2 et 4
```

---

## Utilisation – Version graphique (Avalonia)

### Fenêtre principale

```
┌──────────────┬────────────────────────────────────┐
│  EasySave    │  Sauvegardes                       │
│              │  Gérez et exécutez vos tâches      │
│  ▸ Sauvegardes                       + Nouvelle   │
│    Journaux  │                                    │
│    Paramètres│  [1] Backup1     Full      Prêt    │
│              │  [2] Photos      Diff.     Prêt    │
│              │                                    │
│  2/5         │                                    │
└──────────────┴────────────────────────────────────┘
```

### 1. Créer une sauvegarde

- Cliquer sur **« + Nouvelle sauvegarde »**
- Remplir le formulaire :
  - **Nom** : Backup1
  - **Type** : Full ou Differential
  - **Chemin source** : TestData/Source1
  - **Chemin destination** : Backup/Source1
- Cliquer sur **« Créer la sauvegarde »**

Limite : 5 sauvegardes simultanées maximum.

### 2. Lancer une sauvegarde

- Cliquer sur la ligne de la sauvegarde dans la liste
- Lancer l'exécution depuis le panneau de détails
- Suivre la progression (statut **En cours…** puis **Terminé**)

### 3. Consulter les journaux

Page **Journaux** : tableau avec heure, niveau, job, message, taille et durée.
Filtre disponible par niveau : **All / Info / Warning / Error**.

### 4. Paramètres

Changement de langue : **English** ou **Français**.

---

## Types de sauvegarde

### Sauvegarde complète

Copie tous les fichiers du dossier source vers la destination.

### Sauvegarde différentielle

Copie uniquement les fichiers modifiés depuis la dernière sauvegarde complète.

---

## Exemple d'utilisation

1. Créer une sauvegarde :

```
Source : TestData/Source1
Destination : Backup/Source1
Type : Complète
```

2. Lancer la sauvegarde

3. Vérifier les fichiers dans :

```
Backup/Source1
```
