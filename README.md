# TxtPilot

A simple Windows desktop text file manager built with WPF and .NET 8.

## Features

- Select a folder and list all `.txt` files in it.
- View file name, size, created date, and last modified date.
- Open and read text files.
- Edit and save existing files.
- Create new `.txt` files, with overwrite confirmation.
- Refresh the file list.
- Warn before losing unsaved changes.
- Detect files modified outside the application on save
  (Reload / Overwrite / Cancel).
- Handle missing files or folders, invalid file names, access denied,
  and files that are currently in use.
- Write daily log files with Serilog in a `Logs` folder.

## Requirements

- Windows 10/11
- .NET 8 SDK to build the project
- Visual Studio 2022 with the ".NET desktop development" workload,
  or the .NET CLI

The self-contained executable does not require .NET to be installed.

## Build and Run

From the repository root:

```powershell
dotnet build
dotnet run --project TxtPilot/TxtPilot.csproj
```

Or open `TxtPilot.sln` in Visual Studio 2022 and press `F5`.

## Publish a Standalone Executable

```powershell
dotnet publish TxtPilot/TxtPilot.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true
```

The executable is created at:

```text
TxtPilot/bin/Release/net8.0-windows/win-x64/publish/TxtPilot.exe
```

## Usage

1. Click **Browse Folder** and select a folder.
2. Select a `.txt` file from the Explorer pane.
3. Click **Edit**, make changes, then click **Save** or **Cancel**.
4. Click **+** to create a new text file.
5. Click the refresh button to reload the file list.

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+S` | Save |
| `Ctrl+I` | Enable edit mode |
| `Ctrl+N` | Create a new file |

## Logging

Logs are stored in a `Logs` folder next to the executable. A new file is
created each day:

```text
log-YYYYMMDD.txt
```

Each entry contains the timestamp, log level, operation, message, and
exception details when applicable.

## Project Structure

- `MainWindow` — folder explorer, editor, and file operations.
- `NewFileWindow` — dialog for creating a new file.
- `FileConflictWindow` — dialog shown when a file changes externally.
- `App` — application startup, theme, and Serilog configuration.

## Notes

- Only `.txt` files in the selected folder are listed; subfolders are not scanned.
- Files are saved using UTF-8 encoding.
- If the folder is deleted while editing, unsaved changes remain in the editor
  but cannot be saved to the original location.
