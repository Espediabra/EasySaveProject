# README – EasySave

## Description

EasySave est une application de sauvegarde permettant de copier des fichiers d'un dossier source vers un dossier de destination.

L'utilisateur peut créer et exécuter des tâches de sauvegarde en choisissant :

- Un dossier source
- Un dossier de destination
- Un type de sauvegarde :
  - Complète
  - Différentielle

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

### Lancer l'application

```bash
dotnet run
```

---

## Utilisation

### Menu principal

```
===== EasySave =====

1 - Créer une sauvegarde
2 - Lister les sauvegardes
3 - Lancer une sauvegarde
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
Entrer l'ID de la sauvegarde à exécuter : 1

Exécution en cours...

Copie des fichiers...
Sauvegarde terminée avec succès.
```

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
