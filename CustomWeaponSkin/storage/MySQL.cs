using Dapper;
using MySqlConnector;
using Storage;
using System.Data;

namespace Storage;
public class MySQLStorage : IStorage
{

    private MySqlConnection conn;

    private string table;

    public MySQLStorage(string ip, string port, string user, string password, string database, string table)
    {
        string connectStr = $"server={ip};port={port};user={user};password={password};database={database};Pooling=true;MinimumPoolSize=0;MaximumPoolsize=640;ConnectionIdleTimeout=30;AllowUserVariables=true";
        this.table = table;
        conn = new MySqlConnection(connectStr);
        conn.Execute($"""
            CREATE TABLE IF NOT EXISTS `{table}` (
                `steamid` BIGINT UNSIGNED NOT NULL PRIMARY KEY,
                `itemdef` INTEGER,
                `modelname` TEXT
            );
        """);
    }

    public bool IsStorageInitialized()
    {
        return conn.State == ConnectionState.Open;
    }

    public dynamic? GetPlayerModelInternal(ulong SteamID, long itemDef)
    {
        var result = conn.QueryFirstOrDefault($"SELECT `modelname` FROM `{table}` WHERE `steamid` = {SteamID} AND `itemdef` = {itemDef};");
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
            var sql = $"INSERT INTO {table} (`steamid`, `itemdef`, `modelname`) VALUES ({SteamID}, @itemDef, @modelName);";
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
            var sql = $"UPDATE {table} SET `modelname` = @modelName WHERE `steamid` = {SteamID} AND `itemdef` = @itemDef;";
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

    public async void ClearPlayerAllModelAsync(ulong SteamID)
    {
        var query = "DELETE FROM `cws_players` WHERE `steamid` = @SteamID;";
        await conn.QueryAsync<string>(query, new { SteamID });
    }
}