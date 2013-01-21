using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;

using com.riotgames.platform.game;
using com.riotgames.platform.summoner;
using com.riotgames.platform.statistics;

using LibOfLegends;
using helpers;

namespace LOLQuery
{
    public class EndGameWorker
    {
        #region CONSTANTS

        private const int RECFREQ = 5;
        private const int MAXWAITASYNC = 10000;

        #endregion

        #region Class Variables

        public Dictionary<string, InGameSummoner> inGameSummoners;
        public List<string> inGameKeys;
        public RiotConnect pvpnet;
        public Configuration config;
        string endLoginUser;
        string endLoginPass;
        public bool forceDC;
        public DADatabase db;
        public string logPath;

        public int pvpnetReconnects;
        public double startTime;
        public AutoResetEvent stopWaitHandle;
        private bool gameUpdatedElsewhere;
        public int endTotalPlayers;
        public SummonerCrawler reportingSummoner;
        public List<EndSummonerGameStats> endSummonerStats;
        public List<PublicSummoner> endPSummoners;
        public List<PlayerLifeTimeStats> endLifeStats;

        #endregion

        #region Worker Methods

        public EndGameWorker(ref Dictionary<string, InGameSummoner> iGS, ref List<string> keys, Configuration cfg, string endUser, string endPass)
        {
            logPath = endUser + ".log";
            /** Clear log file */
            File.WriteAllText(logPath, String.Empty);

            inGameSummoners = iGS;
            inGameKeys = keys;
            db = new DADatabase();
            config = cfg;
            endLoginUser = endUser;
            endLoginPass = endPass;
            stopWaitHandle = new AutoResetEvent(false);
            gameUpdatedElsewhere = false;
            pvpnetReconnects = 0;

            endSummonerStats = new List<EndSummonerGameStats>();
            endPSummoners = new List<PublicSummoner>();
            endLifeStats = new List<PlayerLifeTimeStats>();
            forceDC = false;
        }

        public void doWork()
        {
            int ndx;
            double lastPrint = 0;
            double startTime;
            RecentGames recGames;
            InGameSummoner liveGame;
            int i;

            while (!forceDC)
            {
                pvpnet = new RiotConnect(config, endLoginUser, endLoginPass);

                if (waitForPvpnet(pvpnet))
                {
                    while (pvpnet.Connected)
                    {
                        for (ndx = 0; ndx < inGameKeys.Count && pvpnet.Connected; ndx++)
                        {
                            liveGame = inGameSummoners[inGameKeys.ElementAt(ndx)];
                            // Game Completed, begin end game stats
                            // TODO make sure this is fixed with check for null
                            if (liveGame != null && liveGame.gameCompleted)
                            {
                                ConsoleOut("GameUpdate: Game completed and updating started - ID = " + liveGame.summonerCr.lastGameId);
                                startTime = getUnixTimestamp();
                                startEndGameStats(liveGame.summonerCr);

                                if (stopWaitHandle.WaitOne(MAXWAITASYNC))
                                {
                                    // This is a boolean that specifies if other thread updated End game
                                    if (!gameUpdatedElsewhere)
                                        finishEndGameStats();

                                    gameUpdatedElsewhere = false;
                                    inGameSummoners.Remove(inGameKeys.ElementAt(ndx));
                                    inGameKeys.RemoveAt(ndx);
                                }
                                else
                                {
                                    // On 2 timeouts, remove all game entries from database and add as an old game.
                                    if (liveGame.timedOut)
                                    {
                                        db.removeLiveGame(liveGame.summonerCr.lastGameId);
                                        db.removePreGame(liveGame.summonerCr.lastGameId);

                                        // Sort recent games and add correct to DB
                                        // TODO: null object error recGames
                                        recGames = pvpnet.RPC.GetRecentGames(liveGame.summonerCr.accountId);
                                        recGames.gameStatistics.Sort(CompareGames);
                                        for (i = 0; i < recGames.gameStatistics.Count; i++)
                                        {
                                            if (recGames.gameStatistics.ElementAt(i).gameId == liveGame.summonerCr.lastGameId)
                                            {
                                                if (!db.gameStatsExists(liveGame.summonerCr.lastGameId))
                                                {
                                                    db.addOldGame(recGames.gameStatistics.ElementAt(i), liveGame.summonerCr.accountId);
                                                    break;
                                                }
                                            }
                                        }
                                        inGameSummoners.Remove(inGameKeys.ElementAt(ndx));
                                        inGameKeys.RemoveAt(ndx);
                                    }

                                    liveGame.timedOut = true;
                                    ConsoleOut("TIMEOUT: Timedout on game update");
                                }
                            }

                            if (getUnixTimestamp() - lastPrint >= 10)
                            {
                                ConsoleOut("...... " + inGameSummoners.Count);
                                lastPrint = getUnixTimestamp();
                            }
                        }
                    }
                    pvpnetReconnects++;
                }
            }
        }

        public void startEndGameStats(SummonerCrawler curSummoner)
        {
            // Clear all endGameLists to start new endGame
            endTotalPlayers = 10;                       // Set to max, until other async call back sets it
            endSummonerStats.Clear();
            endPSummoners.Clear();
            endLifeStats.Clear();
            reportingSummoner = curSummoner;
            startTime = getUnixTimestamp();

            // Async Call for lifetime stats and Recent games
            pvpnet.RPC.RetrievePlayerStatsByAccountIDAsync(curSummoner.accountId, "CURRENT", new FluorineFx.Net.Responder<PlayerLifeTimeStats>(endPlayerLifeStatsResponder));
            pvpnet.RPC.GetRecentGamesAsync(curSummoner.accountId, new FluorineFx.Net.Responder<RecentGames>(endRecentGamesResponder));
        }

        public void finishEndGameStats()
        {
            PlayerGameStats pStats;
            PlayerLifeTimeStats lifeStats;
            GameResult gRes;
            string summName;

            ConsoleOut("\tGameUpdate: FIRST Time = " + (getUnixTimestamp() - startTime).ToString());

            db.updateEndGame(reportingSummoner.lastGameId, reportingSummoner.accountId);

            ConsoleOut("\tGameUpdate: Updating end game done, now doing fellow players");

            for (int i = 0; i < endSummonerStats.Count; i++)
            {

                pStats = endSummonerStats.ElementAt(i).gameStats;
                gRes = new GameResult(pStats);
                summName = findSummNameInPublicSumm(endSummonerStats.ElementAt(i).accountId);
                lifeStats = findLifeStats(endSummonerStats.ElementAt(i).accountId);

                db.addGameStats(endSummonerStats.ElementAt(i).accountId, pStats, gRes, lifeStats, summName);
            }

            db.removeLiveGame(reportingSummoner.lastGameId);

            ConsoleOut("\tGameUpdate: Finished Updating End of Game Stats ----- Time = " + (getUnixTimestamp() - startTime).ToString());
        }

        #endregion

        #region Async Responder Methods

        public void endRecentGamesResponder(RecentGames rGames)
        {
            int ndx, sNdx, i;
            bool tempExists;
            PlayerGameStats pgStats;
            List<long> fellowSummonerIds = new List<long>();

            if (rGames == null)
                return;

            // Sort recent games by newest game first
            rGames.gameStatistics.Sort(CompareGames);

            for (ndx = 0; ndx < rGames.gameStatistics.Count; ndx++)
            {
                pgStats = rGames.gameStatistics.ElementAt(ndx);

                /* Check if game is current game we are updating */
                if (pgStats.gameId == reportingSummoner.lastGameId)
                {
                    if (db.gameStatsIncomplete(pgStats.gameId))
                    {
                        endSummonerStats.Add(new EndSummonerGameStats(pgStats, rGames.userId));

                        // Set GLOBAL variable for endGame total players
                        endTotalPlayers = 1 + pgStats.fellowPlayers.Count;

                        for (sNdx = 0; sNdx < pgStats.fellowPlayers.Count; sNdx++)
                        {
                            fellowSummonerIds.Add(pgStats.fellowPlayers.ElementAt(sNdx).summonerId);
                        }

                        // Async call to get all names by summoner IDs, continues in responder function
                        pvpnet.RPC.GetSummonerNamesAsync(fellowSummonerIds, new FluorineFx.Net.Responder<List<string>>(endSummonerNamesResponder));
                    }
                    else
                    {
                        // This is a boolean that specifies if other thread updated End game
                        gameUpdatedElsewhere = true;
                        stopWaitHandle.Set();
                    }
                    break;
                }
            }

            for (i = (ndx + 1); i < rGames.gameStatistics.Count; i++)
            {
                pgStats = rGames.gameStatistics.ElementAt(i);
                tempExists = db.gameStatsExists(pgStats.gameId);

                /* Check if it should add an old game */
                if (!tempExists)
                {
                    ConsoleOut("\t\tOld Game Added, id = " + pgStats.gameId);
                    db.addOldGame(pgStats, rGames.userId);
                }
                /* Check if it should skip rest of loop because older games should already be in database */
                else if (tempExists)
                {
                    break;
                }
            }
        }

        public void endPublicSummonerResponder(PublicSummoner summoner)
        {
            endPSummoners.Add(summoner);

            if (summoner != null)
            {
                pvpnet.RPC.GetRecentGamesAsync(summoner.acctId, new FluorineFx.Net.Responder<RecentGames>(endFellowRecentGamesResponder));
                pvpnet.RPC.RetrievePlayerStatsByAccountIDAsync(summoner.acctId, "CURRENT", new FluorineFx.Net.Responder<PlayerLifeTimeStats>(endPlayerLifeStatsResponder));
            }
        }


        public void endSummonerNamesResponder(List<string> summNames)
        {
            for (int ndx = 0; ndx < summNames.Count; ndx++)
            {
                pvpnet.RPC.GetSummonerByNameAsync(summNames.ElementAt(ndx), new FluorineFx.Net.Responder<PublicSummoner>(endPublicSummonerResponder));
            }
        }

        public void endFellowRecentGamesResponder(RecentGames rGames)
        {
            int ndx;
            // Sort recent games by newest game first
            if (rGames == null)
                return;

            rGames.gameStatistics.Sort(CompareGames);

            for (ndx = 0; ndx < rGames.gameStatistics.Count; ndx++)
            {
                if (rGames.gameStatistics.ElementAt(ndx).gameId == reportingSummoner.lastGameId)
                {
                    endSummonerStats.Add(new EndSummonerGameStats(rGames.gameStatistics.ElementAt(ndx), rGames.userId));
                    break;
                }
            }

            if (ndx >= rGames.gameStatistics.Count)
            {
                return;
            }

            //ConsoleOut("\tGameUpdate: EndRecentGames");

            if (endLifeStats.Count >= endTotalPlayers && endSummonerStats.Count >= endTotalPlayers)
            {
                stopWaitHandle.Set();
            }
        }

        public void endPlayerLifeStatsResponder(PlayerLifeTimeStats pStats)
        {
            endLifeStats.Add(pStats);

            //ConsoleOut("\tGameUpdate: EndLifeStats");
            if (endLifeStats.Count >= endTotalPlayers && endSummonerStats.Count >= endTotalPlayers)
            {
                stopWaitHandle.Set();
            }
        }

        #endregion

        #region Helper Methods

        private PlayerLifeTimeStats findLifeStats(long acctId)
        {
            for (int i = 0; i < endLifeStats.Count; i++)
            {
                if (endLifeStats.ElementAt(i) != null && endLifeStats.ElementAt(i).userId == acctId)
                    return endLifeStats.ElementAt(i);
            }

            return null;
        }

        private long findSummIdInPublicSumm(long acctId)
        {
            for (int i = 0; i < endPSummoners.Count; i++)
            {
                if (endPSummoners.ElementAt(i) != null && endPSummoners.ElementAt(i).acctId == acctId)
                    return endPSummoners.ElementAt(i).summonerId;
            }

            return 0;
        }

        private string findSummNameInPublicSumm(long acctId)
        {
            for (int i = 0; i < endPSummoners.Count; i++)
            {
                if (endPSummoners.ElementAt(i).acctId == acctId)
                    return endPSummoners.ElementAt(i).internalName;
            }

            return "";
        }

        private void ConsoleOut(string str)
        {
            string now;
            using (StreamWriter outfile = File.AppendText(logPath))
            {
                now = DateTime.Now.ToString("yy/MM/dd HH:mm:ss");
                outfile.WriteLine(str);
            }
        }

        private static double getUnixTimestamp()
        {
            DateTime epoch = new DateTime(1970, 1, 1).ToLocalTime();
            TimeSpan span = (DateTime.Now - epoch);

            return span.TotalSeconds;
        }

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

        private static int CompareGames(PlayerGameStats x, PlayerGameStats y)
        {
            return -x.createDate.CompareTo(y.createDate);
        }

        #endregion
    }
}
