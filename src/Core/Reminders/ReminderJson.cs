using System.Text.Json;
using System.Text.Json.Serialization;

namespace DesktopPet.Core.Reminders;

public static class ReminderJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static string Serialize(ReminderDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return JsonSerializer.Serialize(document, Options);
    }

    public static ReminderDocument Deserialize(string json)
    {
        try
        {
            var document = JsonSerializer.Deserialize<ReminderDocument>(json, Options)
                ?? throw new InvalidDataException("Reminder data is empty.");
            Validate(document);
            return document;
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is JsonException or NotSupportedException or InvalidOperationException)
        {
            throw new InvalidDataException("Reminder data is invalid.", exception);
        }
    }

    private static void Validate(ReminderDocument document)
    {
        if (document.Version != ReminderDocument.CurrentVersion)
            throw new InvalidDataException($"Unsupported reminder version {document.Version}.");
        if (document.Items.Count > ReminderDocument.MaxItems)
            throw new InvalidDataException("Reminder item limit exceeded.");
        if (document.Items.Select(item => item.Id).Distinct().Count() != document.Items.Count)
            throw new InvalidDataException("Reminder IDs must be unique.");
        if (document.Items.Select(item => item.Order).Distinct().Count() != document.Items.Count)
            throw new InvalidDataException("Reminder order values must be unique.");
        if (document.Items.Any(item =>
                ReminderDefinition.Validate(item) != ReminderValidationError.None))
            throw new InvalidDataException("One or more reminders are invalid.");
    }
}
