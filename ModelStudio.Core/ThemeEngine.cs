using System;
using System.Collections.Generic;

namespace ModelStudio.Core;

public class StudioTheme
{
    public string Name { get; set; } = "Cyberpunk Neon";
    public string PrimaryAccent { get; set; } = "#8B5CF6";
    public string SecondaryAccent { get; set; } = "#06B6D4";
    public string BackgroundDark { get; set; } = "#0B0D14";
    public string PanelDark { get; set; } = "#141923";
    public string HeaderDark { get; set; } = "#1E2430";
    public string BorderBrush { get; set; } = "#262C3A";
}

public class ThemeEngine
{
    public static StudioTheme GetTheme(string themeName) => themeName switch
    {
        "Obsidian Dark" => new StudioTheme
        {
            Name = "Obsidian Dark",
            PrimaryAccent = "#EC4899",
            SecondaryAccent = "#F59E0B",
            BackgroundDark = "#08080A",
            PanelDark = "#121216",
            HeaderDark = "#181820",
            BorderBrush = "#2A2A35"
        },
        "Emerald Studio" => new StudioTheme
        {
            Name = "Emerald Studio",
            PrimaryAccent = "#10B981",
            SecondaryAccent = "#3B82F6",
            BackgroundDark = "#061412",
            PanelDark = "#0E2420",
            HeaderDark = "#14302B",
            BorderBrush = "#1E443D"
        },
        "Clean Dark Slate" => new StudioTheme
        {
            Name = "Clean Dark Slate",
            PrimaryAccent = "#3B82F6",
            SecondaryAccent = "#8B5CF6",
            BackgroundDark = "#0F172A",
            PanelDark = "#1E293B",
            HeaderDark = "#334155",
            BorderBrush = "#475569"
        },
        _ => new StudioTheme() // Cyberpunk Neon
    };
}
