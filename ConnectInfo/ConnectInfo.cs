using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Utils;
using MaxMind.Db;
using Newtonsoft.Json;

namespace ConnectInfo
{
    public class ConnectInfoConfig : BasePluginConfig
    {
        [JsonPropertyName("GeoLiteLanguage")] public string GeoLiteLanguage { get; set; } = "en";
        [JsonPropertyName("CityIncluded")] public bool CityIncluded { get; set; } = true;
        [JsonPropertyName("LogPrefix")] public string LogPrefix { get; set; } = "[Connect Info] ";
        [JsonPropertyName("ConnectMessageWithGeo")] public string ConnectMessageWithGeo { get; set; } = "{PURPLE}[INFO] {DEFAULT}玩家 {GRAY}{PLAYERNAME} {DEFAULT} 进入了服务器, 来自 {GREEN}{GEOINFO} {LIME}[+]";
        [JsonPropertyName("ConnectMessageWithoutGeo")] public string ConnectMessageWithoutGeo { get; set; } = "{PURPLE}[INFO] {DEFAULT}玩家 {GRAY}{PLAYERNAME} {DEFAULT} 进入了服务器 {LIME}[+]";
        [JsonPropertyName("DisconnectMessage")] public string DisconnectMessage { get; set; } = "{PURPLE}[INFO] {DEFAULT}玩家 {GRAY}{PLAYERNAME} {DEFAULT} 离开了服务器 {LIGHTRED}[-]";
        [JsonPropertyName("ConsoleConnectMessageWithGeo")] public string ConsoleConnectMessageWithGeo { get; set; } = "玩家 {PLAYERNAME} 进入了服务器, 来自 {GEOINFO}";
        [JsonPropertyName("ConsoleConnectMessageWithoutGeo")] public string ConsoleConnectMessageWithoutGeo { get; set; } = "玩家 {PLAYERNAME} 进入了服务器";
    }

    [MinimumApiVersion(33)]
    public class ConnectInfo : BasePlugin, IPluginConfig<ConnectInfoConfig>
    {
        public override string ModuleName => "Connect Info";
        public override string ModuleVersion => "v1.0.5";
        public override string ModuleAuthor => "gleb_khlebov";

        public override string ModuleDescription => "Information about the player's location when connecting to chat and console";

        public ConnectInfoConfig Config { get; set; }

        public void OnConfigParsed(ConnectInfoConfig config)
        {
            Config = config;
        }

        public override void Load(bool hotReload)
        {
            Log("Connect Info loaded");
        }

        [GameEventHandler]
        public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;

            if (player is null || !player.IsValid || player.IsBot || player.IsHLTV || player.SteamID.ToString().Length != 17)
                return HookResult.Continue;

            var playerName = player.PlayerName;

            String consoleLogMessage;
            String serverChatMessage;

            consoleLogMessage =
                ReplaceMessageTags(Config.ConsoleConnectMessageWithoutGeo, playerName, String.Empty);
            serverChatMessage =
                ReplaceMessageTags(Config.ConnectMessageWithoutGeo, playerName, String.Empty);

            Log(consoleLogMessage);
            Server.NextFrame(() => Server.PrintToChatAll(serverChatMessage));
            return HookResult.Continue;
        }

        //[GameEventHandler]
        //public HookResult OnPlayerConnect(EventPlayerConnect @event, GameEventInfo info)
        //{
        //    CCSPlayerController? player = @event.Userid;

        //    if (player is null || !player.IsValid || player.IsBot || player.IsHLTV || player.SteamID.ToString().Length != 17)
        //        return HookResult.Continue;

        //    var geoInfo = GetGeoInfo(@event.Address.Split(':')[0]);
        //    var playerName = player.PlayerName;

        //    String consoleLogMessage;
        //    String serverChatMessage;

        //    if (!string.IsNullOrEmpty(geoInfo))
        //    {
        //        consoleLogMessage = ReplaceMessageTags(Config.ConsoleConnectMessageWithGeo, playerName, geoInfo);
        //        serverChatMessage = ReplaceMessageTags(Config.ConnectMessageWithGeo, playerName, geoInfo);
        //    }
        //    else
        //    {
        //        consoleLogMessage = 
        //            ReplaceMessageTags(Config.ConsoleConnectMessageWithoutGeo, playerName, String.Empty);
        //        serverChatMessage =
        //            ReplaceMessageTags(Config.ConnectMessageWithoutGeo, playerName, String.Empty);
        //    }
        //    Log(consoleLogMessage);
        //    Server.NextFrame(() => Server.PrintToChatAll(serverChatMessage));
        //    return HookResult.Continue;
        //}

        [GameEventHandler]
        public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;

            if (player is null || !player.IsValid || player.IsBot || player.IsHLTV || player.SteamID.ToString().Length != 17)
                return HookResult.Continue;

            var disconnectMessage = ReplaceMessageTags(Config.DisconnectMessage, player.PlayerName, String.Empty);
            Server.NextFrame(() => Server.PrintToChatAll(disconnectMessage));

            return HookResult.Continue;
        }

        private string GetGeoInfo(string ip)
        {
            try
            {
                var reader = new Reader(ModuleDirectory + "/../../shared/GeoLite2-City.mmdb");
                var parsedIp = IPAddress.Parse(ip);
                var data = reader.Find<Dictionary<string, object>>(parsedIp);
                var json = JsonConvert.SerializeObject(data);
                reader.Dispose();

                var geoInfo = JsonConvert.DeserializeObject<dynamic>(json)!;
                string city;
                string country;
                switch (Config.GeoLiteLanguage)
                {
                    case "ru":
                        country = geoInfo.country.names.ru;
                        city = geoInfo.city.names.ru;
                        break;
                    case "de":
                        country = geoInfo.country.names.de;
                        city = geoInfo.city.names.de;
                        break;
                    case "es":
                        country = geoInfo.country.names.es;
                        city = geoInfo.city.names.es;
                        break;
                    case "ja":
                        country = geoInfo.country.names.ja;
                        city = geoInfo.city.names.ja;
                        break;
                    case "fr":
                        country = geoInfo.country.names.fr;
                        city = geoInfo.city.names.fr;
                        break;
                    case "en":
                        country = geoInfo.country.names.en;
                        city = geoInfo.city.names.en;
                        break;
                    default:
                        country = geoInfo.country.names.en;
                        city = geoInfo.city.names.en;
                        break;
                }

                string result = null!;
                if (!string.IsNullOrEmpty(country))
                {
                    result = country;
                }

                if (!string.IsNullOrEmpty(result) && !string.IsNullOrEmpty(city) && Config.CityIncluded)
                {
                    result += ", " + city;
                }

                return result;
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }

            return null!;
        }

        private string ReplaceMessageTags(string message, string player, string geoinfo)
        {
            var replacedMessage = message
                .Replace("{PLAYERNAME}", player)
                .Replace("{GEOINFO}", geoinfo);

            replacedMessage = ReplaceColorTags(replacedMessage);

            return replacedMessage;
        }

        private string ReplaceColorTags(string input)
        {
            string[] colorPatterns =
            {
                "{DEFAULT}", "{WHITE}", "{DARKRED}", "{GREEN}", "{LIGHTYELLOW}", "{LIGHTBLUE}", "{OLIVE}", "{LIME}",
                "{RED}", "{LIGHTPURPLE}", "{PURPLE}", "{GREY}", "{YELLOW}", "{GOLD}", "{SILVER}", "{BLUE}", "{DARKBLUE}",
                "{BLUEGREY}", "{MAGENTA}", "{LIGHTRED}", "{ORANGE}"
            };
            string[] colorReplacements =
            {
                $"{ChatColors.Default}", $"{ChatColors.White}", $"{ChatColors.DarkRed}", $"{ChatColors.Green}",
                $"{ChatColors.LightYellow}", $"{ChatColors.LightBlue}", $"{ChatColors.Olive}", $"{ChatColors.Lime}",
                $"{ChatColors.Red}", $"{ChatColors.LightPurple}", $"{ChatColors.Purple}", $"{ChatColors.Grey}",
                $"{ChatColors.Yellow}", $"{ChatColors.Gold}", $"{ChatColors.Silver}", $"{ChatColors.Blue}",
                $"{ChatColors.DarkBlue}", $"{ChatColors.BlueGrey}", $"{ChatColors.Magenta}", $"{ChatColors.LightRed}",
                $"{ChatColors.Orange}"
            };

            for (var i = 0; i < colorPatterns.Length; i++)
                input = input.Replace(colorPatterns[i], colorReplacements[i]);

            return input;
        }

        private void Log(string message)
        {
            Console.BackgroundColor = ConsoleColor.DarkGray;
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.WriteLine(Config.LogPrefix + message);
            Console.ResetColor();
        }
    }
}