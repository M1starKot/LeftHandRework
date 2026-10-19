using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace PenaltiesRemoved;

public record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.mbarneto.penaltiesremoved";
    public string Name { get; init; } = "Penalties Removed";
    public string Author { get; init; } = "m-barneto";
    public List<string>? Contributors { get; init; }
    public SemanticVersioning.Version Version { get; init; } = new("1.4.0");
    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.0");
    public bool HasPrepatcher { get; init; } = false;
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public string? Url { get; init; } = "https://github.com/m-barneto/PenaltiesRemoved";
    public string License { get; init; } = "MIT";
}

public sealed class ModConfig
{
    [JsonPropertyName("removeSpeedPenalty")]
    public bool RemoveSpeedPenalty { get; set; } = true;

    [JsonPropertyName("removeMousePenalty")]
    public bool RemoveMousePenalty { get; set; } = true;

    [JsonPropertyName("removeErgoPenalty")]
    public bool RemoveErgoPenalty { get; set; } = true;

    [JsonPropertyName("removeHearingPenalty")]
    public bool RemoveHearingPenalty { get; set; } = true;

    [JsonPropertyName("removeAttachmentErgoPenalty")]
    public bool RemoveAttachmentErgoPenalty { get; set; } = true;
}

[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]
public sealed class PenaltiesRemovedMod(
    TemplateTable templateTable,
    ISptLogger<PenaltiesRemovedMod> logger
) : IOnLoad
{
    private static readonly PropertyInfo? DeafStrengthProp =
        typeof(TemplateItemProperties).GetProperty(nameof(TemplateItemProperties.DeafStrength));

    private static readonly object? DeafNoneValue = ResolveDeafNoneValue();

    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        var config = LoadConfig();

        foreach (var (_, item) in templateTable.Items)
        {
            if (item.Type != "Item" || item.Properties is null)
            {
                continue;
            }

            ApplyChanges(item.Properties, config);
        }

        logger.Success("[Penalties Removed] Equipment and attachment penalties removed (SPT 4.1.6).");
        return Task.CompletedTask;
    }

    private static void ApplyChanges(TemplateItemProperties props, ModConfig config)
    {
        if (config.RemoveSpeedPenalty && props.SpeedPenaltyPercent is < 0)
        {
            props.SpeedPenaltyPercent = 0;
        }

        if (config.RemoveMousePenalty && props.MousePenalty is < 0)
        {
            props.MousePenalty = 0;
        }

        if (config.RemoveErgoPenalty && props.WeaponErgonomicPenalty is < 0)
        {
            props.WeaponErgonomicPenalty = 0;
        }

        if (config.RemoveHearingPenalty && props.DeafStrength is not null && DeafStrengthProp is not null && DeafNoneValue is not null)
        {
            if (!Equals(props.DeafStrength, DeafNoneValue))
            {
                DeafStrengthProp.SetValue(props, DeafNoneValue);
            }
        }

        if (config.RemoveAttachmentErgoPenalty && props.Ergonomics is < 0)
        {
            props.Ergonomics = 0;
        }
    }

    private static object? ResolveDeafNoneValue()
    {
        if (DeafStrengthProp is null)
        {
            return null;
        }

        var propType = Nullable.GetUnderlyingType(DeafStrengthProp.PropertyType) ?? DeafStrengthProp.PropertyType;

        if (propType == typeof(string))
        {
            return "none";
        }

        if (propType.IsEnum)
        {
            foreach (var name in Enum.GetNames(propType))
            {
                if (string.Equals(name, "None", StringComparison.OrdinalIgnoreCase))
                {
                    return Enum.Parse(propType, name);
                }
            }

            return Enum.ToObject(propType, 0);
        }

        return null;
    }

    private ModConfig LoadConfig()
    {
        try
        {
            var modDir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
            var configPath = System.IO.Path.Combine(modDir, "config.json");

            if (!System.IO.File.Exists(configPath))
            {
                var altPath = System.IO.Path.Combine(modDir, "config", "config.json");
                if (System.IO.File.Exists(altPath))
                {
                    configPath = altPath;
                }
            }

            if (System.IO.File.Exists(configPath))
            {
                var json = System.IO.File.ReadAllText(configPath);
                return JsonSerializer.Deserialize<ModConfig>(json) ?? new ModConfig();
            }
        }
        catch (Exception ex)
        {
            logger.Warning($"[Penalties Removed] Failed to read config.json, using defaults: {ex.Message}");
        }

        return new ModConfig();
    }
}