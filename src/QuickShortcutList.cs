using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using YamlDotNet.Serialization;
using System.Linq;
using System.Drawing;

public class QuickShortcutList
{
    private NotifyIcon notifyIcon;
    private FileSystemWatcher configWatcher;

    public QuickShortcutList()
    {
        notifyIcon = new NotifyIcon
        {
            Visible = true
        };

        // Create a context menu and assign it to the NotifyIcon
        notifyIcon.ContextMenuStrip = Update_ContextMenu();

        // Build the path to the icon file
        string iconFilePath = System.IO.Path.Combine(Application.StartupPath, $"images\\ICOs\\QuickShortcutList.ico");

        // Set the icon and text of the NotifyIcon object
        notifyIcon.Icon = new System.Drawing.Icon(iconFilePath);
        notifyIcon.Text = "Click to open shortcuts";

        StartConfigWatcher(); // Start watching for config changes
    }


    private void StartConfigWatcher()
    {
        configWatcher = new FileSystemWatcher
        {
            Path = AppDomain.CurrentDomain.BaseDirectory,
            Filter = "config.yaml",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
        };

        configWatcher.Changed += OnConfigChanged;
        configWatcher.EnableRaisingEvents = true;
    }


    private void OnConfigChanged(object sender, FileSystemEventArgs e)
    {
        try
        {
            var newConfig = ReadConfig(); // Will throw if malformed
            notifyIcon.ContextMenuStrip = Update_ContextMenu(); // Rebuild menu
        }
        catch
        {
            MessageBox.Show("The YAML configuration is malformed. Keeping the existing menu.",
            "Config Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private ContextMenuStrip Update_ContextMenu()
    {
        var contextMenu = new ContextMenuStrip();
        var config = ReadConfig();

        // Get the list of configured folders
        var configuredFolders = config.Folders
            .Select(folder => new DirectoryInfo(Environment.ExpandEnvironmentVariables(folder)))
            .ToList();

        // Sort alphabetically only if specified in config
        if (config.SortFoldersAlphabetically)
        {
            configuredFolders = configuredFolders.OrderBy(folder => folder.Name).ToList();
        }
        // Otherwise, the order from the YAML file is maintained

        // Add each configured folder as a top-level menu item
        foreach (var configFolder in configuredFolders)
        {
            try
            {
                var menuItem = new ToolStripMenuItem(configFolder.Name);

                // Add click handler for the main folder
                menuItem.Click += (sender, e) =>
                {
                    // Open the folder in File Explorer
                    System.Diagnostics.Process.Start("explorer.exe", configFolder.FullName);
                };

                // Recursively add subdirectories
                if (configFolder.Exists)
                {
                    AddSubdirectoriesAndFiles(menuItem, configFolder, 1, config.MaxDepth);
                }

                contextMenu.Items.Add(menuItem);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error accessing folder {configFolder.Name}: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Add an option to edit the config file
        var editConfigItem = new ToolStripMenuItem("Edit Config");
        editConfigItem.Click += (sender, e) =>
        {
            try
            {
                // Open config.yaml with the default associated application
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.yaml");
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = configPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open config file: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };
        contextMenu.Items.Add(editConfigItem);

        // Add a close option to the context menu
        var closeMenuItem = new ToolStripMenuItem("Close");
        closeMenuItem.Click += CloseMenuItem_Click;
        contextMenu.Items.Add(closeMenuItem);

        return contextMenu;
    }

    private void AddSubdirectoriesAndFiles(ToolStripMenuItem parentMenuItem, DirectoryInfo parentDir, int currentDepth, int maxDepth)
    {
        // Stop recursion if we've reached max depth
        if (currentDepth > maxDepth)
            return;

        try
        {
            // Get and sort subdirectories
            var subDirectories = parentDir.GetDirectories()
                .OrderBy(dir => dir.Name)
                .ToList();

            // Add all subdirectories first
            foreach (var subDir in subDirectories)
            {
                var subMenuItem = new ToolStripMenuItem(subDir.Name);

                // Add click handler for this subdirectory
                subMenuItem.Click += (sender, e) =>
                {
                    // Open the subfolder in File Explorer
                    System.Diagnostics.Process.Start("explorer.exe", subDir.FullName);
                };

                // Recursively add sub-subdirectories
                AddSubdirectoriesAndFiles(subMenuItem, subDir, currentDepth + 1, maxDepth);

                // Add the submenu item to parent
                parentMenuItem.DropDownItems.Add(subMenuItem);
            }

            // Now add all files
            var files = parentDir.GetFiles()
                .OrderBy(file => file.Name)
                .ToList();

            // Add a separator if we have both directories and files
            if (subDirectories.Any() && files.Any())
            {
                parentMenuItem.DropDownItems.Add(new ToolStripSeparator());
            }


            foreach (var file in files)
            {
                Icon fileIcon = Icon.ExtractAssociatedIcon(file.FullName);
                var fileMenuItem = new ToolStripMenuItem(file.Name)
                {
                    Image = fileIcon?.ToBitmap()
                };
                fileMenuItem.Click += (sender, e) =>
                {
                    System.Diagnostics.Process.Start("explorer.exe", file.FullName);
                };
                parentMenuItem.DropDownItems.Add(fileMenuItem);
            }

        }
        catch (Exception ex)
        {
            // Silently handle errors for subdirectories (like access denied)
            // Could log these errors if needed
        }
    }

    private Config ReadConfig()
    {
        var deserializer = new DeserializerBuilder().Build();
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.yaml");
        var configText = File.ReadAllText(path);
        var config = deserializer.Deserialize<Config>(configText);

        // Process environment variables in folder paths
        if (config.Folders != null)
        {
            for (int i = 0; i < config.Folders.Count; i++)
            {
                config.Folders[i] = Environment.ExpandEnvironmentVariables(config.Folders[i]);
            }
        }

        return config;
    }

    private void CloseMenuItem_Click(object sender, EventArgs e)
    {
        // Dispose the NotifyIcon object to remove it from the system tray
        notifyIcon.Dispose();
        Application.Exit(); // Exit the application
    }
}