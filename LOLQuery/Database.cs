using System;
using System.Data;
using System.Collections;
using System.Linq;
using System.Text;
using MySql.Data;
using MySql.Data.MySqlClient;

using LibOfLegends;
using com.riotgames.platform.statistics;

namespace LOLQuery
{
    class Database
    {
        public MySqlConnection conn;
        public bool connected;

        public Database()
        {
            connected = false;
            String myConnectionString = "server=localhost;"
                + "uid=root;"
                + "pwd=passabola3907;"
                + "database=loldata;";

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

        public string[] allPlayers()
        {
            // Database Constants
            const int summonerValueId = 2;

            MySqlCommand cmd = new MySqlCommand();
            MySqlDataReader reader;
            cmd.Connection = conn;

            cmd.CommandText = "SELECT COUNT(*) FROM players";
            int numPlayers = int.Parse(cmd.ExecuteScalar().ToString());

            string[] summoners = new string[numPlayers];

            cmd.CommandText = "SELECT * FROM players";
            reader = cmd.ExecuteReader();

            int cnt = 0;
            while (reader.Read())
            {
                summoners[cnt++] = reader.GetValue(summonerValueId).ToString();
            }
            reader.Close();

            return summoners;
        }

        public int getMatch(int teamAId, int teamBId)
        {
            /** TODO: need to make sure there isn't a similar match in other tourney */
            MySqlCommand cmd = new MySqlCommand();
            MySqlDataReader reader;
            int dbAId;
            int dbBId;
            int ret;

            cmd.Connection = conn;
            cmd.CommandText = "SELECT * FROM matches WHERE active = \"1\"";
            reader = cmd.ExecuteReader();

            if (!reader.HasRows)
            {
                reader.Close();
                return 0;
            }

            while (reader.Read())
            {
                dbAId = int.Parse(reader.GetValue(2).ToString());
                dbBId = int.Parse(reader.GetValue(3).ToString());

                if ((dbAId == teamAId && dbBId == teamBId) || (dbAId == teamBId && dbBId == teamAId))
                {
                    ret = int.Parse(reader.GetValue(0).ToString());
                    reader.Close();
                    return ret;
                }
            }
            reader.Close();

            return 0;
        }

        public int getTeamId(int summoner)
        {

            MySqlCommand cmd = new MySqlCommand();
            MySqlDataReader reader;
            int teamId = 0;

            cmd.Connection = conn;
            cmd.CommandText = "SELECT `team` FROM players WHERE summonerId = \"" + summoner + "\" LIMIT 1";
            reader = cmd.ExecuteReader();

            if (!reader.HasRows)
            {
                reader.Close();
                return 0;
            }
            reader.Read();

            teamId = int.Parse(reader.GetValue(0).ToString());

            reader.Close();

            return teamId;
        }

        public int getOpponent(int matchId, int teamId)
        {
            const int teamAValueId = 2;
            const int teamBValueId = 3;

            int retId = 0;

            MySqlCommand cmd = new MySqlCommand();
            MySqlDataReader reader;

            cmd.Connection = conn;
            cmd.CommandText = "SELECT * FROM matches WHERE id = \"" + matchId + "\" LIMIT 1";
            reader = cmd.ExecuteReader();

            if (!reader.HasRows)
            {
                reader.Close();
                return 0;
            }
            reader.Read();

            if ((retId = int.Parse(reader.GetValue(teamAValueId).ToString())) == teamId)
            {
                retId = int.Parse(reader.GetValue(teamBValueId).ToString());
            }

            reader.Close();

            return retId;
        }

        public bool gameExists(int rGameId)
        {
            MySqlCommand cmd = new MySqlCommand();
            cmd.Connection = conn;

            cmd.CommandText = "SELECT COUNT(*) FROM games WHERE riotGameId = \"" + rGameId + "\"";
            if (int.Parse(cmd.ExecuteScalar().ToString()) == 0)
                return false;

            return true;
        }

        public int getGameId(int rGameId)
        {
            MySqlCommand cmd = new MySqlCommand();
            MySqlDataReader reader;
            cmd.Connection = conn;

            cmd.CommandText = "SELECT * FROM games WHERE riotGameId = \"" + rGameId + "\" LIMIT 1";

            reader = cmd.ExecuteReader();

            if (!reader.HasRows)
            {
                reader.Close();
                return 0;
            }
            reader.Read();

            int ret = int.Parse(reader.GetValue(0).ToString());
            reader.Close();

            return ret;
        }

        public int getAccountId(int summonerId)
        {
            MySqlCommand cmd = new MySqlCommand();
            MySqlDataReader reader;
            cmd.Connection = conn;

            cmd.CommandText = "SELECT `accountId` FROM players WHERE summonerId = \"" + summonerId + "\" LIMIT 1";
            reader = cmd.ExecuteReader();

            if (!reader.HasRows)
            {
                reader.Close();
                return 0;
            }
            reader.Read();

            int ret = int.Parse(reader.GetValue(0).ToString());
            reader.Close();

            return ret;
        }

        public string getSummonerName(int summonerId)
        {
            MySqlCommand cmd = new MySqlCommand();
            MySqlDataReader reader;
            cmd.Connection = conn;

            cmd.CommandText = "SELECT `summoner` FROM players WHERE summonerId = \"" + summonerId + "\" LIMIT 1";
            reader = cmd.ExecuteReader();

            if (!reader.HasRows)
            {
                reader.Close();
                return null;
            }
            reader.Read();

            string ret = reader.GetValue(0).ToString();
            reader.Close();

            return ret;
        }

        public int getPlayerId(int summonerId)
        {
            MySqlCommand cmd = new MySqlCommand();
            MySqlDataReader reader;
            cmd.Connection = conn;

            cmd.CommandText = "SELECT * FROM players WHERE summonerId = \"" + summonerId + "\" LIMIT 1";

            reader = cmd.ExecuteReader();

            if (!reader.HasRows)
            {
                reader.Close();
                return 0;
            }
            reader.Read();

            int ret = int.Parse(reader.GetValue(0).ToString());
            reader.Close();

            return ret;
        }

        public void addGame(int matchId, int blueTeam, int purpTeam, int winner, int rGameId, DateTime createDate) 
        {
            MySqlCommand cmd = new MySqlCommand();

            cmd.Connection = conn;
            cmd.CommandText = "INSERT INTO games SET " +
                "matchId = \"" + matchId + "\", " +
                "blueTeamId = \"" + blueTeam + "\", " +
                "purpleTeamId = \"" + purpTeam + "\", " +
                "winnerId = \"" + winner + "\", " +
                "riotGameId = \"" + rGameId + "\", " +
                "createDate = \"" + createDate.ToString("yyyy-MM-dd HH:mm") + "\"";

            cmd.ExecuteNonQuery();
        }

        public void addGameStats(int rGameId, int summonerId, PlayerGameStats pStats, GameResult stats)
        {
            MySqlCommand cmd = new MySqlCommand();

            cmd.Connection = conn;
            cmd.CommandText = "INSERT INTO gameStats SET " +
                "gameId = \"" + getGameId(rGameId) + "\", " +
                "playerId = \"" + getPlayerId(summonerId) + "\", " +
                "championId = \"" + pStats.championId + "\", " +
                "skinId = \"" + pStats.skinIndex + "\", " +
                "spell1Id = \"" + pStats.spell1 + "\", " +
                "spell2Id = \"" + pStats.spell2 + "\", " +
                "serverPing = \"" + pStats.userServerPing + "\", " +
                "kills = \"" + stats.Kills + "\", " +
                "deaths = \"" + stats.Deaths + "\", " +
                "assists = \"" + stats.Assists + "\", " +
                "level = \"" + stats.Level + "\", " +
                "minionsKilled = \"" + stats.MinionsKilled + "\", " +
                "neutralMinionsKilled = \"" + stats.NeutralMinionsKilled + "\", " +
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
                "turretsDestroyed = \"" + stats.TurretsDestroyed + "\", " +
                "inhibitorsDestroyed = \"" + stats.InhibitorsDestroyed + "\", " +
                "item0 = \"" + stats.Items[0] + "\", " +
                "item1 = \"" + stats.Items[1] + "\", " +
                "item2 = \"" + stats.Items[2] + "\", " +
                "item3 = \"" + stats.Items[3] + "\", " +
                "item4 = \"" + stats.Items[4] + "\", " +
                "item5 = \"" + stats.Items[5] + "\"";

            cmd.ExecuteNonQuery();
        }

        public void updateSummonerId(string summoner, int summId)
        {
            MySqlCommand cmd = new MySqlCommand();
            cmd.Connection = conn;
            cmd.CommandText = "UPDATE players SET accountId = \"" + summId + "\" WHERE summoner = \"" + summoner + "\"";

            cmd.ExecuteNonQuery();
        }

        public void updateMatch(int matchId, int rGameId, int blueTeam, int purpTeam, int winner)
        {
            int gameId = getGameId(rGameId);
            int teamA, teamB;
            int seriesTotal, seriesAScore, seriesBScore;
            int winnerToMatchId, loserToMatchId;
            int matchWinner = 0, loser = 0;

            MySqlCommand cmd = new MySqlCommand();
            MySqlDataReader reader;
            cmd.Connection = conn;

            cmd.CommandText = "SELECT `teamAId`, `teamBId`, `seriesTotal`, `seriesAScore`, `seriesBScore`, `winnerToMatchId`, `loserToMatchId` FROM matches WHERE id = \"" + matchId + "\"";
            reader = cmd.ExecuteReader();

            reader.Read();
            teamA = int.Parse(reader.GetValue(0).ToString());
            teamB = int.Parse(reader.GetValue(1).ToString());
            seriesTotal = int.Parse(reader.GetValue(2).ToString());
            seriesAScore = int.Parse(reader.GetValue(3).ToString());
            seriesBScore = int.Parse(reader.GetValue(4).ToString());
            winnerToMatchId = int.Parse(reader.GetValue(5).ToString());
            loserToMatchId = int.Parse(reader.GetValue(6).ToString());
            reader.Close();

            // Add 1 to score for winner
            if (teamA == winner)
            {
                seriesAScore++;
                loser = teamB;
            }
            else
            {
                seriesBScore++;
                loser = teamA;
            }

            // Determine if there is a match winner, based on Ceiling of seriesTotal / 2
            if (seriesAScore == int.Parse( Math.Ceiling(double.Parse(seriesTotal.ToString()) / 2.0).ToString() ))
                matchWinner = winner;

            if (seriesBScore == int.Parse( Math.Ceiling(double.Parse(seriesTotal.ToString()) / 2.0).ToString() ))
                matchWinner = winner;

            cmd.CommandText = "UPDATE matches SET winnerId = \"" + matchWinner + "\", seriesAScore = \"" + seriesAScore + "\", seriesBScore = \"" + seriesBScore + "\" WHERE id = \"" + matchId + "\"";
            cmd.ExecuteNonQuery();

            // Add gameId to correct game field
            if ((seriesAScore + seriesBScore) == 1)
            {
                cmd.CommandText = "UPDATE matches set seriesGame1 = \"" + gameId + "\" WHERE id = \"" + matchId + "\"";
                cmd.ExecuteNonQuery();
            }
            else if ((seriesAScore + seriesBScore) == 2)
            {
                cmd.CommandText = "UPDATE matches set seriesGame2 = \"" + gameId + "\" WHERE id = \"" + matchId + "\"";
                cmd.ExecuteNonQuery();
            }
            else
            {
                cmd.CommandText = "UPDATE matches set seriesGame3 = \"" + gameId + "\" WHERE id = \"" + matchId + "\"";
                cmd.ExecuteNonQuery();
            }

            // Update the next matches if there is a match winner
            if (matchWinner != 0)
            {
                if (winnerToMatchId > 1000)
                    newTeamToMatch(winnerToMatchId, winner);
                if (loserToMatchId > 1000)
                    newTeamToMatch(loserToMatchId, loser);
            }
        }

        public void newTeamToMatch(int matchId, int teamId) 
        {
            MySqlCommand cmd = new MySqlCommand();
            MySqlDataReader reader;
            cmd.Connection = conn;

            int teamAId, teamBId;

            cmd.CommandText = "SELECT `teamAId`, `teamBId` FROM matches WHERE id = \"" + matchId + "\"";

            reader = cmd.ExecuteReader();
            reader.Read();
            teamAId = int.Parse(reader.GetValue(0).ToString());
            teamBId = int.Parse(reader.GetValue(1).ToString());
            reader.Close();

            // Add teamId to new match, and activate match if 2 teams set
            if (teamAId == 0)
            {
                cmd.CommandText = "UPDATE matches set teamAId = \"" + teamId + "\" WHERE id = \"" + matchId + "\"";
            }
            else
            {
                cmd.CommandText = "UPDATE matches set teamBId = \"" + teamId + "\", active = \"1\" WHERE id = \"" + matchId + "\"";
            }
            cmd.ExecuteNonQuery();
        }

        public string[] watchSummonerNames()
        {
            // Database Constants
            const int captainValueId = 8;
            const int summonerValueId = 2;

            MySqlCommand cmd = new MySqlCommand();
            MySqlDataReader reader;
            cmd.Connection = conn;

            cmd.CommandText = "SELECT COUNT(*) FROM teams WHERE captain != \"0\"";
            int numTeams = int.Parse(cmd.ExecuteScalar().ToString());

            int[] userIds = new int[numTeams];
            string[] summoners = new string[numTeams];

            cmd.CommandText = "SELECT * FROM teams WHERE captain != \"0\"";
            reader = cmd.ExecuteReader();
            
            if (!reader.HasRows)
            {
                return null;
            }

            int cnt = 0;
            while (reader.Read())
            {
                userIds[cnt++] = int.Parse(reader.GetValue(captainValueId).ToString());
            }
            reader.Close();

            for (int i = 0; i < userIds.Count(); i++)
            {
                cmd.CommandText = "SELECT * FROM players WHERE id = \"" + userIds[i] + "\"";
                reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    summoners[i] = reader.GetValue(summonerValueId).ToString();
                }
                reader.Close();
            }

            return summoners;
        }
    }
}