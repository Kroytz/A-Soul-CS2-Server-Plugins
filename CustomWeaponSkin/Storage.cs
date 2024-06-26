namespace Storage;

public interface IStorage {
    public bool IsStorageInitialized();
    public string? GetPlayerModel(ulong SteamID, long itemDef);
    public Task<int> SetPlayerModel(ulong SteamID, long itemDef, string modelName);
    public Task<List<string>> GetPlayerAllModelAsync(ulong SteamID);
    public void ClearPlayerModel(ulong SteamID, long itemDef);
    public void ClearPlayerAllModelAsync(ulong SteamID);
}