using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace helpers
{
    public class CrawlWorkerData
    {
        public Configuration configuration;
        public List<SummonerCrawler> summonerList;
        public string loginUser;
        public string loginPass;
        public string endLoginUser;
        public string endLoginPass;

        public CrawlWorkerData(Configuration config, List<SummonerCrawler> list, string user, string pass, string eUser, string ePass)
        {
            configuration = config;
            summonerList = list;
            loginUser = user;
            loginPass = pass;
            endLoginUser = eUser;
            endLoginPass = ePass;
        }
    }
}
