using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;

using LibOfLegends;

using com.riotgames.platform.statistics;
using com.riotgames.platform.summoner;
using com.riotgames.platform.gameclient.domain;

using helpers;

namespace LOLQuery
{
    public class RiotConnect
    {
        public Configuration config;
        public ConnectionProfile connectionData;
        public ProxyProfile proxy;

        public RPCService RPC;
        public bool Connected;
        public bool AutoWatch;
        public string logPath;

        public RiotConnect(Configuration configure, string username, string password)
        {
            proxy = new ProxyProfile();
            //TODO: Make autowatch change based on input
            AutoWatch = false;
            config = configure;
            connectionData = new ConnectionProfile(config.Authentication, config.Region.RegionData, proxy, username, password);
            logPath = username + ".log";

            Connect();
        }

        public void ConsoleOut(string str)
        {
            using (StreamWriter outfile = File.AppendText(logPath))
            {
                outfile.WriteLine(str);
            }
        }

        void Connect()
        {
            ConsoleOut("Connecting to server (" + config.Region.Abbreviation + ".LOL.riotgames.com) .....");
            try
            {
                RPC = new RPCService(connectionData, OnConnect, OnDisconnect);
                RPC.Connect();
            }
            catch (Exception ex)
            {
                ConsoleOut("Server connection error: " + ex.Message);
            }
        }

        private void OnConnect(RPCConnectResult result)
        {
            if (result.Success())
            {
                Connected = true;
                ConsoleOut("Successfully connected to the server.");
            }
            else
            {
                ConsoleOut("Queue to login, attempting a reconnect...");
            }
        }

        void ConnectInThread()
        {
            ConsoleOut("Attempting reconnect...");
            (new Thread(Connect)).Start();
        }

        void OnDisconnect()
        {
            if (!AutoWatch)
            {
                Connected = false;
                ConsoleOut("Disconnected from server.");
            }
            else
            {
                //You get disconnected after idling for two hours
                Connected = false;
                ConsoleOut("Disconnected from server.");
                //Reconnect
                //Thread.Sleep(5000);
                //ConnectInThread();
            }
        }

        static int CompareGames(PlayerGameStats x, PlayerGameStats y)
        {
            return -x.createDate.CompareTo(y.createDate);
        }

        static int getRawStat(List<RawStat> stat, string type)
        {
            RawStat resStat = stat.Find(
                    delegate(RawStat st)
                    {
                        return st.statType == type;
                    }
                    );

            if (resStat == null)
                return -1;
            else
                return resStat.value;
        }

        void RunAutomaticUpdates()
        {
            PublicSummoner summoner;
            RecentGames gameData;
            List<PlayerGameStats> games;


            while (Connected)
            {
                summoner = RPC.GetSummonerByName("appaK");
                gameData = RPC.GetRecentGames(summoner.acctId);
                games = gameData.gameStatistics;

                ConsoleOut("ID: " + summoner.acctId);

                for (int i = 0; i < games.Count; i++)
                {
                    for (int j = 0; j < games[i].statistics.Count; j++)
                        ConsoleOut(games[i].statistics[j].statType + " = " + games[i].statistics[j].value);
                }

                if (Connected)
                    Thread.Sleep(100 * 1000);
            }
        }
    }
}
