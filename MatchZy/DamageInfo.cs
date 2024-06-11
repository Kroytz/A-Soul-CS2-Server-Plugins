using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;


namespace MatchZy
{

    public partial class MatchZy
    {

        private void InitPlayerDamageInfo()
        {
            foreach (var key in playerData.Keys) {
                if (!playerData[key].IsValid) continue;
                if (playerData[key].IsBot) continue;
                int attackerId = key;
                foreach (var key2 in playerData.Keys) {
                    if (key == key2) continue;
                    if (!playerData[key2].IsValid || playerData[key2].IsBot) continue;
                    if (playerData[key].TeamNum == playerData[key2].TeamNum) continue;
                    if (playerData[key].TeamNum == 2) {
                        if (playerData[key2].TeamNum != 3) continue;
                        int targetId = key2;
                        if (!playerDamageInfo.TryGetValue(attackerId, out var attackerInfo))
                            playerDamageInfo[attackerId] = attackerInfo = new Dictionary<int, DamagePlayerInfo>();

                        if (!attackerInfo.TryGetValue(targetId, out var targetInfo))
                            attackerInfo[targetId] = targetInfo = new DamagePlayerInfo();
                    } else if (playerData[key].TeamNum == 3) {
                        if (playerData[key2].TeamNum != 2) continue;
                        int targetId = key2;
                        if (!playerDamageInfo.TryGetValue(attackerId, out var attackerInfo))
                            playerDamageInfo[attackerId] = attackerInfo = new Dictionary<int, DamagePlayerInfo>();

                        if (!attackerInfo.TryGetValue(targetId, out var targetInfo))
                            attackerInfo[targetId] = targetInfo = new DamagePlayerInfo(); 
                    }
                }
            }

            usedHPFunction.Clear();
        }

		public Dictionary<int, Dictionary<int, DamagePlayerInfo>> playerDamageInfo = new Dictionary<int, Dictionary<int, DamagePlayerInfo>>();
		private void UpdatePlayerDamageInfo(EventPlayerHurt @event, int targetId)
		{
			int attackerId = (int)@event.Attacker.UserId!;
			if (!playerDamageInfo.TryGetValue(attackerId, out var attackerInfo))
				playerDamageInfo[attackerId] = attackerInfo = new Dictionary<int, DamagePlayerInfo>();

			if (!attackerInfo.TryGetValue(targetId, out var targetInfo))
				attackerInfo[targetId] = targetInfo = new DamagePlayerInfo();

			targetInfo.DamageHP += @event.DmgHealth;
			targetInfo.Hits++;
		}

        private void ShowDamageInfo()
        {
            try
            {
                HashSet<(int, int)> processedPairs = new HashSet<(int, int)>();

                foreach (var entry in playerDamageInfo)
                {
                    int attackerId = entry.Key;
                    foreach (var (targetId, targetEntry) in entry.Value)
                    {
                        if (processedPairs.Contains((attackerId, targetId)) || processedPairs.Contains((targetId, attackerId)))
                            continue;

                        // Access and use the damage information as needed.
                        int damageGiven = targetEntry.DamageHP;
                        int hitsGiven = targetEntry.Hits;
                        int damageTaken = 0;
                        int hitsTaken = 0;

                        if (playerDamageInfo.TryGetValue(targetId, out var targetInfo) && targetInfo.TryGetValue(attackerId, out var takenInfo))
                        {
                            damageTaken = takenInfo.DamageHP;
                            hitsTaken = takenInfo.Hits;
                        }

                        if (!playerData.ContainsKey(attackerId) || !playerData.ContainsKey(targetId)) continue;

                        var attackerController = playerData[attackerId];
                        var targetController = playerData[targetId];

                        if (attackerController != null && targetController != null)
                        {
                            if (!attackerController.IsValid || !targetController.IsValid) continue;
                            if (attackerController.Connected != PlayerConnectedState.PlayerConnected) continue;
                            if (targetController.Connected != PlayerConnectedState.PlayerConnected) continue;
                            if (!attackerController.PlayerPawn.IsValid || !targetController.PlayerPawn.IsValid) continue;
                            if (attackerController.PlayerPawn.Value == null || targetController.PlayerPawn.Value == null) continue;

                            int attackerHP = attackerController.PlayerPawn.Value.Health < 0 ? 0 : attackerController.PlayerPawn.Value.Health;
                            string attackerName = attackerController.PlayerName;

                            int targetHP = targetController.PlayerPawn.Value.Health < 0 ? 0 : targetController.PlayerPawn.Value.Health;
                            string targetName = targetController.PlayerName;

                            attackerController.PrintToChat($"{chatPrefix} {ChatColors.Green}To: [{damageGiven} / {hitsGiven} hits] From: [{damageTaken} / {hitsTaken} hits] - {targetName} - ({targetHP} hp){ChatColors.Default}");
                            targetController.PrintToChat($"{chatPrefix} {ChatColors.Green}To: [{damageTaken} / {hitsTaken} hits] From: [{damageGiven} / {hitsGiven} hits] - {attackerName} - ({attackerHP} hp){ChatColors.Default}");
                        }

                        // Mark this pair as processed to avoid duplicates.
                        processedPairs.Add((attackerId, targetId));
                    }
                }
                playerDamageInfo.Clear();
            }
            catch (Exception e)
            {
                Log($"[ShowDamageInfo FATAL] An error occurred: {e.Message}");
            }

        }

        public List<uint> usedHPFunction = new List<uint>();
        private void ShowDamageInfoForTeammates(CCSPlayerController? player)
        {
            if (player == null) return;
            if (usedHPFunction.Contains(player.Index))
            {
                player.PrintToChat($"{chatPrefix} 你本回合已经使用过报血功能了！");
                return;
            }

            bool hasResult = false;
            try
            {
                HashSet<(int, int)> processedPairs = new HashSet<(int, int)>();

                foreach (var entry in playerDamageInfo)
                {
                    int attackerId = entry.Key;
                    foreach (var (targetId, targetEntry) in entry.Value)
                    {
                        if (processedPairs.Contains((attackerId, targetId)) || processedPairs.Contains((targetId, attackerId)))
                            continue;

                        // Access and use the damage information as needed.
                        int damageGiven = targetEntry.DamageHP;
                        int hitsGiven = targetEntry.Hits;
                        int damageTaken = 0;
                        int hitsTaken = 0;

                        if (playerDamageInfo.TryGetValue(targetId, out var targetInfo) && targetInfo.TryGetValue(attackerId, out var takenInfo))
                        {
                            damageTaken = takenInfo.DamageHP;
                            hitsTaken = takenInfo.Hits;
                        }

                        if (!playerData.ContainsKey(attackerId) || !playerData.ContainsKey(targetId)) continue;

                        var attackerController = playerData[attackerId];
                        var targetController = playerData[targetId];

                        if (attackerController != null && targetController != null)
                        {
                            if (attackerController != player) continue;
                            if (!attackerController.IsValid || !targetController.IsValid) continue;
                            if (attackerController.Connected != PlayerConnectedState.PlayerConnected) continue;
                            if (targetController.Connected != PlayerConnectedState.PlayerConnected) continue;
                            if (!attackerController.PlayerPawn.IsValid || !targetController.PlayerPawn.IsValid) continue;
                            if (attackerController.PlayerPawn.Value == null || targetController.PlayerPawn.Value == null) continue;
                            if (damageGiven == 0) continue;

                            int attackerHP = attackerController.PlayerPawn.Value.Health < 0 ? 0 : attackerController.PlayerPawn.Value.Health;
                            string attackerName = attackerController.PlayerName;

                            int targetHP = targetController.PlayerPawn.Value.Health < 0 ? 0 : targetController.PlayerPawn.Value.Health;
                            string targetName = targetController.PlayerName;

                            if (targetHP <= 0) continue;

                            hasResult = true;
                            var teammates = Utilities.GetPlayers().Where(players => players.Team == attackerController.Team && players.Connected == PlayerConnectedState.PlayerConnected && players.IsValid).ToList();

                            foreach (var teammate in teammates)
                            {
                                teammate.PrintToChat($"{ChatColors.Purple}{player.PlayerName}{ChatColors.Yellow}: 我对 {targetName} 造成了 [{damageGiven} HP / {hitsGiven} 次] 伤害");
                            }
                        }

                        // Mark this pair as processed to avoid duplicates.
                        processedPairs.Add((attackerId, targetId));
                    }
                }
            }
            catch (Exception e)
            {
                Log($"[ShowDamageInfoForTeammates FATAL] An error occurred: {e.Message}");
            }

            if (!hasResult)
            {
                player.PrintToChat($"{chatPrefix} 你本回合没有对任何敌人造成伤害！");
                return;
            }

            usedHPFunction.Add(player.Index);
        }
    }

	public class DamagePlayerInfo
	{
		public int DamageHP { get; set; } = 0;
		public int Hits { get; set; } = 0;
	}
}
