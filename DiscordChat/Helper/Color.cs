using System.Drawing;
using CounterStrikeSharp.API.Modules.Utils;

namespace DiscordChat.Helper;

public static class ColorHelper
{
    private static readonly Dictionary<string, char> PredefinedColors = new()
    {
        { "#ffffff", ChatColors.White },
        { "#8b0000", ChatColors.DarkRed },
        { "#b981f0", ChatColors.LightPurple },
        { "#3eff3f", ChatColors.Green },
        { "#bcfe94", ChatColors.Olive },
        { "#a3fe47", ChatColors.Lime },
        { "#ff3f3f", ChatColors.Red },
        { "#c4c4c4", ChatColors.Grey },
        { "#ebe378", ChatColors.Gold },
        { "#b0c2d8", ChatColors.Silver },
        { "#5d97d7", ChatColors.Blue },
        { "#4c6aff", ChatColors.DarkBlue },
        { "#d42de6", ChatColors.Magenta },
        { "#eb4b4b", ChatColors.LightRed },
        { "#e1af37", ChatColors.Orange },
    };
    
    public static char HexColorToChatColor(string hexColorCode)
    {
        var color = ColorTranslator.FromHtml(hexColorCode);

        hexColorCode = hexColorCode.ToUpper();

        if (PredefinedColors.TryGetValue(hexColorCode, out var colorName))
        {
            return colorName;
        }

        var targetColor = ColorTranslator.FromHtml(hexColorCode);
        var closestColor = FindClosestColor(targetColor, PredefinedColors.Keys);
        
        return PredefinedColors.TryGetValue(closestColor, out var symbol) ? symbol : ChatColors.Default;
    }
    public static Color ChatColorToHexColor(char chatColor)
    {
        var hex = PredefinedColors.FirstOrDefault(x => x.Value == chatColor).Key ?? "#ffffff";
        return ColorTranslator.FromHtml(hex);
    }

    private static string FindClosestColor(Color targetColor, IEnumerable<string> colorHexCodes)
    {
        var minDistance = double.MaxValue;
        string? closestColor = null;

        foreach (var hexCode in colorHexCodes)
        {
            var color = ColorTranslator.FromHtml(hexCode);
            var distance = ColorDistance(targetColor, color);

            if (!(distance < minDistance))
                continue;

            minDistance = distance;
            closestColor = hexCode;
        }

        return closestColor ?? "#ffffff";
    }

    private static double ColorDistance(Color color1, Color color2)
    {
        var rDiff = color1.R - color2.R;
        var gDiff = color1.G - color2.G;
        var bDiff = color1.B - color2.B;

        return Math.Sqrt(rDiff * rDiff + gDiff * gDiff + bDiff * bDiff);
    }
}