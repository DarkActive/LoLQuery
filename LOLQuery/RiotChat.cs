using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Threading;

using System.Xml;
using System.Xml.Serialization;
using System.IO;

using agsXMPP;
using agsXMPP.protocol.client;
using agsXMPP.Collections;
using agsXMPP.protocol.iq.roster;

namespace LOLQuery
{
    public class RiotChat
    {
        public XmppClientConnection xmpp;
        public long inviteId;
        public List<Presence> roster;

        public RiotChat(string user, string pass)
        {
            xmpp = new XmppClientConnection();
            roster = new List<Presence>();

            xmpp.Username = user;
            xmpp.Password = "AIR_" + pass;

            xmpp.Server = "pvp.net";
            xmpp.ConnectServer = "chat.na1.lol.riotgames.com";
            xmpp.Port = 5223;
            xmpp.AutoResolveConnectServer = false;
            xmpp.UseCompression = false;
            xmpp.UseStartTLS = false;
            xmpp.UseSSL = true;

            xmpp.OnAuthError += new XmppElementHandler(xmpp_OnEleError);
            xmpp.OnError += new ErrorHandler(xmpp_OnError);
            xmpp.OnStreamError += new XmppElementHandler(xmpp_OnEleError);
            xmpp.OnSocketError += new ErrorHandler(xmpp_OnError);

            xmpp.OnLogin += new ObjectHandler(xmpp_OnLogin);
            xmpp.OnPresence += new PresenceHandler(xmpp_OnPresence);
            xmpp.OnReadXml += new XmlHandler(xmpp_OnReadXML);
            xmpp.OnWriteXml += new XmlHandler(xmpp_OnWriteXML);
            xmpp.OnMessage += new MessageHandler(xmpp_OnMessage);

            xmpp.Open();
        }

        public void xmpp_OnError(object sender, Exception ex)
        {
            Console.WriteLine("Error: " + ex.ToString());
        }

        public void xmpp_OnReadXML(object sender, string xml)
        {
            //Console.WriteLine("Read XML: " + xml);
        }

        public void xmpp_OnWriteXML(object sender, string xml)
        {
            //Console.WriteLine("Write XML: " + xml);
        }

        public void xmpp_OnEleError(object sender, agsXMPP.Xml.Dom.Element el)
        {
            Console.WriteLine("Error: " + el.ToString());
        }

        private void xmpp_OnLogin(object sender)
        {
            Console.WriteLine("Connected to PvP.net Chat.");
            sendPresence();

            //xmpp.Send(new Message("sum415489@pvp.net", MessageType.chat, "SUP KAPPA?!?"));
            //inviteSummoner("sum415489@pvp.net", 1);
        }

        private void xmpp_OnPresence(object sender, Presence pres)
        {
            Presence pr;

            if (pres.Type == PresenceType.unavailable)
            {
                roster.Remove(findPresence(roster, pres));
            }
            else
            {
                pr = findPresence(roster, pres);
                if (pr == null)
                    roster.Add(pres);
                else
                {
                    roster.Remove(pr);
                    roster.Add(pres);
                }
            }
        }

        public static Presence findPresence(List<Presence> haystack, Presence needle)
        {
            Presence findPres = haystack.Find(
                delegate(Presence pr)
                {
                    return pr.From.User == needle.From.User;
                }
                );

            return findPres;
        }

        public static Presence findSummonerInRoster(List<Presence> haystack, Jid needle)
        {
            Presence findPres = haystack.Find(
                delegate(Presence pr)
                {
                    return pr.From.User == needle.User;
                }
                );

            return findPres;
        }

        public void sendPresence()
        {
            Presence p = new Presence(ShowType.chat, "<body><profileIcon>10</profileIcon><statusMsg>Tracking appaK's Stats\ntest</statusMsg><level>30</level><wins>1337</wins><leaves>0</leaves><queueType /><rankedWins>1337</rankedWins><rankedLosses>0</rankedLosses><rankedRating>1337</rankedRating><tier>PLATINUM</tier><gameStatus>outOfGame</gameStatus></body>");
            p.Type = PresenceType.available;
            xmpp.Send(p);
        }

        private void xmpp_OnMessage(object sender, Message msg)
        {
            if (msg.Body != null && msg.Type == MessageType.normal && msg.Subject == "PRACTICE_GAME_INVITE")
            {
                string[] stringSeperators = new string[] {"<inviteId>","</inviteId>"};
                string[] result;

                result = msg.Body.Split(stringSeperators, StringSplitOptions.None);

                try 
                {
                    inviteId = Int64.Parse(result[1]);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: Invalid invite message: " + ex.Message);
                }
            }
                
        }

        private void MessageCallBack(object sender, Message msg, object data)
        {
            Console.WriteLine("HELLO");
            if (msg.Body != null && msg.Type == MessageType.normal && msg.Subject == "PRACTICE_GAME_INVITE")
                Console.WriteLine("NEW GAME INVITE");
        }

        public void inviteSummoner(long summonerId, long gameId, long invId)
        {
            Message inv = new Message();
            string summoner = "sum" + summonerId.ToString() + "@pvp.net";

            inv.To = new Jid(summoner);
            inv.Type = MessageType.normal;
            inv.Body = "<body><inviteId>1167194767</inviteId><userName>WESA001</userName><profileIconId>10</profileIconId><gameType>PRACTICE_GAME</gameType><gameTypeIndex>practiceGame_gameMode_GAME_CFG_DRAFT_TOURNAMENT</gameTypeIndex><gameId>" + gameId + "</gameId><mapId>1</mapId><gamePassword>hello</gamePassword></body>";
            inv.Subject = "PRACTICE_GAME_INVITE";

            xmpp.Send(inv);
        }
    }
}
