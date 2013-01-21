using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using LibOfLegends;

using agsXMPP;
using agsXMPP.protocol.client;
using agsXMPP.Collections;
using agsXMPP.protocol.iq.roster;

using com.riotgames.platform.statistics;
using com.riotgames.platform.summoner;
using com.riotgames.platform.summoner.masterybook;
using com.riotgames.platform.summoner.spellbook;
using com.riotgames.platform.game;
using com.riotgames.platform.gameclient.domain;
using com.riotgames.platform.clientfacade.domain;

using helpers;

namespace LOLQuery
{
    public class RiotCrawl
    {

        public RiotConnect pvpnet;
        public RiotChat chat;
        public DADatabase db;

        public List<SummonerCrawler> summonersList;

        private static int MaxSummoners = 25000;

        public RiotCrawl(Configuration config)
        {
            summonersList = new List<SummonerCrawler>();
            db = new DADatabase();

            if (db.getTrackedSummoners(ref summonersList))
            {
                crawl(config);
            }
        }

        private void crawl(Configuration config)
        {
            int numThreads;
            int summNdx, thrStart;
            CrawlWorkerData curWorker;
            List<RiotLogin> riotLogins = new List<RiotLogin>();
            List<RiotLogin> endRiotLogins = new List<RiotLogin>();
            List<CrawlWorkerData> thrSummList = new List<CrawlWorkerData>();
            List<SummonerCrawler> temp;
            List<Thread> _threads = new List<Thread>();
            List<CrawlWorker> _workers = new List<CrawlWorker>();
            float dTemp;

            CrawlWorker worker;
            Thread curThread;

            if (summonersList.Count < (MaxSummoners * 4))
            {
                numThreads = 4;
                MaxSummoners = (summonersList.Count / 4) + 4;
            }
            else
            {
                dTemp = (summonersList.Count / float.Parse(MaxSummoners.ToString()));
                numThreads = int.Parse(Math.Ceiling(dTemp).ToString());
            }

            db.getRiotLogins(ref riotLogins, ref endRiotLogins);

            if (numThreads > 5)
                numThreads = 5;

            summNdx = 0;
            for (int thr = 0; thr < numThreads; thr++)
            {
                thrStart = thr * MaxSummoners;
                temp = new List<SummonerCrawler>();
                for (summNdx = thrStart; summNdx < (thrStart + MaxSummoners) && summNdx < summonersList.Count; summNdx++)
                {
                    temp.Add(summonersList[summNdx]);
                }

                curWorker = new CrawlWorkerData(config, temp, riotLogins[thr].Username, riotLogins[thr].Password, endRiotLogins[thr].Username, endRiotLogins[thr].Password);
                thrSummList.Add(curWorker);
                riotLogins[thr].inUse = true;

                worker = new CrawlWorker(curWorker);
                curThread = new Thread(worker.doWork);
                _threads.Add(curThread);
                _workers.Add(worker);
                curThread.Start();
            }

            Console.WriteLine("Number of threads: " + numThreads);
            Console.WriteLine("First Summ Thread 1: " + thrSummList.ElementAt(0).loginUser);
            //Console.WriteLine("First Summ Thread 2: " + thrSummList.ElementAt(1).loginUser);
            //Console.WriteLine("First Summ Thread 3: " + thrSummList.ElementAt(2).loginUser);

            int command;
            string lineRead;
            Console.Write("$ ");

            while ((command = Console.Read()) != 'q')
            {
                switch (command)
                {
                    case 'p':

                        lineRead = Console.ReadLine();

                        try
                        {
                            int thNum = int.Parse(lineRead);
                            thNum = thNum - 1;
                            if (thNum < _workers.Count)
                            {
                                if (_workers.ElementAt(thNum).pause)
                                    _workers.ElementAt(thNum).pause = false;
                                else
                                    _workers.ElementAt(thNum).pause = true;
                            }
                            else
                            {
                                Console.WriteLine("Thread doesn't exist");
                            }
                        }
                        catch (Exception ex)
                        {
                            string e = ex.ToString();
                            Console.WriteLine("Invalid thread");
                        }

                    break;

                    case 'c':

                        lineRead = Console.ReadLine();

                        try
                        {
                            int thNum = int.Parse(lineRead);
                            thNum = thNum - 1;
                            if (thNum < _workers.Count)
                            {
                                Console.WriteLine("Thread " + (thNum + 1) + " has " + _workers.ElementAt(thNum).summonerList.Count + " summoners.");
                            }
                            else
                            {
                                Console.WriteLine("Thread doesn't exist");
                            }
                        }
                        catch (Exception ex)
                        {
                            string e = ex.ToString();
                            Console.WriteLine("Invalid thread number");
                        }

                    break;

                    case 'l':

                        lineRead = Console.ReadLine();

                        try
                        {
                            int thNum = int.Parse(lineRead);
                            thNum = thNum - 1;
                            if (thNum < _workers.Count)
                            {
                                Console.WriteLine("Thread " + (thNum + 1) + " has " + _workers.ElementAt(thNum).liveGames.Count + " live games.");
                            }
                            else
                            {
                                Console.WriteLine("Thread doesn't exist");
                            }
                        }
                        catch (Exception ex)
                        {
                            string e = ex.ToString();
                            Console.WriteLine("Invalid thread number");
                        }

                    break;

                    case 'd':

                        lineRead = Console.ReadLine();

                        try
                        {
                            int count = 0;
                            int thNum = int.Parse(lineRead);
                            thNum = thNum - 1;
                            if (thNum < _workers.Count)
                            {
                                List<string> keys = _workers.ElementAt(thNum).liveGameKeys;
                                Dictionary<string, InGameSummoner> games = _workers.ElementAt(thNum).liveGames;

                                for (int i = 0; i < keys.Count; i++)
                                {
                                    if (games[keys.ElementAt(i)].gameCompleted)
                                        count++;
                                }

                                double pc = (count / keys.Count) * 100;
                                int percent = int.Parse(Math.Ceiling(pc).ToString());

                                Console.WriteLine("Thread " + (thNum + 1) + " has " + count + " completed games ( " + percent + " ).");
                            }
                            else
                            {
                                Console.WriteLine("Thread doesn't exist");
                            }
                        }
                        catch (Exception ex)
                        {
                            string e = ex.ToString();
                            Console.WriteLine("Invalid thread number");
                        }

                    break;

                    default:

                        Console.ReadLine();

                    break;
                }

                Console.Write("$ ");
            }
            Console.WriteLine("Program terminating...");
            Console.WriteLine("Goodbye!");
        }

        private void updateSummoner(PublicSummoner pSumm, PlayerLifeTimeStats summStats, AggregatedStats aggStats, AllPublicSummonerDataDTO summPages, MasteryBook masteries)
        {
            PlayerStatSummary rSolo5x5;
            PlayerStatSummary rTeam5x5;
            PlayerStatSummary normal5x5;
            List<ChampionStatistics> champStats = ChampionStatistics.GetChampionStatistics(aggStats);

            db.updateSummoner("summonerLevel", pSumm.summonerLevel.ToString(), pSumm.summonerId);
            db.updateSummoner("profileIconId", pSumm.profileIconId.ToString(), pSumm.summonerId);

            if ((rSolo5x5 = summStats.getRankedSolo5x5()) != null) 
            {
                db.updateSummoner("solo5x5_elo", rSolo5x5.rating.ToString(), pSumm.summonerId);
                db.updateSummoner("solo5x5_wins", rSolo5x5.wins.ToString(), pSumm.summonerId);
                db.updateSummoner("solo5x5_losses", rSolo5x5.losses.ToString(), pSumm.summonerId);
                db.updateSummoner("solo5x5_maxElo", rSolo5x5.maxRating.ToString(), pSumm.summonerId);
            }

            if ((rTeam5x5 = summStats.getRankedTeam5x5()) != null) 
            {
                db.updateSummoner("team5x5_elo", rTeam5x5.rating.ToString(), pSumm.summonerId);
                db.updateSummoner("team5x5_wins", rTeam5x5.wins.ToString(), pSumm.summonerId);
                db.updateSummoner("team5x5_losses", rTeam5x5.losses.ToString(), pSumm.summonerId);
                db.updateSummoner("team5x5_maxElo", rTeam5x5.maxRating.ToString(), pSumm.summonerId);
            }

            if ((normal5x5 = summStats.getNormal5x5()) != null) 
            {
                db.updateSummoner("normal5x5_elo", normal5x5.rating.ToString(), pSumm.summonerId);
                db.updateSummoner("normal5x5_wins", normal5x5.wins.ToString(), pSumm.summonerId);
                db.updateSummoner("normal5x5_losses", normal5x5.losses.ToString(), pSumm.summonerId);
                db.updateSummoner("normal5x5_maxElo", normal5x5.maxRating.ToString(), pSumm.summonerId);
            }

            db.updateSummoner("ranked_kills", ChampionStatistics.totalRankedKills(champStats).ToString(), pSumm.summonerId);
            db.updateSummoner("ranked_assists", ChampionStatistics.totalRankedAssists(champStats).ToString(), pSumm.summonerId);
            db.updateSummoner("ranked_deaths", ChampionStatistics.totalRankedDeaths(champStats).ToString(), pSumm.summonerId);
            db.updateSummoner("ranked_pentaKills", ChampionStatistics.totalRankedPentaKills(champStats).ToString(), pSumm.summonerId);
            db.updateSummoner("ranked_quadraKills", ChampionStatistics.totalRankedQuadraKills(champStats).ToString(), pSumm.summonerId);
            db.updateSummoner("ranked_tripleKills", ChampionStatistics.totalRankedTripleKills(champStats).ToString(), pSumm.summonerId);
            db.updateSummoner("ranked_doubleKills", ChampionStatistics.totalRankedDoubleKills(champStats).ToString(), pSumm.summonerId);
            db.updateSummoner("ranked_minionKills", ChampionStatistics.totalRankedMinionKills(champStats).ToString(), pSumm.summonerId);
            db.updateSummoner("ranked_goldEarned", ChampionStatistics.totalRankedGoldEarned(champStats).ToString(), pSumm.summonerId);
            db.updateSummoner("ranked_turretsDestroyed", ChampionStatistics.totalRankedTurretsDestroyed(champStats).ToString(), pSumm.summonerId);
            db.updateSummoner("ranked_mostKills", ChampionStatistics.rankedMostKills(champStats).ToString(), pSumm.summonerId);
            db.updateSummoner("ranked_mostDeaths", ChampionStatistics.rankedMostDeaths(champStats).ToString(), pSumm.summonerId);

            /** Update Runes and Masteries */

        }
    }
}
