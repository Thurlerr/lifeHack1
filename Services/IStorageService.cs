namespace LifeHacks.Services;

public interface IStorageService
{
    ValueTask<T?> GetItemAsync<T>(string key);
    ValueTask SetItemAsync<T>(string key, T value);
    ValueTask RemoveItemAsync(string key);
    ValueTask ClearAsync();
    ValueTask<string> ExportBackupJsonAsync();
    ValueTask<bool> ImportBackupJsonAsync(string json);
    ValueTask DownloadBackupFileAsync();
}
