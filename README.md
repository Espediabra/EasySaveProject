# EasySave 3.0

<div align="center">

## Secure • Smart • Modular Backup Solution

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=.net)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-Latest-239120?style=for-the-badge&logo=c-sharp)](https://learn.microsoft.com/dotnet/csharp/)
[![Avalonia](https://img.shields.io/badge/UI-Avalonia-8A2BE2?style=for-the-badge)](https://avaloniaui.net/)
[![Platform](https://img.shields.io/badge/Platform-Windows-0078D6?style=for-the-badge&logo=windows)](https://www.microsoft.com/windows)
[![Version](https://img.shields.io/badge/Version-3.0-success?style=for-the-badge)]()

*A modern backup manager built for reliability, scalability and secure file protection.*

</div>

---

# Overview

EasySave is a modular backup management application developed in **C# / .NET 10**.

The project was designed to provide a complete and extensible backup ecosystem capable of managing secure file transfers, monitoring backup executions and simplifying large-scale backup workflows.

EasySave 3.0 introduces a modern architecture based on services, strategies and UI separation while integrating:

- Full, differential and incremental backups
- Real-time monitoring
- JSON/XML logging
- Encryption integration
- Multilingual support
- Modern Avalonia user interface
- Dedicated log server
- Configurable settings system
- Optimized backup execution

The solution is composed of multiple projects working together to provide a professional backup platform.

---

# Main Objectives

- Simplify backup management
- Secure sensitive files
- Provide scalable backup strategies
- Improve monitoring and traceability
- Separate business logic from presentation
- Offer a modern and intuitive UI
- Reduce unnecessary file transfers
- Ensure maintainable architecture

---

# Key Features

## Backup System

| Feature | Description |
|---|---|
| Full Backup | Copies all files from source to destination |
| Differential Backup | Copies only files modified since the last full backup |
| Incremental Backup | Copies only files modified since the last execution |
| Multi-job Execution | Execute multiple backup jobs simultaneously |
| Backup Strategies | Strategy-based backup engine architecture |
| Optimized Transfers | Prevents unnecessary copies |
| File Monitoring | Tracks execution state in real time |
| Large File Support | Handles large backup volumes efficiently |

---

## Security & Reliability

| Feature | Description |
|---|---|
| CryptoSoft Integration | External encryption executable integration |
| Secure Backup Workflow | Protects sensitive files during execution |
| Error Handling | Dedicated exception management system |
| Execution Traceability | Detailed operation history |
| Data Integrity | Secure and verified backup operations |
| Configurable Logging | JSON and XML logging providers |

---

## User Experience

| Feature | Description |
|---|---|
| Avalonia UI | Cross-platform modern desktop interface |
| Dark Mode Ready | Modern visual experience |
| Localization System | English and French support |
| Navigation Menus | Simplified workflow navigation |
| Settings Management | Configurable application behavior |
| Real-time Feedback | Live execution monitoring |
| Legacy Console Support | Backward compatibility console mode |

---
# User Interface

EasySave 3.0 provides a modern graphical interface focused on real-time monitoring, usability, and operational control.

The application now includes:

- Real-time backup monitoring
- Parallel job visualization
- Play / Pause / Stop controls
- Live progress tracking
- Log visualization and filtering
- Dynamic settings management
- Multi-language support
- Centralized logging integration

---

## Main Dashboard

The main dashboard allows users to manage and monitor all backup jobs from a single interface.

Features available directly from the dashboard:

- Create backup jobs
- Start multiple jobs simultaneously
- Pause / Resume / Stop jobs individually
- View live execution progress
- Track blocked jobs
- Monitor remaining files and transfer state

---

## Backup Management

Each backup job contains:

| Property | Description |
|---|---|
| Name | Backup job identifier |
| Source Path | Source directory |
| Target Path | Destination directory |
| Backup Type | Full or Differential |
| Status | Idle, Running, Paused, Completed, Error |
| Progress | Real-time execution percentage |
| Remaining Files | Remaining files to transfer |
| Blocking State | Displays detected blocking software |

---

## Real-Time Monitoring

The monitoring system displays live execution information for all running jobs.

Displayed information includes:

- Current file transfer
- Progress percentage
- Estimated remaining time (ETA)
- Remaining files
- Remaining size
- Transfer speed
- Pause state
- Blocking software detection

Example:

```text
[RUNNING] DocumentsBackup
Progress: 68%
Remaining files: 124
ETA: 00:02:18
Current file: report_2026.xlsx
```
---

# Supported Languages

| Language | Status |
|---|---|
| English | Available |
| French | Available |
| Spanish | Planned |
| German | Planned |

Localization files are stored inside the application resources and managed through the dedicated `LocalizationService`.

---

# Software Architecture

EasySave follows a layered and modular architecture to improve scalability and maintainability.

## Core Architecture

| Layer | Responsibility |
|---|---|
| UI Layer | User interaction and navigation |
| ViewModels | State management and binding |
| Services | Business logic and execution |
| Backup Strategies | Backup behavior implementation |
| Logging System | Execution trace generation |
| Configuration System | Persistent settings management |
| Localization System | Language resource management |

---

## Main Components

### Backup Engine

Responsible for:

- File transfers
- Backup execution
- Strategy management
- Optimization logic
- Progress tracking

### Strategy System

Implemented using dedicated backup strategies:

- `FullBackupStrategy`
- `DifferentialBackupStrategy`
- Base strategy abstraction
- Factory-based strategy creation

### Logging System

The application integrates a complete logging infrastructure:

- JSON log provider
- XML log provider
- Structured log entries
- Log DTO models
- Execution tracking

### Configuration System

Handles:

- Persistent application settings
- User preferences
- Backup configuration
- Runtime behavior

### Localization System

Provides:

- Dynamic translations
- Multi-language resource files
- UI language switching

### Encryption System

EasySave integrates with `CryptoSoft.exe` to secure sensitive backup files.

---

# Project Structure

```text
EasySaveProject/
│
├── CryptoSoft/
│   └── CryptoSoft.exe                # External encryption tool
│
├── EasyLog/
│   ├── JsonLogProvider.cs            # JSON logging implementation
│   ├── XmlLogProvider.cs             # XML logging implementation
│   ├── LogEntry.cs                   # Log model
│   ├── LogLevel.cs                   # Log severity levels
│   └── ILogProvider.cs               # Logging abstraction
│
├── EasySaveProject/
│   ├── App/
│   │   ├── Commands/                 # UI commands
│   │   ├── ViewModel/                # MVVM view models
│   │   └── LegacyConsoleApp.cs       # Legacy console mode
│   │
│   ├── Core/
│   │   ├── Backup/                   # Backup engine
│   │   │   ├── Factories/            # Strategy factories
│   │   │   └── Strategies/           # Backup strategies
│   │   │
│   │   ├── Config/                   # Configuration system
│   │   ├── DTOs/                     # Transfer objects
│   │   ├── Helper/                   # Utility helpers
│   │   ├── Localization/             # Language resources
│   │   ├── Models/                   # Core models
│   │   ├── Repositories/             # Data persistence layer
│   │   ├── Services/                 # Main services
│   │   └── State/                    # Execution state management
│   │
│   ├── Infrastructure/
│   │   └── Logging/                  # Logging infrastructure
│   │
│   ├── Views/                        # Avalonia views
│   └── Program.cs                    # Application entry point
│
├── EasySaveProject.Tests/
│   └── Unit tests
│
├── EasySaveProjectUI/
│   └── Dedicated Avalonia UI project
│
├── LogServer/
│   └── Centralized log server
│
└── README.md
```

---

# Installation

## Prerequisites

- Multi-platforming
- .NET 10.0
- Docker Desktop

Recommended:

- Visual Studio 2026
- Visual Studio Code 2026
- Git

---

## Clone Repository

```bash
git clone https://github.com/Espediabra/EasySaveProject.git
```

---

## Navigate to Project

```bash
cd EasySaveProject
```

---

---

## Configuration 

```bash
dotnet msbuild -t: PublishWin
dotnet msbuild -t: PublishLinux
```

---

## Build Solution

```bash
dotnet build
```

---

## Run Application

```bash
dotnet run
```

---

# Usage

## Create a Backup Job

1. Launch EasySave
2. Create a new backup task
3. Select source folder
4. Select destination folder
5. Choose backup type
6. Enable encryption if needed
7. Save configuration

---

## Execute Backup

1. Open backup list
2. Select a backup task
3. Start execution
4. Monitor progress in real time
5. Review logs after completion

---

# Backup Types

## Full Backup

Copies every file from the source directory.

```text
Source      : C:/Projects
Destination : D:/Backups/Projects
Type        : Full
```

---

## Differential Backup

Copies files modified since the last full backup.

```text
Source      : C:/Projects
Destination : D:/Backups/Projects
Type        : Differential
```

---

# Logs & Monitoring

EasySave includes a complete monitoring and logging infrastructure.

## JSON Log Example

```json
{
  "Timestamp": "2026-05-18T09:42:20",
  "BackupName": "WorkDocuments",
  "Source": "C:/Users/Admin/Documents",
  "Destination": "D:/Backups/Documents",
  "Type": "Incremental",
  "FilesCopied": 42,
  "Encryption": true,
  "Status": "Success"
}
```

---

## Logging Features

- Structured JSON logs
- XML export support
- Execution history
- Transfer statistics
- Error tracking
- Monitoring integration
- Real-time execution status

---

# Security

Security is a central part of EasySave 3.0.

## Security Features

- External encryption integration
- Sensitive file protection
- Secure backup workflows
- Controlled file access
- Isolated error handling
- Safe configuration management

---

# Performance Optimizations

EasySave includes several optimization mechanisms:

- Intelligent file comparison
- Reduced unnecessary copies
- Optimized transfer workflow
- Strategy-based execution
- Lightweight logging system
- Multi-task execution support

---

# Testing

The solution includes a dedicated test project:

```text
EasySaveProject.Tests/
```

Used for:

- Unit testing
- Backup logic validation
- Service testing
- Future regression testing

---

# Technologies Used

| Technology | Purpose |
|---|---|
| C# | Main language |
| .NET 10 | Application framework |
| Avalonia UI | Desktop interface |
| Docker | Log centralization service |
| JSON / XML | Logging formats |
| MVVM | UI architecture |
| Strategy Pattern | Backup strategy management |
| Repository Pattern | Data management |


---

# Roadmap

## Planned Improvements

- Cloud backup integration
- Parallel file transfers
- Backup scheduling system
- Compression support
- Advanced dashboard
- Remote backup monitoring
- Real-time analytics
- Automatic updates
- Linux support
- Backup restoration assistant

---

# Contributing

Contributions and improvements are welcome.

## Workflow

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push your branch
5. Open a Pull Request

---

# License

This project is distributed under the MIT License.

---

<div align="center">

## EasySave 3.0

Professional backup management designed for security, monitoring and scalability.

</div>
