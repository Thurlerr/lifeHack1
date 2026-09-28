using System.Text.Json;
using Microsoft.JSInterop;

namespace LifeHacks.Services;

public class LocalStorageService : IStorageService
{
    private readonly IJSRuntime _js;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public LocalStorageService(IJSRuntime js)
    {
        _js = js;
    }

    public async ValueTask<T?> GetItemAsync<T>(string key)
    {
        try
        {
            var raw = await _js.InvokeAsync<string?>("localStorage.getItem", key);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(raw, JsonOptions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LocalStorageService] Error reading key '{key}': {ex.Message}");
            return default;
        }
    }

    public async ValueTask SetItemAsync<T>(string key, T value)
    {
        try
        {
            var json = JsonSerializer.Serialize(value, JsonOptions);
            await _js.InvokeVoidAsync("localStorage.setItem", key, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LocalStorageService] Error writing key '{key}': {ex.Message}");
        }
    }

    public async ValueTask RemoveItemAsync(string key)
    {
        try
        {
            await _js.InvokeVoidAsync("localStorage.removeItem", key);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LocalStorageService] Error removing key '{key}': {ex.Message}");
        }
    }

    public async ValueTask ClearAsync()
    {
        try
        {
            await _js.InvokeVoidAsync("localStorage.clear");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LocalStorageService] Error clearing storage: {ex.Message}");
        }
    }

    public async ValueTask<string> ExportBackupJsonAsync()
    {
        var backup = new Dictionary<string, string>();
        string[] keys = ["lifehacks_habits", "lifehacks_trackers", "lifehacks_purchases", "lifehacks_history", "lifehacks_settings", "lifehacks_last_date"];

        foreach (var key in keys)
        {
            try
            {
                var val = await _js.InvokeAsync<string?>("localStorage.getItem", key);
                if (val != null)
                {
                    backup[key] = val;
                }
            }
            catch
            {
                // ignore key read error
            }
        }

        var exportPayload = new
        {
            App = "LifeHacks",
            Version = "2.0",
            ExportedAt = DateTime.UtcNow,
            Data = backup
        };

        return JsonSerializer.Serialize(exportPayload, JsonOptions);
    }

    public async ValueTask<bool> ImportBackupJsonAsync(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("Data", out var dataElem) && dataElem.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in dataElem.EnumerateObject())
                {
                    var val = prop.Value.ValueKind == JsonValueKind.String
                        ? prop.Value.GetString()
                        : prop.Value.GetRawText();

                    if (!string.IsNullOrEmpty(val))
                    {
                        await _js.InvokeVoidAsync("localStorage.setItem", prop.Name, val);
                    }
                }
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LocalStorageService] Import error: {ex.Message}");
            return false;
        }
    }

    public async ValueTask DownloadBackupFileAsync()
    {
        var json = await ExportBackupJsonAsync();
        var filename = $"lifehacks_backup_{DateTime.Now:yyyyMMdd_HHmm}.json";
        await _js.InvokeVoidAsync("appInterop.downloadFile", filename, "application/json", json);
    }
}
