using Microsoft.Data.Sqlite;
using Dapper;
using System.Data;
namespace Storage;

public class SqliteStorage : IStorage {

    private SqliteConnection conn { get; set; }
    public SqliteStorage(string ModuleDirectory) {

        conn = new SqliteConnection($"Data Source={Path.Join(ModuleDirectory, "data.db")}");
        conn.Open();

        conn.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS `cws_players` (
                `steamid` UNSIGNED BIG INT NOT NULL,
                `itemdef` INTEGER,
                `modelname` TEXT
            )
        ");
    }

    public bool IsStorageInitialized()
    {
        return conn.State == ConnectionState.Open;
    }

    public dynamic? GetPlayerModelInternal(ulong SteamID, long itemDef)
    {
        var result = conn.QueryFirstOrDefault(@$"SELECT `modelname` FROM `cws_players` WHERE `steamid` = @SteamId AND `itemdef` = @ItemDef;", new { SteamId = SteamID, ItemDef = itemDef });
        return result;
    }

    public string? GetPlayerModel(ulong SteamID, long itemDef)
    {
        var result = GetPlayerModelInternal(SteamID, itemDef);
        if (result == null)
        {
            return null;
        }
        return result!.modelname;
    }

    public async Task<int> SetPlayerModel(ulong SteamID, long itemDef, string modelName)
    {
        if (GetPlayerModel(SteamID, itemDef) == null)
        {
            var sql = $"INSERT INTO `cws_players` (`steamid`, `itemdef`, `modelname`) VALUES ({SteamID}, @itemDef, @modelName);";
            return await conn.ExecuteAsync(sql,
                new
                {
                    itemDef,
                    modelName
                }
            );
        }
        else
        {
            var sql = $"UPDATE `cws_players` SET `modelname` = @modelName WHERE `steamid` = {SteamID} AND `itemdef` = @itemDef;";
            return await conn.ExecuteAsync(sql,
                new
                {
                    itemDef,
                    modelName
                }
            );
        }
    }

    public async Task<List<string>> GetPlayerAllModelAsync(ulong SteamID)
    {
        var query = "SELECT modelname FROM `cws_players` WHERE `steamid` = @SteamID;";
        var result = await conn.QueryAsync<string>(query, new { SteamID });
        return result.ToList();
    }

    public async void ClearPlayerModel(ulong SteamID, long itemDef)
    {
        var query = "DELETE FROM `cws_players` WHERE `steamid` = @SteamID AND `itemdef` = @itemDef;";
        await conn.QueryAsync<string>(query, new { SteamID, itemDef });
    }

    public async void ClearPlayerAllModelAsync(ulong SteamID)
    {
        var query = "DELETE FROM `cws_players` WHERE `steamid` = @SteamID;";
        await conn.QueryAsync<string>(query, new { SteamID });
    }
}