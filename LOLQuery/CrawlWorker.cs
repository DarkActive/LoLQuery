using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;

using LibOfLegends;
using FluorineFx;

using helpers;

using com.riotgames.platform.statistics;
using com.riotgames.platform.summoner;
using com.riotgames.platform.summoner.masterybook;
using com.riotgames.platform.summoner.spellbook;
using com.riotgames.platform.game;
using com.riotgames.platform.gameclient.domain;
using com.riotgames.platform.clientfacade.domain;

namespace LOLQuery
{
    public class CrawlWorker
    {
        # region Constants

        private const int GameStatusFreq = 60;
        private const int RecFreq = 5;
        private const int OUTOFGAME = 0;
        private const int INGAME = 1;
        private const int GAMEDONE = 2;
        private const int WAITINTERVAL = 3000;

        # endregion

        #region Class Variables

        public RiotConnect pvpnet;
        public bool pause;
        public DADatabase db;
        public CrawlWorkerData data;
        public List<SummonerCrawler> summonerList;
        public Dictionary<string, InGameSummoner> liveGames;
        public List<string> liveGameKeys;
        public double lastUpdateSummList;

        private AutoResetEvent stopWaitHandle;
        public double startTime;
        public int pvpnetReconnects;

        private bool forceDC;
        private string logPath;

        #endregion

        #region Worker Methods

        public CrawlWorker(CrawlWorkerData cData)
        {
            pause = false;
            data = cData;
            forceDC = false;
            db = new DADatabase();

            summonerList = cData.summonerList;
            liveGames = new Dictionary<string, InGameSummoner>();
            liveGameKeys = new List<string>();
            stopWaitHandle = new AutoResetEvent(false);
            pvpnetReconnects = 0;

            logPath = data.loginUser + ".log";
            /** Clear log file */
            File.WriteAllText(logPath, String.Empty);
        }

        public void doWork()
        {
            /** Persistent connection */
            while (!forceDC)
            {
                /* Establish Connection to PvPNet */
                pvpnet = new RiotConnect(data.configuration, data.loginUser, data.loginPass);

                /** Wait for PvP.net Server connection */
                if (waitForPvpnet(pvpnet))
                {
                    /* Initialise the PvPNet Crawler */
                    crawlPvpnet();
                }
            }
        }

        private void crawlPvpnet()
        {
            PlatformGameLifecycleDTO specGame;
            GameDTO liveGame;
            int summNdx;
            SummonerCrawler curSummoner;
            int queries = 0, totalQueries = 0;
            double lastItr = 0, time, lastPost = 0;
            bool firstItr = true;

            lastUpdateSummList = getUnixTimestamp();

            /* New thread in charge of checking endGame */
            if (pvpnetReconnects == 0)
            {
                EndGameWorker endGameWorker = new EndGameWorker(ref liveGames, ref liveGameKeys, data.configuration, data.endLoginUser, data.endLoginPass);
                Thread endGameThread = new Thread(endGameWorker.doWork);
                endGameThread.Start();
            }

            while (pvpnet.Connected)
            {
                // If pause is set, wait until unpaused
                while (pause)
                {
                    ConsoleOut("Thread is paused....");
                    Thread.Sleep(10 * 1000);
                }

                /* Signal start of first iteration */
                lastItr = getUnixTimestamp();
                
                for (summNdx = 0; summNdx < summonerList.Count && pvpnet.Connected && !pause; summNdx++)
                {
                    curSummoner = summonerList.ElementAt(summNdx);

                    if ((getUnixTimestamp() - curSummoner.lastGameCheck) < GameStatusFreq)
                    {
                        continue;
                    }

                    if (firstItr)
                    {
                        List<SummonerCrawler> preGamesList = db.findPreGames(curSummoner.accountId, curSummoner.summonerId, curSummoner.summonerName);
                        SummonerCrawler curSummCrawl;

                        for (int m = 0; m < preGamesList.Count; m++)
                        {
                            curSummCrawl = preGamesList.ElementAt(m);
                            liveGames.Add(getHashKey(curSummCrawl.lastGameId, curSummCrawl.accountId), new InGameSummoner(curSummCrawl, true));
                            liveGameKeys.Add(getHashKey(curSummCrawl.lastGameId, curSummCrawl.accountId));
                        }
                    }

                    if (curSummoner.gameStatus == OUTOFGAME)
                    {
                        // Currently in a new game, add pregame info
                        if ((specGame = pvpnet.RPC.game.retrieveInProgressSpectatorGameInfo(curSummoner.summonerName)) != null)
                        {
                            liveGame = specGame.game;
                            curSummoner.gameStatus = INGAME;

                            // Add to db and liveGame list if doesn't already exist
                            if (!db.gameStatsExists(liveGame.id) && liveGame.gameType != "PRACTICE_GAME")
                            {
                                double pc = (double.Parse(summNdx.ToString()) / summonerList.Count) * 100;
                                int percent = int.Parse(Math.Round(pc).ToString());
                                ConsoleOut("\tGameUpdate: Adding In Progress Game - ID =  " + liveGame.id + " ------- " + percent + "%");
                                curSummoner.lastGameId = liveGame.id;

                                db.addInProgressGame(specGame, curSummoner.accountId);
                                liveGames.Add(getHashKey(liveGame.id, curSummoner.accountId), new InGameSummoner((SummonerCrawler)curSummoner.Clone()));
                                liveGameKeys.Add(getHashKey(curSummoner.lastGameId, curSummoner.accountId));
                            }
                            curSummoner.lastGameId = liveGame.id;
                        }

                        curSummoner.lastGameCheck = getUnixTimestamp();
                    }
                    else if (curSummoner.gameStatus == INGAME)
                    {
                        // Was in game and no longer. Set game Status to finshed.
                        if ((specGame = pvpnet.RPC.game.retrieveInProgressSpectatorGameInfo(curSummoner.summonerName)) == null)
                        {
                            curSummoner.gameStatus = OUTOFGAME;
                            if (liveGames.ContainsKey(getHashKey(curSummoner.lastGameId, curSummoner.accountId))) 
                            {
                                liveGames[getHashKey(curSummoner.lastGameId, curSummoner.accountId)].gameCompleted = true;
                                ConsoleOut("\tGameUpdate: Set game to completed - ID = " + curSummoner.lastGameId);
                            }
                            curSummoner.lastGameId = 0;
                        }
                        // Was in game and still in game, but check for different game
                        else
                        {
                            liveGame = specGame.game;
                            if (curSummoner.lastGameId != liveGame.id && liveGame.gameType != "PRACTICE_GAME")
                            {
                                // Set old game to complete
                                if (liveGames.ContainsKey(getHashKey(curSummoner.lastGameId, curSummoner.accountId)))
                                {
                                    liveGames[getHashKey(curSummoner.lastGameId, curSummoner.accountId)].gameCompleted = true;
                                    ConsoleOut("\tGameUpdate: Set game to completed, but late - ID = " + curSummoner.lastGameId);
                                }

                                // Add to db and liveGame list if doesn't already exist
                                if (!db.gameStatsExists(liveGame.id))
                                {
                                    double pc = (double.Parse(summNdx.ToString()) / summonerList.Count) * 100;
                                    int percent = int.Parse(Math.Round(pc).ToString());
                                    ConsoleOut("\tGameUpdate: Adding In Progress Game - ID =  " + liveGame.id + " ------- " + percent + "%");
                                    curSummoner.lastGameId = liveGame.id;

                                    db.addInProgressGame(specGame, curSummoner.accountId);
                                    liveGames.Add(getHashKey(curSummoner.lastGameId, curSummoner.accountId), new InGameSummoner((SummonerCrawler)curSummoner.Clone()));
                                    liveGameKeys.Add(getHashKey(curSummoner.lastGameId, curSummoner.accountId));
                                }
                            }
                            curSummoner.lastGameId = liveGame.id;
                        }

                        curSummoner.lastGameCheck = getUnixTimestamp();
                    }
                  
                    queries++;
                    totalQueries++;
                }
                time = getUnixTimestamp() - lastItr;
                ConsoleOut("TrackStatus: " + summonerList.Count + " Summoners tracked; " + liveGames.Count() + " Live Games; Iteration took " + time + " seconds");
                lastPost = getUnixTimestamp();
                firstItr = false;
            }
            pvpnetReconnects++;
        }

        #endregion

        #region Helper Methods

        private string getHashKey(long gameId, long acctId)
        {
            return gameId.ToString() + "_" + acctId.ToString();
        }

        private static double getUnixTimestamp()
        {
            DateTime epoch = new DateTime(1970, 1, 1).ToLocalTime();
            TimeSpan span = (DateTime.Now - epoch);

            return span.TotalSeconds;
        }

        private static int CompareGames(PlayerGameStats x, PlayerGameStats y)
        {
            return -x.createDate.CompareTo(y.createDate);
        }

        private void ConsoleOut(string str)
        {
            string now;
            using (StreamWriter outfile = File.AppendText(logPath))
            {
                now = DateTime.Now.ToString("yy/MM/dd HH:mm:ss");
                outfile.WriteLine("[" + now + "] " + str);
            }
        }

        /** Method to wait for connection or keep trying reconnect */
        private static bool waitForPvpnet(RiotConnect pvpnet)
        {
            int time = 0;

            while (!pvpnet.Connected && time < RecFreq)
            {
                Thread.Sleep(1000);
                time++;
            }

            if (time >= RecFreq)
                return false;

            return true;
        }

        #endregion
    }
}
