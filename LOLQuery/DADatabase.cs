using System;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Windows.Forms;

using LibOfLegends;
using com.riotgames.platform.summoner;
using com.riotgames.platform.summoner.masterybook;
using com.riotgames.platform.summoner.spellbook;
using com.riotgames.platform.game;
using com.riotgames.platform.statistics;

using helpers;

namespace LOLQuery
{
    public class DADatabase
    {
        public MySqlConnection conn;
        public bool connected;

        public DADatabase()
        {
            connected = false;
            String myConnectionString = "server=localhost;"
                + "uid=root;"
                + "pwd=ferrari5606;"
                + "database=lolquery;";

            try
            {
                conn = new MySql.Data.MySqlClient.MySqlConnection();
                conn.ConnectionString = myConnectionString;
                conn.Open();
                connected = true;
            }
            catch (MySql.Data.MySqlClient.MySqlException ex)
            {
                string s = ex.ToString();
                Console.WriteLine("Can not connect to LOLData DB, please try again later.");
                connected = false;
            }
        }

        public bool gameStatsExists(long gameId)
        {
            MySqlCommand cmd = new MySqlCommand();
            cmd.Connection = conn;

            cmd.CommandText = "SELECT COUNT(*) FROM games WHERE riotGameId = \"" + gameId + "\"";

            if (int.Parse(cmd.ExecuteScalar().ToString()) == 0)
                return false;

            return true;
        }

        public bool gameStatsIncomplete(long gameId)
        {
            MySqlCommand cmd = new MySqlCommand();
            cmd.Connection = conn;

            cmd.CommandText = "SELECT COUNT(*) FROM games WHERE riotGameId = \"" + gameId + "\" AND prematchInfo = \"1\"";

            if (int.Parse(cmd.ExecuteScalar().ToString()) == 0)
                return false;

            return true;
        }

        public int getTotalTrackedSummoners()
        {
            MySqlCommand cmd = new MySqlCommand();
            cmd.Connection = conn;

            cmd.CommandText = "SELECT COUNT(*) FROM summoners WHERE track = \"1\"";

            return int.Parse(cmd.ExecuteScalar().ToString());
        }

        public bool getRiotLogins(ref List<RiotLogin> logins, ref List<RiotLogin> eLogins)
        {
            MySqlCommand cmd = new MySqlCommand();
            MySqlDataReader reader;
            cmd.Connection = conn;

            cmd.CommandText = "SELECT `username`, `password` FROM riotAccounts";
            reader = cmd.ExecuteReader();

            if (!reader.HasRows)
            {
                reader.Close();
                return false;
            }

            while (reader.Read())
            {
                logins.Add(new RiotLogin(reader.GetString(0), reader.GetString(1)));
                eLogins.Add(new RiotLogin("end" + reader.GetString(0), reader.GetString(1)));
            }
            reader.Close();

            return true;
        }

        public void removeLiveGame(long gameId)
        {
            MySqlCommand cmd = new MySqlCommand();
            cmd.Connection = conn;

            cmd.CommandText = "DELETE FROM liveGames WHERE gameId =\"" + gameId + "\"";
            cmd.ExecuteNonQuery();
        }

        public bool getTrackedSummoners(ref List<SummonerCrawler> summoners)
        {
            SummonerCrawler temp;
            MySqlCommand cmd = new MySqlCommand();
            MySqlDataReader reader;
            cmd.Connection = conn;

            cmd.CommandText = "SELECT `summoner_name`, `summoner_id`, `account_id` FROM summoners";
            reader = cmd.ExecuteReader();

            if (!reader.HasRows)
            {
                reader.Close();
                return false;
            }

            while (reader.Read())
            {
                temp = new SummonerCrawler(reader.GetString(0), reader.GetInt64(1), reader.GetInt64(2));
                summoners.Add(temp);
            }
            reader.Close();

            return true;
        }

        public bool getTrackedSummoners(ref List<SummonerCrawler> summoners, long startNdx)
        {
            SummonerCrawler temp;
            MySqlCommand cmd = new MySqlCommand();
            MySqlDataReader reader;
            cmd.Connection = conn;

            cmd.CommandText = "SELECT `summoner_name`, `summoner_id`, `account_id` FROM summoners WHERE id > \"" + startNdx + "\"";
            reader = cmd.ExecuteReader();

            if (!reader.HasRows)
            {
                reader.Close();
                return false;
            }

            while (reader.Read())
            {
                temp = new SummonerCrawler(reader.GetString(0), reader.GetInt64(1), reader.GetInt64(2));
                summoners.Add(temp);
            }
            reader.Close();

            return true;
        }

        public bool getTrackedSummoners(ref string[] summonerNames, ref long[] summonerIds)
        {
            MySqlCommand cmd = new MySqlCommand();
            MySqlDataReader reader;
            cmd.Connection = conn;

            cmd.CommandText = "SELECT `summoner_name`, `summoner_id` FROM summoners";
            reader = cmd.ExecuteReader();

            if (!reader.HasRows)
            {
                reader.Close();
                return false;
            }

            int ndx = 0;
            while (reader.Read())
            {
                summonerNames[ndx] = reader.GetString(0);
                summonerIds[ndx] = reader.GetInt64(1);

                ndx++;
            }
            reader.Close();

            return true;
        }

        private static int findChampPickByName(GameDTO game, string summName)
        {
            PlayerChampionSelectionDTO champSelection;
            champSelection = game.playerChampionSelections.Find(
                delegate(PlayerChampionSelectionDTO ch)
                {
                    return ch.summonerInternalName == summName;
                }
                );

            return champSelection.championId;
        }

        private bool isGameDraft(PlayerGameStats game)
        {
            if (game.queueType == "RANKED_SOLO_5x5")
                return true;

            if (game.queueType == "RANKED_TEAM_5x5")
                return true;

            if (game.queueType == "RANKED_TEAM_3x3")
                return true;

            return false;
        }

        private bool isGameDraft(GameDTO game)
        {
            if (game.queueTypeName == "RANKED_SOLO_5x5")
                return true;

            if (game.queueTypeName == "RANKED_TEAM_5x5")
                return true;

            if (game.queueTypeName == "RANKED_TEAM_3x3")
                return true;

            if (game.gameType == "PRACTICE_GAME" && (game.gameTypeConfigId == 2 || game.gameTypeConfigId == 6))
                return true;

            return false;
        }

        private bool isGameRanked(PlayerGameStats game)
        {
            if (game.queueType == "RANKED_SOLO_5x5")
                return true;

            if (game.queueType == "RANKED_TEAM_5x5")
                return true;

            if (game.queueType == "RANKED_TEAM_3x3")
                return true;

            return false;
        }

        private bool isGameRanked(GameDTO game)
        {
            if (game.queueTypeName == "RANKED_SOLO_5x5")
                return true;

            if (game.queueTypeName == "RANKED_TEAM_5x5")
                return true;

            if (game.queueTypeName == "RANKED_TEAM_3x3")
                return true;

            return false;
        }

        // *******
        // TODO: Add summoner ID and account ID to new summoners.
        public void addInProgressGame(PlatformGameLifecycleDTO specGame, long accountId)
        {
            MySqlCommand cmd = new MySqlCommand();
            cmd.Connection = conn;
            MySqlCommand eCmd = new MySqlCommand();
            eCmd.Connection = conn;
            MySqlDataReader reader;

            GameDTO game;
            int curElo = 0;
            int ndx, i;
            long[] teammate = new long[4];
            long[] opponent = new long[5];
            int[] bans = new int[6];
            int[] picks = new int[10];
            long[] pickOrder = new long[10];
            DateTime date = DateTime.Now;

            // Set GameDTO variable
            game = specGame.game;

            // Do nothing if duplicate
            eCmd.CommandText = "SELECT count(*) FROM games WHERE riotGameId = \"" + game.id + "\"";
            if (int.Parse(eCmd.ExecuteScalar().ToString()) != 0)
                return;

            // Don't add practice games, or games that don't count
            if (game.gameType == "PRACTICE_GAME")
                return;

            // Get Current Elo of Ranked games
            if (isGameRanked(game)) {
                if (game.queueTypeName == "RANKED_SOLO_5x5")
                    eCmd.CommandText = "SELECT `solo5x5_elo` FROM summoners WHERE account_id = \"" + accountId + "\" LIMIT 1";
                else if (game.queueTypeName == "RANKED_TEAM_5x5")
                    eCmd.CommandText = "SELECT `team5x5_elo` FROM summoners WHERE account_id = \"" + accountId + "\" LIMIT 1";
                else if (game.queueTypeName == "RANKED_TEAM_3x3")
                    eCmd.CommandText = "SELECT `team3x3_elo` FROM summoners WHERE account_id = \"" + accountId + "\" LIMIT 1";

                reader = eCmd.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        curElo = reader.GetInt32(0);
                    }
                }
                else
                {
                    curElo = 0;
                }
                reader.Close();
            }

            // If game is draft mode, sort pick order by draft order
            if (isGameDraft(game))
            {
                // Find pick order and champ picks for BLUE TEAM
                int tmpNdx;
                int three = 0;
                int five = 0;
                for (i = 0; i < game.teamOne.Count(); i++)
                {
                    if (game.teamOne[i].pickTurn == 3)
                    {
                        tmpNdx = (game.teamOne[i].pickTurn + (three++));
                        picks[tmpNdx] = findChampPickByName(game, game.teamOne[i].summonerInternalName);
                    }
                    else if (game.teamOne[i].pickTurn == 5)
                    {
                        tmpNdx = (game.teamOne[i].pickTurn + 2 + (five++));
                        picks[tmpNdx] = findChampPickByName(game, game.teamOne[i].summonerInternalName);
                    }
                    else
                    {
                        tmpNdx = (game.teamOne[i].pickTurn - 1);
                        picks[tmpNdx] = findChampPickByName(game, game.teamOne[i].summonerInternalName);
                    }
                }
                // Find pick order and champ picks for PURPLE TEAM
                int two = 0;
                int four = 0;
                for (i = 0; i < game.teamTwo.Count(); i++)
                {
                    if (game.teamTwo[i].pickTurn == 2)
                    {
                        tmpNdx = (game.teamTwo[i].pickTurn - 1 + (two++));
                        picks[tmpNdx] = findChampPickByName(game, game.teamTwo[i].summonerInternalName);
                    }
                    else if (game.teamTwo[i].pickTurn == 4)
                    {
                        tmpNdx = (game.teamTwo[i].pickTurn + 1 + (four++));
                        picks[tmpNdx] = findChampPickByName(game, game.teamTwo[i].summonerInternalName);
                    }
                    else
                    {
                        tmpNdx = (game.teamTwo[i].pickTurn + 3);
                        picks[tmpNdx] = findChampPickByName(game, game.teamTwo[i].summonerInternalName);
                    }
                }
            }
            // If game is not draft mode sort normally
            else
            {
                for (i = 0; i < game.teamOne.Count(); i++)
                {
                    if (i == 0)
                        picks[0] = findChampPickByName(game, game.teamOne[i].summonerInternalName);
                    if (i == 1)
                        picks[3] = findChampPickByName(game, game.teamOne[i].summonerInternalName);
                    if (i == 2)
                        picks[4] = findChampPickByName(game, game.teamOne[i].summonerInternalName);
                    if (i == 3)
                        picks[7] = findChampPickByName(game, game.teamOne[i].summonerInternalName);
                    if (i == 4)
                        picks[8] = findChampPickByName(game, game.teamOne[i].summonerInternalName);
                }

                for (i = 0; i < game.teamTwo.Count(); i++)
                {
                    if (i == 0)
                        picks[1] = findChampPickByName(game, game.teamTwo[i].summonerInternalName);
                    if (i == 1)
                        picks[2] = findChampPickByName(game, game.teamTwo[i].summonerInternalName);
                    if (i == 2)
                        picks[5] = findChampPickByName(game, game.teamTwo[i].summonerInternalName);
                    if (i == 3)
                        picks[6] = findChampPickByName(game, game.teamTwo[i].summonerInternalName);
                    if (i == 4)
                        picks[9] = findChampPickByName(game, game.teamTwo[i].summonerInternalName);
                }
            }

            // Determine Ban Order
            for (ndx = 0; ndx < game.bannedChampions.Count(); ndx++)
            {
                bans[(game.bannedChampions[ndx].pickTurn - 1)] = game.bannedChampions[ndx].championId;
            }

            // Do nothing if duplicate
            eCmd.CommandText = "SELECT count(*) FROM games WHERE riotGameId = \"" + game.id + "\"";
            if (int.Parse(eCmd.ExecuteScalar().ToString()) != 0)
                return;

            if (game.queueTypeName == "RANKED_TEAM_3x3" || game.queueTypeName == "NORMAL_3x3")
            {
                // Insert into DB
                cmd.CommandText = "INSERT INTO games SET " +
                    "riotGameId = \"" + game.id + "\", " +
                    "reportAccountId = \"" + accountId + "\", " +
                    "gameMode = \"" + game.gameMode + "\", " +
                    "gameType = \"" + game.gameType + "\", " +
                    "gameTypeConfigId = \"" + game.gameTypeConfigId + "\", " +
                    "gameMapId = \"" + game.mapId + "\", " +
                    "createDate = \"" + date.ToString("yyyy-MM-dd HH:mm:ss") + "\", " +
                    "subType = \"" + game.queueTypeName + "\", " +
                    "gameName = \"" + game.name + "\", " +
                    "prematchInfo = \"" + 1 + "\", " +
                    "bluePlayer0 = \"" + game.teamOne.ElementAt(0).summonerId + "\", " +
                    "bluePlayer1 = \"" + game.teamOne.ElementAt(1).summonerId + "\", " +
                    "bluePlayer2 = \"" + game.teamOne.ElementAt(2).summonerId + "\", " +
                    "bluePlayer3 = \"" + 0 + "\", " +
                    "bluePlayer4 = \"" + 0 + "\", " +
                    "purplePlayer0 = \"" + game.teamTwo.ElementAt(0).summonerId + "\", " +
                    "purplePlayer1 = \"" + game.teamTwo.ElementAt(1).summonerId + "\", " +
                    "purplePlayer2 = \"" + game.teamTwo.ElementAt(2).summonerId + "\", " +
                    "purplePlayer3 = \"" + 0 + "\", " +
                    "purplePlayer4 = \"" + 0 + "\", " +
                    "ban0 = \"" + bans[0] + "\", " +
                    "ban1 = \"" + bans[1] + "\", " +
                    "ban2 = \"" + bans[2] + "\", " +
                    "ban3 = \"" + bans[3] + "\", " +
                    "ban4 = \"" + bans[4] + "\", " +
                    "ban5 = \"" + bans[5] + "\", " +
                    "pick0 = \"" + picks[0] + "\", " +
                    "pick1 = \"" + picks[1] + "\", " +
                    "pick2 = \"" + picks[2] + "\", " +
                    "pick3 = \"" + picks[3] + "\", " +
                    "pick4 = \"" + picks[4] + "\", " +
                    "pick5 = \"" + picks[5] + "\", " +
                    "pick6 = \"" + picks[6] + "\", " +
                    "pick7 = \"" + picks[7] + "\", " +
                    "pick8 = \"" + picks[8] + "\", " +
                    "pick9 = \"" + picks[9] + "\"";

                cmd.ExecuteNonQuery();
            }
            else
            {

                // Insert into DB
                cmd.CommandText = "INSERT INTO games SET " +
                    "riotGameId = \"" + game.id + "\", " +
                    "reportAccountId = \"" + accountId + "\", " +
                    "gameMode = \"" + game.gameMode + "\", " +
                    "gameType = \"" + game.gameType + "\", " +
                    "gameTypeConfigId = \"" + game.gameTypeConfigId + "\", " +
                    "gameMapId = \"" + game.mapId + "\", " +
                    "createDate = \"" + date.ToString("yyyy-MM-dd HH:mm:ss") + "\", " +
                    "subType = \"" + game.queueTypeName + "\", " +
                    "gameName = \"" + game.name + "\", " +
                    "prematchInfo = \"" + 1 + "\", " +
                    "bluePlayer0 = \"" + game.teamOne.ElementAt(0).summonerId + "\", " +
                    "bluePlayer1 = \"" + game.teamOne.ElementAt(1).summonerId + "\", " +
                    "bluePlayer2 = \"" + game.teamOne.ElementAt(2).summonerId + "\", " +
                    "bluePlayer3 = \"" + game.teamOne.ElementAt(3).summonerId + "\", " +
                    "bluePlayer4 = \"" + game.teamOne.ElementAt(4).summonerId + "\", " +
                    "purplePlayer0 = \"" + game.teamTwo.ElementAt(0).summonerId + "\", " +
                    "purplePlayer1 = \"" + game.teamTwo.ElementAt(1).summonerId + "\", " +
                    "purplePlayer2 = \"" + game.teamTwo.ElementAt(2).summonerId + "\", " +
                    "purplePlayer3 = \"" + game.teamTwo.ElementAt(3).summonerId + "\", " +
                    "purplePlayer4 = \"" + game.teamTwo.ElementAt(4).summonerId + "\", " +
                    "ban0 = \"" + bans[0] + "\", " +
                    "ban1 = \"" + bans[1] + "\", " +
                    "ban2 = \"" + bans[2] + "\", " +
                    "ban3 = \"" + bans[3] + "\", " +
                    "ban4 = \"" + bans[4] + "\", " +
                    "ban5 = \"" + bans[5] + "\", " +
                    "pick0 = \"" + picks[0] + "\", " +
                    "pick1 = \"" + picks[1] + "\", " +
                    "pick2 = \"" + picks[2] + "\", " +
                    "pick3 = \"" + picks[3] + "\", " +
                    "pick4 = \"" + picks[4] + "\", " +
                    "pick5 = \"" + picks[5] + "\", " +
                    "pick6 = \"" + picks[6] + "\", " +
                    "pick7 = \"" + picks[7] + "\", " +
                    "pick8 = \"" + picks[8] + "\", " +
                    "pick9 = \"" + picks[9] + "\"";

                cmd.ExecuteNonQuery();
            }

            cmd.CommandText = "INSERT INTO liveGames SET " +
                "gameId = \"" + game.id + "\", " +
                "participantStatus = \"" + specGame.game.statusOfParticipants + "\", " +
                "observable = \"" + game.spectatorsAllowed + "\", " +
                "observerDelay = \"" + game.spectatorDelay + "\", " +
                "observerServerIp = \"" + specGame.playerCredentials.observerServerIp + "\", " +
                "observerServerPort = \"" + specGame.playerCredentials.observerServerPort + "\", " +
                "observerEncryptionKey = \"" + specGame.playerCredentials.observerEncryptionKey + "\", " +
                "createDate = \"" + date.ToString("yyyy-MM-dd HH:mm:ss") + "\"";

            cmd.ExecuteNonQuery();
        }

        public void addOldGame(PlayerGameStats game, long accountId)
        {
            MySqlCommand cmd = new MySqlCommand();
            cmd.Connection = conn;
            MySqlCommand eCmd = new MySqlCommand();
            eCmd.Connection = conn;
            int[] picks = new int[10];
            DateTime date = DateTime.Now;

            int i;
            int blueNdx;
            long[] blueTeam = new long[5];
            int purpleNdx;
            long[] purpleTeam = new long[5];

            // Do nothing if duplicate
            eCmd.CommandText = "SELECT count(*) FROM games WHERE riotGameId = \"" + game.gameId + "\"";
            if (int.Parse(eCmd.ExecuteScalar().ToString()) != 0)
                return;

            blueNdx = 0;
            purpleNdx = 0;
            for (i = 0; i < game.fellowPlayers.Count; i++)
            {
                if (game.fellowPlayers.ElementAt(i).teamId == 100)
                {
                    if (blueNdx == 0)
                        picks[0] = game.fellowPlayers.ElementAt(i).championId;
                    if (blueNdx == 1)
                        picks[3] = game.fellowPlayers.ElementAt(i).championId;
                    if (blueNdx == 2)
                        picks[4] = game.fellowPlayers.ElementAt(i).championId;
                    if (blueNdx == 3)
                        picks[7] = game.fellowPlayers.ElementAt(i).championId;
                    if (blueNdx == 4)
                        picks[8] = game.fellowPlayers.ElementAt(i).championId;

                    blueTeam[blueNdx++] = game.fellowPlayers.ElementAt(i).summonerId;
                }
                else
                {
                    if (purpleNdx == 0)
                        picks[1] = game.fellowPlayers.ElementAt(i).championId;
                    if (purpleNdx == 1)
                        picks[2] = game.fellowPlayers.ElementAt(i).championId;
                    if (purpleNdx == 2)
                        picks[5] = game.fellowPlayers.ElementAt(i).championId;
                    if (purpleNdx == 3)
                        picks[6] = game.fellowPlayers.ElementAt(i).championId;
                    if (purpleNdx == 4)
                        picks[9] = game.fellowPlayers.ElementAt(i).championId;

                    purpleTeam[purpleNdx++] = game.fellowPlayers.ElementAt(i).summonerId;
                }
            }

            // Insert into DB
            cmd.CommandText = "INSERT INTO games SET " +
                "riotGameId = \"" + game.gameId + "\", " +
                "reportAccountId = \"" + accountId + "\", " +
                "gameMode = \"" + game.gameMode + "\", " +
                "gameType = \"" + game.gameType + "\", " +
                "gameTypeConfigId = \"" + 1 + "\", " +
                "gameMapId = \"" + game.gameMapId + "\", " +
                "createDate = \"" + date.ToString("yyyy-MM-dd HH:mm:ss") + "\", " +
                "subType = \"" + game.queueType + "\", " +
                "gameName = \"" + "oldgame" + "\", " +
                "prematchInfo = \"" + 0 + "\", " +
                "bluePlayer0 = \"" + blueTeam[0] + "\", " +
                "bluePlayer1 = \"" + blueTeam[1] + "\", " +
                "bluePlayer2 = \"" + blueTeam[2] + "\", " +
                "bluePlayer3 = \"" + blueTeam[3] + "\", " +
                "bluePlayer4 = \"" + blueTeam[4] + "\", " +
                "purplePlayer0 = \"" + purpleTeam[0] + "\", " +
                "purplePlayer1 = \"" + purpleTeam[0] + "\", " +
                "purplePlayer2 = \"" + purpleTeam[0] + "\", " +
                "purplePlayer3 = \"" + purpleTeam[0] + "\", " +
                "purplePlayer4 = \"" + purpleTeam[0] + "\", " +
                "pick0 = \"" + picks[0] + "\", " +
                "pick1 = \"" + picks[1] + "\", " +
                "pick2 = \"" + picks[2] + "\", " +
                "pick3 = \"" + picks[3] + "\", " +
                "pick4 = \"" + picks[4] + "\", " +
                "pick5 = \"" + picks[5] + "\", " +
                "pick6 = \"" + picks[6] + "\", " +
                "pick7 = \"" + picks[7] + "\", " +
                "pick8 = \"" + picks[8] + "\", " +
                "pick9 = \"" + picks[9] + "\"";

            cmd.ExecuteNonQuery();

            GameResult gameRes = new GameResult(game);
            addOldGameStats(game.gameId, accountId, game, gameRes);
        }

        public void updateEndGame(long gameId, long accountId)
        {
            MySqlCommand cmd = new MySqlCommand();
            cmd.Connection = conn;

            DateTime time = DateTime.Now;

            cmd.CommandText = "UPDATE games SET " +
                "reportAccountId = \"" + accountId + "\", " +
                "endDate = \"" + time.ToString("yyyy-MM-dd HH:mm:ss") + "\", " +
                "prematchInfo = \"" + 0 + "\" WHERE riotGameId = \"" + gameId + "\"";

            cmd.ExecuteNonQuery();
        }

        public void addGameStats(long accountId, PlayerGameStats pStats, GameResult stats, PlayerLifeTimeStats lifeStats, string summonerName)
        {
            MySqlCommand cmd = new MySqlCommand();
            cmd.Connection = conn;
            MySqlDataReader reader;
            MySqlCommand eCmd = new MySqlCommand();
            eCmd.Connection = conn;
            string neutralMinions, turretsDest, inhibsDest;

            int oldElo = 0, eloChange = 0, newElo = 0;
            
            // Get Current Elo of Ranked games
            if (isGameRanked(pStats) && lifeStats != null)
            {
                if (pStats.queueType == "RANKED_SOLO_5x5")
                    eCmd.CommandText = "SELECT `solo5x5_elo` FROM summoners WHERE account_id = \"" + accountId + "\" LIMIT 1";
                else if (pStats.queueType == "RANKED_TEAM_5x5")
                    eCmd.CommandText = "SELECT `team5x5_elo` FROM summoners WHERE account_id = \"" + accountId + "\" LIMIT 1";
                else if (pStats.queueType == "RANKED_TEAM_3x3")
                    eCmd.CommandText = "SELECT `team3x3_elo` FROM summoners WHERE account_id = \"" + accountId + "\" LIMIT 1";

                reader = eCmd.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        oldElo = reader.GetInt32(0);
                    }
                }
                else
                {
                    oldElo = 0;
                }
                reader.Close();

                newElo = findPlayerStatSummaryType(lifeStats.playerStatSummaries.playerStatSummarySet, pStats.queueType).rating;

                if (oldElo == 400)
                    oldElo = 0;

                if (newElo == 400)
                    newElo = 0;

                if (oldElo != 0 && newElo != 0)
                {
                    eloChange = newElo - oldElo;
                }
            }

            if (stats.TurretsDestroyed == null)
                turretsDest = "0";
            else
                turretsDest = stats.TurretsDestroyed.ToString();

            if (stats.InhibitorsDestroyed == null)
                inhibsDest = "0";
            else
                inhibsDest = stats.InhibitorsDestroyed.ToString();

            if (stats.NeutralMinionsKilled == null)
                neutralMinions = "0";
            else
                neutralMinions = stats.NeutralMinionsKilled.ToString();

            cmd.CommandText = "INSERT INTO gamestats SET " +
                "riotGameId = \"" + pStats.gameId + "\", " +
                "accountId = \"" + accountId + "\", " +
                "championId = \"" + pStats.championId + "\", " +
                "skinId = \"" + pStats.skinIndex + "\", " +
                "spell1Id = \"" + pStats.spell1 + "\", " +
                "spell2Id = \"" + pStats.spell2 + "\", " +
                "summonerLevel = \"" + pStats.level + "\", " +
                "ipEarned = \"" + pStats.ipEarned + "\", " +
                "boostIpEarned = \"" + pStats.boostIpEarned + "\", " +
                "xpEarned = \"" + pStats.experienceEarned + "\", " +
                "boostXpEarned = \"" + pStats.boostXpEarned + "\", " +
                "premadeSize = \"" + pStats.premadeSize + "\", " +
                "createDate = \"" + pStats.createDate.ToString("yyyy-MM-dd HH:mm") + "\", " +
                "afk = \"" + (pStats.afk ? "1" : "0") + "\", " +
                "leaver = \"" + (pStats.leaver ? "1" : "0") + "\", " +
                "invalid = \"" + (pStats.invalid ? "1" : "0") + "\", " +
                "win = \"" + (stats.Win ? "1" : "0") + "\", " +               // FIX
                "ranked = \"" + (pStats.ranked ? "1" : "0") + "\", " +
                "oldElo = \"" + oldElo + "\", " +
                "newElo = \"" + newElo + "\", " +
                "eloChange = \"" + eloChange + "\", " +
                "serverPing = \"" + pStats.userServerPing + "\", " +
                "kills = \"" + stats.Kills + "\", " +
                "deaths = \"" + stats.Deaths + "\", " +
                "assists = \"" + stats.Assists + "\", " +
                "level = \"" + stats.Level + "\", " +
                "minionsKilled = \"" + stats.MinionsKilled + "\", " +
                "neutralMinionsKilled = \"" + neutralMinions + "\", " +
                "goldEarned = \"" + stats.GoldEarned + "\", " +
                "magicDamageDealt = \"" + stats.MagicalDamageDealt + "\", " +
                "physicalDamageDealt = \"" + stats.PhysicalDamageDealt + "\", " +
                "totalDamageDealt = \"" + stats.TotalDamageDealt + "\", " +
                "magicDamageTaken = \"" + stats.MagicalDamageTaken + "\", " +
                "physicalDamageTaken = \"" + stats.PhysicalDamageTaken + "\", " +
                "totalDamageTaken = \"" + stats.TotalDamageTaken + "\", " +
                "totalHealingDone = \"" + stats.TotalHealingDone + "\", " +
                "largestMultiKill = \"" + stats.LargestMultiKill + "\", " +
                "largestKillingSpree = \"" + stats.LargestKillingSpree + "\", " +
                "timeSpentDead = \"" + stats.TimeSpentDead + "\", " +
                "turretsDestroyed = \"" + turretsDest + "\", " +
                "inhibitorsDestroyed = \"" + inhibsDest + "\", " +
                "item0 = \"" + stats.Items[0] + "\", " +
                "item1 = \"" + stats.Items[1] + "\", " +
                "item2 = \"" + stats.Items[2] + "\", " +
                "item3 = \"" + stats.Items[3] + "\", " +
                "item4 = \"" + stats.Items[4] + "\", " +
                "item5 = \"" + stats.Items[5] + "\"";

            cmd.ExecuteNonQuery();
            
            updateSummonerStats(accountId, lifeStats, pStats.summonerId, summonerName);
        }

        public void addSummoner(long summonerId, long accountId, string summonerName)
        {
            MySqlCommand cmd = new MySqlCommand();
            MySqlCommand cCmd = new MySqlCommand();
            MySqlDataReader read;
            cmd.Connection = conn;
            cCmd.Connection = conn;

            cCmd.CommandText = "SELECT * FROM summoners WHERE account_id = \"" + accountId + "\"";
            read = cCmd.ExecuteReader();

            if (read.HasRows)
            {
                read.Close();
                return;
            }
            read.Close();

            cmd.CommandText = "INSERT INTO summoners SET " +
                "summoner_id = \"" + summonerId + "\", " +
                "account_id = \"" + accountId + "\", " +
                "summoner_name = \"" + summonerName + "\"";

            cmd.ExecuteNonQuery();
        }

        public void updateSummonerStats(long accountId, PlayerLifeTimeStats lifeStats, long summonerId, string summonerName)
        {
            MySqlDataReader reader;
            MySqlCommand eCmd = new MySqlCommand();
            eCmd.Connection = conn;
            PlayerStatSummary rSolo5x5;
            PlayerStatSummary rTeam5x5;
            PlayerStatSummary normal5x5;
            PlayerStatSummary rteam3x3;
            long oldSummId = 0;

            eCmd.CommandText = "SELECT `summoner_id` FROM summoners WHERE account_id = \"" + accountId + "\" LIMIT 1";

            reader = eCmd.ExecuteReader();

            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    oldSummId = reader.GetInt32(0);
                }

                if (oldSummId != summonerId)
                {
                    reader.Close();
                    updateSummoner("summoner_id", summonerId.ToString(), accountId);
                    updateSummonerId(oldSummId.ToString(), accountId);
                }
                else
                {
                    reader.Close();
                }
            }
            else
            {
                reader.Close();
                addSummoner(summonerId, accountId, summonerName);
            }

            // TODO profile icon update
            //updateSummoner("summonerLevel", pSumm.summonerLevel.ToString(), accountId);
            //updateSummoner("profileIconId", pSumm.profileIconId.ToString(), accountId);

            if (lifeStats != null)
            {
                if ((rSolo5x5 = lifeStats.getRankedSolo5x5()) != null)
                {
                    updateSummoner("solo5x5_elo", rSolo5x5.rating.ToString(), accountId);
                    updateSummoner("solo5x5_wins", rSolo5x5.wins.ToString(), accountId);
                    updateSummoner("solo5x5_losses", rSolo5x5.losses.ToString(), accountId);
                    updateSummoner("solo5x5_maxElo", rSolo5x5.maxRating.ToString(), accountId);
                }

                if ((rTeam5x5 = lifeStats.getRankedTeam5x5()) != null)
                {
                    updateSummoner("team5x5_elo", rTeam5x5.rating.ToString(), accountId);
                    updateSummoner("team5x5_wins", rTeam5x5.wins.ToString(), accountId);
                    updateSummoner("team5x5_losses", rTeam5x5.losses.ToString(), accountId);
                    updateSummoner("team5x5_maxElo", rTeam5x5.maxRating.ToString(), accountId);
                }

                if ((normal5x5 = lifeStats.getNormal5x5()) != null)
                {
                    updateSummoner("normal5x5_elo", normal5x5.rating.ToString(), accountId);
                    updateSummoner("normal5x5_wins", normal5x5.wins.ToString(), accountId);
                    updateSummoner("normal5x5_losses", normal5x5.losses.ToString(), accountId);
                    updateSummoner("normal5x5_maxElo", normal5x5.maxRating.ToString(), accountId);
                }

                if ((rteam3x3 = lifeStats.getRankedTeam3x3()) != null)
                {
                    updateSummoner("team3x3_elo", normal5x5.rating.ToString(), accountId);
                    updateSummoner("team3x3_wins", normal5x5.wins.ToString(), accountId);
                    updateSummoner("team3x3_losses", normal5x5.losses.ToString(), accountId);
                    updateSummoner("team3x3_maxElo", normal5x5.maxRating.ToString(), accountId);
                }
            }
        }

        public void updatePreGame(PlayerGameStats stats, long summonerId, PlayerStatSummaries playerStats)
        {
            MySqlCommand cmd = new MySqlCommand();
            cmd.Connection = conn;
            PlayerStatSummary pStatSummary;
            GameResult gResult;
            int win;
            int ranked;
            int curElo = 0;
            int newElo = 0;
            int oldElo = 0;
            int eloChange = 0;

            gResult = new GameResult(stats);

            /** Get Old Elo */
            MySqlCommand eCmd = new MySqlCommand();
            MySqlDataReader reader;
            eCmd.Connection = conn;
            eCmd.CommandText = "SELECT `oldElo` FROM games WHERE riotGameId = \"" + stats.gameId + "\" AND summonerId = \"" + summonerId + "\" LIMIT 1";
            reader = eCmd.ExecuteReader();
            reader.Read();
            oldElo = reader.GetInt32(0);
            reader.Close();

            /** Get New Elo */
            if (stats.subType == "RANKED_SOLO_5x5" || stats.subType == "RANKED_TEAM_5x5" || stats.subType == "RANKED_TEAM_3x3") 
            {
                pStatSummary = findPlayerStatSummaryType(playerStats.playerStatSummarySet, stats.subType);
                if (pStatSummary != null)
                    newElo = pStatSummary.rating;

                /** Get Current Elo */
                if (stats.subType == "RANKED_SOLO_5x5")
                    eCmd.CommandText = "SELECT `solo5x5_elo` FROM summoners WHERE summoner_id = \"" + summonerId + "\" LIMIT 1";
                else if (stats.subType == "RANKED_TEAM_5x5")
                    eCmd.CommandText = "SELECT `team5x5_elo` FROM summoners WHERE summoner_id = \"" + summonerId + "\" LIMIT 1";
                else
                    eCmd.CommandText = "SELECT `team3x3_elo` FROM summoners WHERE summoner_id = \"" + summonerId + "\" LIMIT 1";
                reader = eCmd.ExecuteReader();
                reader.Read();
                curElo = reader.GetInt32(0);
                reader.Close();

                /** Just became ranked high enough to show ELO */
                if (oldElo == 0 && newElo != 0)
                {
                    // Estimate elo change of 12
                    eloChange = 12;
                    oldElo = newElo - eloChange;
                }
                else if (newElo == 0 && oldElo != 0)
                {
                    // Esimate elo change of 12
                    eloChange = 12;
                    newElo = oldElo - eloChange;
                }
                else if (oldElo == 0 && newElo == 0 && curElo != 0) 
                {
                    // Estimate elo change of 12
                    eloChange = 12;
                    oldElo = curElo;

                    if (gResult.Win)
                        newElo = curElo + eloChange;
                    else
                        newElo = curElo - eloChange;
                }
                else if (oldElo == 0 && newElo == 0)
                {
                }
                else 
                {
                    eloChange = newElo - oldElo;
                }
            }

            win = gResult.Win ? 1 : 0;
            ranked = stats.ranked ? 1 : 0;
            cmd.CommandText = "UPDATE games SET " +
                "gameType = \"" + stats.gameType + "\", " +
                "ipEarned = \"" + stats.ipEarned + "\", " +
                "boostIpEarned = \"" + stats.boostIpEarned + "\", " +
                "premadeSize = \"" + stats.premadeSize + "\", " +
                "createDate = \"" + stats.createDate.ToString("yyyy-MM-dd HH:mm") + "\", " +
                "ranked = \"" + ranked + "\", " +
                "afk = \"" + stats.afk + "\", " +
                "serverPing = \"" + stats.userServerPing + "\", " +
                "win = \"" + win + "\", " +
                "oldElo = \"" + oldElo + "\", " +
                "newElo = \"" + newElo + "\", " +
                "eloChange = \"" + eloChange + "\", " +
                "prematchInfo = \"" + 0 + "\" WHERE riotGameId = \"" + stats.gameId + "\" AND summonerId = \"" + summonerId + "\"";
            cmd.ExecuteNonQuery();

            /** Update summoners database with new ELO */
            if (stats.subType == "RANKED_SOLO_5x5")
            {
                cmd.CommandText = "UPDATE summoners SET " +
                    "solo5x5_elo = \"" + newElo + "\" WHERE summoner_id = \"" + summonerId + "\"";
                cmd.ExecuteNonQuery();
            }
            else if (stats.subType == "RANKED_TEAM_5x5")
            {
                cmd.CommandText = "UPDATE summoners SET " +
                    "team5x5_elo = \"" + newElo + "\" WHERE summoner_id = \"" + summonerId + "\"";
                cmd.ExecuteNonQuery();
            }
            else
            {
                cmd.CommandText = "UPDATE summoners SET " +
                    "team3x3_elo = \"" + newElo + "\" WHERE summoner_id = \"" + summonerId + "\"";
                cmd.ExecuteNonQuery();
            }
        }

        public List<long> getFellowPlayersIds(long gameId, long summonerId)
        {
            List<long> players = new List<long>();
            MySqlCommand cmd = new MySqlCommand();
            MySqlDataReader reader;
            cmd.Connection = conn;

            cmd.CommandText = "SELECT `teammate1`, `teammate2`, `teammate3`, `teammate4`, `opponent0`, `opponent1`, `opponent2`, `opponent3`, `opponent4` FROM games WHERE " + 
                "riotGameId = \"" + gameId + "\" AND summonerId = \"" + summonerId + "\"";
            reader = cmd.ExecuteReader();
            reader.Read();

            if (reader.GetInt64(0) != 0)
                players.Add(reader.GetInt64(0));

            if (reader.GetInt64(1) != 0)
                players.Add(reader.GetInt64(1));

            if (reader.GetInt64(2) != 0)
                players.Add(reader.GetInt64(2));

            if (reader.GetInt64(3) != 0)
                players.Add(reader.GetInt64(3));

            if (reader.GetInt64(4) != 0)
                players.Add(reader.GetInt64(4));

            if (reader.GetInt64(5) != 0)
                players.Add(reader.GetInt64(5));

            if (reader.GetInt64(6) != 0)
                players.Add(reader.GetInt64(6));

            if (reader.GetInt64(7) != 0)
                players.Add(reader.GetInt64(7));

            if (reader.GetInt64(8) != 0)
                players.Add(reader.GetInt64(8));

            reader.Close();

            return players;
        }

        private static PlayerStatSummary findPlayerStatSummaryType(List<PlayerStatSummary> summaries, string subType)
        {
            PlayerStatSummary psSummary;

            char[] removeChar = {'_'};
            string findSubType = subType.Replace("_", "");

            psSummary = summaries.Find(
                delegate(PlayerStatSummary pss)
                {
                    return pss.playerStatSummaryTypeString.ToUpper() == findSubType.ToUpper();
                }
            );

            return psSummary;
        }

        public void addOldGameStats(long rGameId, long accountId, PlayerGameStats pStats, GameResult stats)
        {
            MySqlCommand cmd = new MySqlCommand();

            string turretsDest;
            string inhibsDest;
            string neutralMinions;

            if (stats.TurretsDestroyed == null)
                turretsDest = "0";
            else
                turretsDest = stats.TurretsDestroyed.ToString();

            if (stats.InhibitorsDestroyed == null)
                inhibsDest = "0";
            else
                inhibsDest = stats.InhibitorsDestroyed.ToString();

            if (stats.NeutralMinionsKilled == null)
                neutralMinions = "0";
            else
                neutralMinions = stats.NeutralMinionsKilled.ToString();

            cmd.Connection = conn;
            cmd.CommandText = "INSERT INTO gamestats SET " +
                "riotGameId = \"" + rGameId + "\", " +
                "accountId = \"" + accountId + "\", " +
                "championId = \"" + pStats.championId + "\", " +
                "skinId = \"" + pStats.skinIndex + "\", " +
                "spell1Id = \"" + pStats.spell1 + "\", " +
                "spell2Id = \"" + pStats.spell2 + "\", " +
                "summonerLevel = \"" + pStats.level + "\", " +
                "ipEarned = \"" + pStats.ipEarned + "\", " +
                "boostIpEarned = \"" + pStats.boostIpEarned + "\", " +
                "xpEarned = \"" + pStats.experienceEarned + "\", " +
                "boostXpEarned = \"" + pStats.boostXpEarned + "\", " +
                "premadeSize = \"" + pStats.premadeSize + "\", " +
                "createDate = \"" + pStats.createDate.ToString("yyyy-MM-dd HH:mm") + "\", " +
                "afk = \"" + (pStats.afk ? "1" : "0") + "\", " +
                "leaver = \"" + (pStats.leaver ? "1" : "0") + "\", " +
                "invalid = \"" + (pStats.invalid ? "1" : "0") + "\", " +
                "win = \"" + (stats.Win ? "1" : "0") + "\", " +               // FIX
                "ranked = \"" + (pStats.ranked ? "1" : "0") + "\", " +
                "oldElo = \"" + 0 + "\", " +
                "newElo = \"" + 0 + "\", " +
                "eloChange = \"" + 0 + "\", " +
                "serverPing = \"" + pStats.userServerPing + "\", " +
                "kills = \"" + stats.Kills + "\", " +
                "deaths = \"" + stats.Deaths + "\", " +
                "assists = \"" + stats.Assists + "\", " +
                "level = \"" + stats.Level + "\", " +
                "minionsKilled = \"" + stats.MinionsKilled + "\", " +
                "neutralMinionsKilled = \"" + neutralMinions + "\", " +
                "goldEarned = \"" + stats.GoldEarned + "\", " +
                "magicDamageDealt = \"" + stats.MagicalDamageDealt + "\", " +
                "physicalDamageDealt = \"" + stats.PhysicalDamageDealt + "\", " +
                "totalDamageDealt = \"" + stats.TotalDamageDealt + "\", " +
                "magicDamageTaken = \"" + stats.MagicalDamageTaken + "\", " +
                "physicalDamageTaken = \"" + stats.PhysicalDamageTaken + "\", " +
                "totalDamageTaken = \"" + stats.TotalDamageTaken + "\", " +
                "totalHealingDone = \"" + stats.TotalHealingDone + "\", " +
                "largestMultiKill = \"" + stats.LargestMultiKill + "\", " +
                "largestKillingSpree = \"" + stats.LargestKillingSpree + "\", " +
                "timeSpentDead = \"" + stats.TimeSpentDead + "\", " +
                "turretsDestroyed = \"" + turretsDest + "\", " +
                "inhibitorsDestroyed = \"" + inhibsDest + "\", " +
                "item0 = \"" + stats.Items[0] + "\", " +
                "item1 = \"" + stats.Items[1] + "\", " +
                "item2 = \"" + stats.Items[2] + "\", " +
                "item3 = \"" + stats.Items[3] + "\", " +
                "item4 = \"" + stats.Items[4] + "\", " +
                "item5 = \"" + stats.Items[5] + "\"";

            cmd.ExecuteNonQuery();
        }

        #region Private Update Methods

        public void addSummonerId(string sumName, long sumId, long acctId)
        {
            MySqlCommand cmd = new MySqlCommand();
            cmd.Connection = conn;
            cmd.CommandText = "UPDATE summoners SET summoner_id = \"" + sumId + "\", account_id = \"" + acctId + "\" WHERE summoner_name = \"" + sumName + "\"";
            cmd.ExecuteNonQuery();
        }

        public void updateSummoner(string column, string value, long accountId)
        {
            MySqlCommand cmd = new MySqlCommand();
            cmd.Connection = conn;
            cmd.CommandText = "UPDATE summoners SET " + column + " = \"" + value + "\" WHERE account_id = \"" + accountId + "\"";
            cmd.ExecuteNonQuery();
        }

        public void updateSummonerId(string value, long accountId)
        {
            MySqlCommand eCmd = new MySqlCommand();
            MySqlDataReader reader;
            eCmd.Connection = conn;
            string temp;

            eCmd.CommandText = "SELECT `oldSummoner_ids` FROM summoners WHERE account_id = \"" + accountId + "\" LIMIT 1";

            reader = eCmd.ExecuteReader();
            reader.Read();

            temp = reader.GetString(0) + ";" + value;

            reader.Close();

            MySqlCommand cmd = new MySqlCommand();
            cmd.Connection = conn;
            cmd.CommandText = "UPDATE summoners SET oldSummoner_ids = \"" + temp + "\" WHERE account_id = \"" + accountId + "\"";
            cmd.ExecuteNonQuery();
        }

        public List<SummonerCrawler> findPreGames(long accountId, long summonerId, string summonerName)
        {
            MySqlCommand eCmd = new MySqlCommand();
            MySqlDataReader reader;
            eCmd.Connection = conn;
            SummonerCrawler temp;
            List<SummonerCrawler> retList = new List<SummonerCrawler>();

            eCmd.CommandText = "SELECT `riotGameId` FROM games WHERE reportAccountId = \"" + accountId + "\" AND prematchInfo = \"1\"";

            reader = eCmd.ExecuteReader();

            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    temp = new SummonerCrawler(summonerName, summonerId, accountId, reader.GetUInt32(0));
                    retList.Add(temp);
                }
            }

            reader.Close();

            return retList;
        }

        public void removePreGame(long gameId)
        {
            MySqlCommand cmd = new MySqlCommand();
            cmd.Connection = conn;

            cmd.CommandText = "DELETE FROM games WHERE riotGameId = \"" + gameId + "\"";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "DELETE FROM gamestats WHERE riotGameId = \"" + gameId + "\"";
            cmd.ExecuteNonQuery();
        }

        private void updateSummonerById(string column, string value, long id)
        {
            MySqlCommand cmd = new MySqlCommand();
            cmd.Connection = conn;
            cmd.CommandText = "UPDATE summoners SET " + column + " = \"" + value + "\" WHERE id = \"" + id + "\"";
            cmd.ExecuteNonQuery();
        }

        public void updateSummonerByName(string column, string value, string sumName)
        {
            MySqlCommand cmd = new MySqlCommand();
            cmd.Connection = conn;
            cmd.CommandText = "UPDATE summoners SET " + column + " = \"" + value + "\" WHERE summoner_name = \"" + sumName + "\"";
            cmd.ExecuteNonQuery();
        }

        #endregion
    }
}