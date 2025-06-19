# QuickShortcutList
## Description
QuickShortcutList is a lightweight system tray application that provides quick access to your favorite directories and their contents through a customizable menu.

## Building/Compiling
This was developed in VSCode, but should work in VisualStudio as well. Build the QuickShortcutList.csproj to compile the application.

## Running
After [building/compiling](#buildingcompiling), run the QuickShortcutList.exe. Optionally, add the resulting executable to your startup folder to have it run on startup. This can be done by pressing `Win+R` and typing `shell:startup` and pressing enter. Then, copy the executable or create a shortcut in the folder that opens.

## Configuration
QuickShortcutList is configured using a `config.yaml` file located in the same directory as the application. The configuration file supports the following settings:

### Configuration Options

| Option | Description | Default |
|--------|-------------|---------|
| `Folders` | List of directories to include in the menu | None |
| `MaxDepth` | Maximum folder depth to display in the menu | 2 |
| `SortFoldersAlphabetically` | Whether to sort top-level folders alphabetically | `true` |

### Example Configuration

```yaml
Folders:
  - '%APPDATA%'
  - 'C:\Projects'
  - '%USERPROFILE%\Documents'
MaxDepth: 3
SortFoldersAlphabetically: true
```