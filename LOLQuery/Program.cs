using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Web;
using System.Diagnostics;

using LibOfLegends;
using Nil;

using agsXMPP;
using agsXMPP.protocol.client;
using agsXMPP.Collections;
using agsXMPP.protocol.iq.roster;

using System.Xml;
using System.IO;
using com.riotgames.platform.statistics;
using com.riotgames.platform.summoner;
using com.riotgames.platform.game;
using com.riotgames.platform.gameclient.domain;
using com.riotgames.platform.clientfacade.domain;

using helpers;

namespace LOLQuery
{
    class Program
    {
        private static string baseDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
        private static string ConfigurationFile = Path.Combine(baseDirectory, "Config/Configuration.xml");

        // Variable to force a end program on disconnect from pvpnet
        public static bool forceDC;

        // Attempt reconnect frequency in SECONDS
        private const int RECFREQ = 5;

        static void Main(string[] args)
        {
            // Set disconnect to false, to enable connection
            forceDC = false;

            //Database db;
            Configuration configuration;
            try
            {
                Serialiser<Configuration> serialiser = new Serialiser<Configuration>(ConfigurationFile);
                configuration = serialiser.Load();
            }
            catch (System.IO.FileNotFoundException)
            {
                Console.WriteLine("Unable to load configuration file \"" + ConfigurationFile + "\"");
                return;
            }
            catch (System.InvalidOperationException)
            {
                Console.WriteLine("Malformed configuration file");
                return;
            }

            // Check argument for program mode
            if (args.Length != 0)
            {
                switch (args[0])
                {
                    case "crawl":
                        crawlPvpnet(configuration);
                    break;

                    case "creatematch":
                        createMatch(configuration);
                    break;
                    case "addsummonerids":
                        addSummonerIDs(configuration);
                    break;
                    case "test":
                        test(configuration);
                    break;

                    case "test2":
                        RiotConnect pvpnet = new RiotConnect(configuration, "endwesa005", "baylife13");

                        while (!pvpnet.Connected) ;

                        List<long> fellows = new List<long>();
                        List<string> fellowNames = new List<string>();
                        RecentGames game = pvpnet.RPC.GetRecentGames(34022924);

                        for (int i = 0; i < game.gameStatistics.Count; i++)
                        {
                            if (game.gameStatistics.ElementAt(i).gameId == 675775152)
                            {
                                for (int j = 0; j < game.gameStatistics.ElementAt(i).fellowPlayers.Count; j++)
                                {
                                    fellows.Add(game.gameStatistics.ElementAt(i).fellowPlayers.ElementAt(j).summonerId);
                                }
                                break;
                            }
                        }

                        fellowNames = pvpnet.RPC.GetSummonerNames(fellows);
                        PublicSummoner summoner;
                        RecentGames gameStats;

                        for (int n = 0; n < fellowNames.Count; n++)
                        {
                            summoner = pvpnet.RPC.GetSummonerByName(fellowNames.ElementAt(n));
                            gameStats = pvpnet.RPC.GetRecentGames(summoner.acctId);

                            if (gameStats == null)
                                Console.WriteLine(summoner.internalName + " PROBLEM = NULL");
                            else
                                Console.WriteLine(summoner.internalName + " recent Games successful");
                        }

                        while (1 == 1) ;

                    break;

                }
            }
        }

        /** Command Methods */
        #region Command Methods

        private static void addSummonerIDs(Configuration config)
        {
            PublicSummoner summ;
            List<SummonerCrawler> list = new List<SummonerCrawler>();
            SummonerCrawler curSummoner;

            RiotConnect pvpnet = new RiotConnect(config, "wesa001", "baylife13");
            DADatabase db = new DADatabase();

            db.getTrackedSummoners(ref list);

            while (!pvpnet.Connected);

            for (int i = 0; i < list.Count; i++)
            {
                curSummoner = list.ElementAt(i);
                if (curSummoner.summonerId == 0 || curSummoner.accountId == 0)
                {
                    if ((summ = pvpnet.RPC.GetSummonerByName(curSummoner.summonerName)) != null)
                    {
                        db.updateSummonerByName("summoner_id", summ.summonerId.ToString(), curSummoner.summonerName);
                        db.updateSummonerByName("account_id", summ.acctId.ToString(), curSummoner.summonerName);
                        Console.Out.WriteLine("ADDED SUMMONER = " + curSummoner.summonerName);
                    }
                }
            }
        }

        private static void test(Configuration config)
        {
            RiotConnect pvpnet = new RiotConnect(config, "wesa001", "baylife13");
            DADatabase db = new DADatabase();

            while (!pvpnet.Connected);

            PublicSummoner summoner = pvpnet.RPC.summoner.GetSummonerByName("coupdegrace666");
            PlatformGameLifecycleDTO game = pvpnet.RPC.game.retrieveInProgressSpectatorGameInfo("coupdegrace666");
            db.addInProgressGame(game, summoner.summonerId);

            Console.Out.WriteLine("summ id: " + summoner.summonerId);

            while (1 == 1) ;
        }

        private static void createMatch(Configuration config)
        {
            /** Initialize PvP.net Server Connection */
            RiotConnect pvpnet = new RiotConnect(config, "wesa001", "baylife13");
            RiotChat chat = new RiotChat("wesa001", "baylife13");

            /** Wait for PvP.net Server to be connected */
            while (!pvpnet.Connected) ;

            PublicSummoner mysumm = pvpnet.RPC.GetSummonerByName("wesa001");
            PublicSummoner summ = pvpnet.RPC.GetSummonerByName("appak");
            //GameDTO game = pvpnet.RPC.game.createPracticeGame(new PracticeGameConfig("WESA MATCH 1", "test"));
            GameDTO game = pvpnet.RPC.game.joinGame(433787973, "hello");

            //Console.WriteLine("summ id " + summ.summonerId);

            chat.sendPresence();

            chat.inviteSummoner(summ.summonerId, 433787973, 1);

            while (1 == 1) ;
        }


        private static void crawlPvpnet(Configuration config)
        {
            RiotCrawl crawler = new RiotCrawl(config);
        }

        /** Method to crawl PvPnet for all the summoners being crawled */
        private static void crawlPvpnetss(Configuration config)
        {
            RiotConnect pvpnet;
            RiotChat chat;

            /** Persistent connection */
            while (!forceDC) 
            {
                /* Establish Connection to PvPNet */
                pvpnet = new RiotConnect(config, "darkactivestats", "baylife13");

                /** Wait for PvP.net Server connection */
                if (waitForPvpnet(pvpnet))
                {
                    chat = new RiotChat("darkactivestats", "baylife13");

                    /* Initialise the PvPNet Crawler */
                    //RiotCrawl crawler = new RiotCrawl(pvpnet, chat);
                }
            }
        }

        /** Method to wait for connection or keep trying reconnect */
        private static bool waitForPvpnet(RiotConnect pvpnet)
        {
            int time = 0;

            while (!pvpnet.Connected && time < RECFREQ)
            {
                Thread.Sleep(1000);
                time++;
            }

            if (time >= RECFREQ)
                return false;

            return true;
        }

        #endregion

        /** Other Methods */
        static void quit(RPCService RPC)
        {
            RPC.Disconnect();
            GC.Collect();
            Process.GetCurrentProcess().Kill();
        }

        static int CompareGames(PlayerGameStats x, PlayerGameStats y)
        {
            return -x.createDate.CompareTo(y.createDate);
        }

        /**

        static void tournamentWatcher(RiotConnect serv, string[] summoners)
        {
            int cnt, itr;
            RecentGames recGames;
            PublicSummoner summ;
            DateTime[] latestMatch = new DateTime[summoners.Count()];
            Database db = new Database();
            int matchId;

            itr = 0;
            while (serv.Connected)
            {
                Console.WriteLine("Starting to check summoners' recentgames in list");
                for (cnt = 0; cnt < summoners.Count(); cnt++)
                {
                    summ = serv.RPC.GetSummonerByName(summoners[cnt]);
                    recGames = serv.RPC.GetRecentGames(summ.acctId);
                    // Sort recent games by putting oldest game at bottom of list
                    recGames.gameStatistics.Sort(CompareGames);
                    // Sort recent games by putting oldest game at top of list if first iteration
                    if (itr == 0)
                        recGames.gameStatistics.Reverse();

                    foreach (var game in recGames.gameStatistics)
                    {
                        // If not first iteration and game is found to be same as latestMatch or older, then break
                        if (itr != 0 && DateTime.Compare(game.createDate, latestMatch[cnt]) <= 0)
                            break;

                        // Skip matches already checked by comparing dates
                        if (DateTime.Compare(game.createDate, latestMatch[cnt]) > 0)
                        {
                            // Skip matches that aren't custom, and gameMode is not Classic
                            // SKip invalid games
                            if (game.gameType == "PRACTICE_GAME" && game.gameMode == "CLASSIC" && !game.invalid)
                            {

                                if ((matchId = isTournamentMatch(game, summ.summonerId)) != 0)
                                {
                                    Console.WriteLine("Analyzing tournament game with ID " + game.gameId + ", for summoner ID " + summ.summonerId);
                                    analyzeTGame(summ.acctId, serv.RPC, game, matchId);
                                }
                            }
                            latestMatch[cnt] = game.createDate;
                        }
                    }
                }
                Console.WriteLine("Sleeping for 30 minutes...");
                Thread.Sleep(30 * 60 * 1000);
            }
        }

        static int[] getRiotAccountIds(RPCService RPC, string[] summoners)
        {
            int[] accountIds = new int[summoners.Count()];
            PublicSummoner tempSumm;

            for (int cnt = 0; cnt < summoners.Count(); cnt++) 
            {
                tempSumm = RPC.GetSummonerByName(summoners[cnt]);
                accountIds[cnt] = tempSumm.acctId;
            }
            

            return accountIds;
        }

        static int isTournamentMatch(PlayerGameStats game, int summoner)
        {
            Database db = new Database();
            int curTeam = 0;
            int eneTeam = 0;
            int matchId;

            // If summoner doesn't exist in database
            if ((curTeam = db.getTeamId(summoner)) == 0)
                return 0;

            foreach (var player in game.fellowPlayers) 
            {
                if (player.teamId == game.teamId)
                {
                    if (db.getTeamId(player.summonerId) != curTeam)
                        return 0;
                }
                else
                {
                    if (eneTeam == 0)
                    {
                        // If summoner doesn't exist in database
                        if ((eneTeam = db.getTeamId(player.summonerId)) == 0)
                            return 0;
                    }
                    else {
                        if (db.getTeamId(player.summonerId) != eneTeam)
                            return 0;
                    }
                }
            }

            // Check if a match exists in DB
            if ((matchId = db.getMatch(curTeam, eneTeam)) == 0)
                return 0;

            // Check if game already reported
            if (db.gameExists(game.gameId))
                return 0;

            return matchId;
        }

        static void analyzeTGame(int playerId, RPCService RPC, PlayerGameStats playerStats, int matchId)
        {
            Database db = new Database();
            int cnt;
            int pTeam, oTeam, blueTeam, purpTeam, winner;

            // Get reporting players team
            pTeam = db.getTeamId(playerStats.summonerId);

            // Get raw stats for reporting player
            GameResult pResult = new GameResult(playerStats);

            // Find opponent team
            oTeam = db.getOpponent(matchId, pTeam);

            // Find what team ids are on blue/purple team
            if (playerStats.teamId == 100)
            {
                blueTeam = pTeam;
                purpTeam = oTeam;
            }
            else
            {
                blueTeam = oTeam;
                purpTeam = pTeam;
            }

            // Find what team is winner
            if (pResult.Win)
                winner = pTeam;
            else
                winner = oTeam;

            // Add Game to the database
            db.addGame(matchId, blueTeam, purpTeam, winner, playerStats.gameId, playerStats.createDate);
            Console.WriteLine("Added game for MatchID: " + matchId);

            db.updateMatch(matchId, playerStats.gameId, blueTeam, purpTeam, winner);
            Console.WriteLine("Updated tournament match");

            // Add reporting player's game stats
            db.addGameStats(playerStats.gameId, playerStats.summonerId, playerStats, pResult);
            Console.WriteLine("Added gamestats for reporting player with summoner: " + db.getSummonerName(playerStats.summonerId));

            // Add rest of players' game stats
            RecentGames tempRG;
            GameResult tempGameRes;
            int gCnt;

            for (cnt = 0; cnt < playerStats.fellowPlayers.Count; cnt++)
            {
                tempRG = RPC.GetRecentGames(db.getAccountId(playerStats.fellowPlayers[cnt].summonerId));
                for (gCnt = 0; gCnt < tempRG.gameStatistics.Count; gCnt++)
                {
                    if (tempRG.gameStatistics[gCnt].gameId == playerStats.gameId)
                        break;
                }
                if (gCnt < tempRG.gameStatistics.Count)
                {
                    tempGameRes = new GameResult(tempRG.gameStatistics[gCnt]);

                    db.addGameStats(playerStats.gameId, tempRG.gameStatistics[gCnt].summonerId, tempRG.gameStatistics[gCnt], tempGameRes);
                    Console.WriteLine("Added gamestats for player with summoner: " +  db.getSummonerName(tempRG.gameStatistics[gCnt].summonerId));
                }
                else
                {
                    Console.WriteLine("Game stats not found for summoner: " +  db.getSummonerName(playerStats.fellowPlayers[cnt].summonerId));
                }
            }
        }

        static void processRecentGames(PublicSummoner summoner, RecentGames games)
        {
            games.gameStatistics.Sort(CompareGames);

            Console.WriteLine("GameID: " + summoner.acctId + "\n " + summoner.summonerId + "\n\n\n");
            int test = games.gameStatistics[1].fellowPlayers[1].summonerId;

            List<RespRecentGames> respGames = new List<RespRecentGames>();
            for (int i = 0; i < games.gameStatistics.Count; i++)
            {
                GameResult result = new GameResult(games.gameStatistics[i]);
                RespRecentGames resp = new RespRecentGames();
                
                resp.userId = summoner.acctId;
                resp.gameType = games.gameStatistics[i].gameType;
                resp.subType = games.gameStatistics[i].subType;
                resp.queueType = games.gameStatistics[i].queueType;
                resp.spell1 = games.gameStatistics[i].spell1;
                resp.spell2 = games.gameStatistics[i].spell2;
                resp.createDate = games.gameStatistics[i].createDate;
                resp.championId = games.gameStatistics[i].championId;
                resp.fellowPlayers = games.gameStatistics[i].fellowPlayers;
                resp.championsKilled = result.Kills;
                resp.numDeaths = result.Deaths;
                resp.assists = result.Assists;
                resp.lose = Convert.ToInt32(result.Win);
                resp.minionsKilled = result.MinionsKilled;
                resp.neutralMinionsKilled = Convert.ToInt32(result.NeutralMinionsKilled);
                resp.goldEarned = result.GoldEarned;
                resp.totalDamageDealt = result.TotalDamageDealt;
                resp.totalDamageDealt = result.TotalDamageTaken;
                resp.item0 = result.Items[0];
                resp.item1 = result.Items[1];
                resp.item2 = result.Items[2];
                resp.item3 = result.Items[3];
                resp.item4 = result.Items[4];
                resp.item5 = result.Items[5];

                respGames.Add(resp);
            }
            XmlWriter writer = XmlWriter.Create("test.xml");
            System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(respGames.GetType());
            x.Serialize(writer, respGames);

        }
         */
    }
}
