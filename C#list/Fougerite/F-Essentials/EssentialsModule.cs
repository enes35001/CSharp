using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fougerite;
using Fougerite.Events;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using System.Net;

namespace F_Essentials
{
    public class EssentialsModule : Module
    {
        public IniParser config;
        public IniParser friends;
        public IniParser share;
        public IniParser HelpList;
        public IniParser RulesList;
        public IniParser Warps;
        public IniParser SpawnsOverwrite;
        public IniParser PlayerDatabase;
        public IniParser BodyParts;

        // config

        public bool EnableJoinMessages;
        public bool EnableLeaveMessages;
        public bool EnableCountryMessages;
        public bool EnableFirstjoinMessages;
        public bool EnableWarps;
        public bool EnableRemove;
        public bool EnableHelp;
        public bool EnableRules;
        public bool EnableNewSpawns;

        public bool ShowAirdrop;
        public bool HurtMessages;
        public bool DeathMessages;
        public bool OverwriteSpawns;

        // messages
        public string JoinMessage;
        public string LeaveMessage;
        public string FirstJoinMessage;
        public string CountryMessage;
        public string CountryFailMessage;

        public string TprSendMessage;
        public string TprIncommingMessage;
        public string TprAcceptedMessage;
        public string TprAcceptMessage;
        public string TprBlockMessage;
        public string TprUnderRockMessage;
        public string TprMoveMessage;
        public string TprMaxTokens;
        public string TprNoTokensMessage;

        public string HomeSetMessage;
        public string HomeDelayMessage;
        public string HomeCooldownMessage;
        public string MaxHomesMessage;
        public string NoHomeMessage;

        public string RemoveOnMessage;
        public string RemoveOffMessage;
        public string NotAllowedRemoveMessage;

        public string WarpDelayMessage;
        public string WarpCooldownMessage;
        public string WarpNotFoundMessage;
        public string WarpNotAllowedMessage;

        public string AddFriendMessage;
        public string FriendAddedMessage;
        public string UnfriendMessage;
        public string AlreadyFriendedMessage;
        public string HurtWarningPopup;

        public string SharedMessage;
        public string UnSharedMessage;
        public string AlreadySharedMessage;

        public string AirdropIncommingMessage;



        // tpr settings;
        public bool TprStructureBlock;
        public bool TprMovecheck;
        public int Tokens;
        public bool Enabletpr;
        public int TprCooldown;
        public int TprDelay;

        //Warp settings
        public int WarpDelay;
        public int WarpCooldown;
        public bool WarpMoveCheck;

        //Remover settings
        public int Removetime;
        public bool Refund;

        //AirdropSettings
        public int MinPlayers;
        public int NumOfPlanes;
        public int AirdropTimer;


        //home settings
        public bool SleepingHome;
        public bool EnableHome;
        public bool HomeMoveCheck;
        public bool StructureHomeBlock;
        public bool CancelHomeOnDamage;
        public int MaxHomes;
        public int HomeCooldown;
        public int HomeDelay;

        //SpawnsOverwrite settings
        public int AmountOfSpawns;



        public override string Name { get { return "Fougerite-Essentials"; } }
        public override string Author { get { return "ice cold"; } }
        public override string Description { get { return "The ultimate Fougerite-Essentials plugin with allot of features"; } }
        public override Version Version { get { return new Version("1.0B"); } }

        public List<Player> tprreq = new List<Player>();
        public List<ulong> remove = new List<ulong>();
        public List<ulong> warpcd = new List<ulong>();

        public Dictionary<ulong, Vector3> homemovecheck = new Dictionary<ulong, Vector3> { };
        public Dictionary<ulong, Vector3> warpmovecheck = new Dictionary<ulong, Vector3> { };
        public Dictionary<ulong, Vector3> tprmovecheck = new Dictionary<ulong, Vector3> { };

        public System.Random rnd;
        string cou = string.Empty;
     
        public override void Initialize()
        {
            CheckConfig();
            dircheck();
            Hooks.OnCommand += Command;
            Hooks.OnAirdropCalled += Airdrop;
            Hooks.OnPlayerConnected += OnPlayerConnected;
            Hooks.OnPlayerDisconnected += OnPlayerDisconnected;
            Hooks.OnPlayerKilled += Killed;
            Hooks.OnPlayerSpawned += Spawned;
            Hooks.OnServerLoaded += Loaded;
            Hooks.OnPluginInit += Init;

            rnd = new System.Random();

            friends = new IniParser(Path.Combine(ModuleFolder, "FriendsDatabase.list"));
            share = new IniParser(Path.Combine(ModuleFolder, "ShareDatabase.list"));
            HelpList = new IniParser(Path.Combine(ModuleFolder, "helplist.list"));
            Warps = new IniParser(Path.Combine(ModuleFolder, "Warps.list"));
            SpawnsOverwrite = new IniParser(Path.Combine(ModuleFolder, "Spawns.list"));
            PlayerDatabase = new IniParser(Path.Combine(ModuleFolder, "PlayerDatabase.list"));
            BodyParts = new IniParser(Path.Combine(ModuleFolder, "BodyParts.list"));
        }     

        public override void DeInitialize()
        {
            Hooks.OnCommand -= Command;
            Hooks.OnAirdropCalled -= Airdrop;
            Hooks.OnPlayerConnected -= OnPlayerConnected;
            Hooks.OnPlayerDisconnected -= OnPlayerDisconnected;
            Hooks.OnPlayerKilled -= Killed;
            Hooks.OnPlayerSpawned -= Spawned;
            Hooks.OnServerLoaded -= Loaded;
            Hooks.OnPluginInit -= Init;
        }
        public void Spawned(Player pl, SpawnEvent se)
        {
            if(EnableNewSpawns)
            {
                if(!se.CampUsed)
                {
                    int d = rnd.Next(1, AmountOfSpawns);
                    string l = SpawnsOverwrite.GetSetting("SpawnLocations", d.ToString());
                    Vector3 loc = Util.GetUtil().ConvertStringToVector3(l);
                    pl.SafeTeleportTo(loc);
                }              
            }
        }
        public void OnPlayerConnected(Player pl)
        {
            PlayerDatabase.AddSetting(pl.SteamID, "ID", pl.SteamID);
            PlayerDatabase.AddSetting(pl.SteamID, "IP", pl.IP);
            PlayerDatabase.AddSetting(pl.SteamID, "Name", pl.Name);
            PlayerDatabase.AddSetting(pl.SteamID, "FID", pl.ToString());
            if(EnableJoinMessages)
            {
                if (!PlayerDatabase.ContainsSetting(pl.SteamID, "Joined"))
                {
                    PlayerDatabase.AddSetting(pl.SteamID, "Joined", "yes");
                    PlayerDatabase.Save();
                    if (EnableFirstjoinMessages)
                    {
                        Logger.Log("[Fougerite-Essentials] " + pl.Name + " has joined the server for the first time");
                        string message = FirstJoinMessage.Replace("{player}", pl.Name);
                        Broadcast(message);
                    }
                }
                if (EnableCountryMessages)
                {
                    FindCountry(pl);
                }
                else
                {
                    string msg = JoinMessage.Replace("{player}", pl.Name);
                    Broadcast(msg);
                }
                PlayerDatabase.Save();
            }                    
        }
        public void OnPlayerDisconnected(Player pl)
        {
            if (EnableLeaveMessages)
            {
                string message = LeaveMessage.Replace("{player}", pl.Name);
                Broadcast(message);
            }
        }
        public void Airdrop(Vector3 vec)
        {
            if (ShowAirdrop)
            {
                foreach (var pl in Server.GetServer().Players)
                {
                    if (pl.IsOnline)
                    {
                        double range = Math.Round(Math.Ceiling(Vector3.Distance(vec, pl.Location)));
                        string message = AirdropIncommingMessage.Replace("{dist}", range.ToString());
                        pl.Message(message);
                    }
                }
            }                      
        }
        public void Command(Player pl, string cmd, string[] args)
        {
            if(cmd == "esshelp")
            {
                pl.Message("Fougerite-Essentials " + Version + " by ice cold");
                pl.Message("/help - see all commands");
                pl.Message("/rules - see all the rules");
                pl.Message("/info - See info about this plugin");
                pl.Message("/friendhelp - See all information about the friends system");
                pl.Message("/sharehelp - See all help about the share system");
                if(EnableHome) { pl.Message("/home - See all help about the home system"); }
                if(EnableRemove) { pl.Message("/remove - Enables/disables remover tool"); }
                if(Enabletpr) { pl.Message("/tpr name - sends teleport request to that user"); pl.Message("/tpa - Accepts an teleport reqequest"); }
                if(EnableWarps) { pl.Message("/warphelp - See all the help for the warping system"); }
                if(pl.Admin || pl.Moderator)
                {
                    pl.Message("/country player - Shows from which country a player comes");
                    pl.Message("/download url filename - downloads that object and put it in the the Downloaded folder EXAMPLE: (/download http://myitem.com myitem.zip)");
                    pl.Message("/addnewspawn - adds a new spawns to the overwrite spawn list");
                }              
            }
            else if(cmd == "country")
            {
                if(pl.Admin || pl.Moderator)
                {
                    if(args.Length != 1) { pl.Message("Usage /country player"); return; }
                    Player target = Server.GetServer().FindPlayer(args[0]);
                    if(target == null) { pl.Message("Couldn't find the target user"); return; }
                    string country = PlayerDatabase.GetSetting(target.SteamID, "Country");
                    pl.Message("[color #ffb90f]" + target.Name  + " is located from [color #c1ffc1]" + country);
                }
            }
            else if(cmd == "download")
            {
                if(pl.Admin)
                {
                    if(args.Length != 2) { pl.Message("Usage /download url filename - EXAMPLE: (/download http://myitem.com myitem.zip)"); return; }
                    using (WebClient web = new WebClient())
                    {
                        web.DownloadFileAsync(new Uri(args[0]), Util.GetRootFolder() + "\\Save\\Fougerite-Essentials\\Downloaded\\" + args[1]);
                        pl.Message("[color #00ced1]Downloading " + args[1] + " to downloaded folder");
                    }
                }
            }
            else if(cmd == "addnewspawn")
            {
                if(pl.Admin)
                {
                    if(EnableNewSpawns)
                    {
                        string[] c = SpawnsOverwrite.EnumSection("SpawnLocations");
                        string co = (Convert.ToInt32(c[c.Length - 1]) + 1).ToString();
                        SpawnsOverwrite.AddSetting("SpawnLocations", co, pl.Location.ToString());
                        SpawnsOverwrite.Save();
                    }                 
                }
            }
            else if(cmd == "warphelp")
            {
                pl.Message("Fougerite-Essentials WarpSystem by ice cold");
                pl.Message("/warp name - Warps you to that location");
                pl.Message("/warps - See all the current warps");
                if(pl.Admin)
                {
                    pl.Message("/warp_set Name");
                    pl.Message("/warp_remove Name");
                }
            }
            else if(cmd == "warp")
            {
                if(args.Length != 1) { pl.Message("Usage /warp name"); return; }
                if(Warps.ContainsSetting("Warps", args[0]))
                {
                    if (pl.Admin)
                    {
                        Vector3 loc = Util.GetUtil().ConvertStringToVector3(Warps.GetSetting("Warps", args[0]));
                        pl.TeleportTo(loc);
                        pl.Message("[color#00bfff]Warped to " + args[0]);
                    }
                    else
                    {
                        if(!warpcd.Contains(pl.UID))
                        {
                            var dict = new Dictionary<string, object>();
                            dict["pl"] = pl;
                            warpcd.Add(pl.UID);
                            Warpcooldown(WarpCooldown * 1000, dict).Start();
                            Warpdelaytimer(WarpDelay * 1000, dict).Start();
                            string message = WarpDelayMessage.Replace("{warpname}", args[0]);
                        }
                        else
                        {
                            pl.Message(WarpCooldownMessage);
                        }
                    }
                }             
            }
            else if(cmd == "warps")
            {
                pl.Message("WarpsList");
                string[] l = Warps.EnumSection("Warps");
                foreach(var num in l)
                {
                    pl.Message(num);
                }
            }
            else if(cmd == "warp_set")
            {
                if(pl.Admin)
                {
                    if(args.Length != 1) { pl.Message("Usage /warp_set Name"); return; }
                    if(Warps.ContainsSetting("Warps", args[0])) { pl.Message("There is already a warp with the name " + args[0]); return; }
                    Warps.AddSetting("Warps", args[0], pl.Location.ToString());
                    Warps.Save();
                    pl.Message("Warp saved");
                }
            }
            else if(cmd == "warp_remove")
            {
                if(pl.Admin)
                {
                    if(args.Length != 1) { pl.Message("Usage /warp_remove Name"); return; }
                    if(!Warps.ContainsSetting("Warps", args[0])) { pl.Message("There is no warp with the name " + args[0]); return; }
                    Warps.DeleteSetting("Warps", args[0]);
                    Warps.Save();
                    pl.Message("The warp " + args[0] + " has been succesfully removed");
                }
            }
            else if(cmd == "help")
            {
                if(EnableHelp)
                {
                    foreach (var h in HelpList.EnumSection("Help"))
                    {
                        string d = HelpList.GetSetting("Help", h);
                        pl.MessageFrom("Help", d);
                    }
                }
                
            }
            else if(cmd == "rules")
            {
                if(EnableRules)
                {
                    foreach (var r in RulesList.EnumSection("Rules"))
                    {
                        string d = RulesList.GetSetting("Help", r);
                        pl.MessageFrom("Help", d);
                    }
                }          
            }
            else if(cmd == "info")
            {
                pl.MessageFrom(Name, "[color #87cefa][Fougerite-Essentials " + Version + " plugin brought by ice cold");
                pl.MessageFrom(Name, "[color #20b2aa]Wanna support ice cold? https://www.patreon.com/uberrust");
                pl.MessageFrom(Name, "[color #8470ff]You can download this plugin at ");
            }
            else if(cmd == "friendhelp")
            {
                pl.Message("[Fougerite-Essentials] Friends system brought by ice cold");
                pl.Message("/addfriend Name - ads player to your friends list");
                pl.Message("/unfriend Name - Unfriend someone");
                pl.Message("/friends - See all your friends");
            }
            else if(cmd == "sharehelp")
            {
                pl.Message("[Fougerite-Essentials] Share system brought by ice cold");
                pl.Message("/share Name - Door/structure share the player");
                pl.Message("/unshare Name - unshare someone");
                pl.Message("/sharelist - See all the people you have shared");
            }
        }
        public void FindCountry(Player pl)
        {
            try
            {
                Country county;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(@"http://ip-api.com/json/" + pl.IP);
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    var json = reader.ReadToEnd();
                    county = JsonConvert.DeserializeObject<Country>(json);
                }
                cou = county.country;
                string message = CountryMessage.Replace("{player}", pl.Name).Replace("{country}", cou);
                Broadcast(message);
                PlayerDatabase.AddSetting(pl.SteamID, "Country", cou);
                PlayerDatabase.Save();
            }
            catch(Exception ex)
            {
                Logger.LogWarning("[Fougerite-Essentials] Failed to get country of " + pl.Name + " (" + pl.IP + ") ");
                Logger.LogError("[Fougerite-Essentials] ERROR:  " + ex);
                PlayerDatabase.AddSetting(pl.SteamID, "Country", "Undefined");
                PlayerDatabase.Save();
                string message = CountryFailMessage.Replace("{player}", pl.Name);
                Broadcast(message);
            }
           
        }

        private void Broadcast(string message)
        {
            Server.GetServer().Broadcast(message);
        }
        private void dircheck()
        {
            if(!Directory.Exists(Util.GetRootFolder() + "\\Save\\Fougerite-Essentials\\Downloaded"))
            {
                Directory.CreateDirectory(Util.GetRootFolder() + "\\Save\\Fougerite-Essentials\\Downloaded");
            }
        }
        private void CheckConfig()
        {
            try
            {
                config = new IniParser(Path.Combine(ModuleFolder, "Configuration.ini"));
                EnableNewSpawns = bool.Parse(config.GetSetting("Global", "EnableNewSpawns"));
                EnableJoinMessages = bool.Parse(config.GetSetting("Global", "EnableJoinMessages"));
                EnableLeaveMessages = bool.Parse(config.GetSetting("Global", "EnableLeaveMessages"));
                EnableCountryMessages = bool.Parse(config.GetSetting("Global", "EnableCountryMessages"));
                EnableFirstjoinMessages = bool.Parse(config.GetSetting("Global", "EnableFirstjoinMessages"));
                EnableWarps = bool.Parse(config.GetSetting("Global", "EnableWarps"));
                EnableRemove = bool.Parse(config.GetSetting("Global", "EnableRemove"));
                EnableHelp = bool.Parse(config.GetSetting("Global", "EnableHelp"));
                EnableRules = bool.Parse(config.GetSetting("Global", "EnableRules"));
                ShowAirdrop = bool.Parse(config.GetSetting("Global", "ShowAirdrop"));
                HurtMessages = bool.Parse(config.GetSetting("Global", "HurtMessages"));
                DeathMessages = bool.Parse(config.GetSetting("Global", "DeathMessages"));
                OverwriteSpawns = bool.Parse(config.GetSetting("Global", "OverwriteSpawns"));
                JoinMessage = config.GetSetting("GlobalMessages", "JoinMessage");
                LeaveMessage = config.GetSetting("GlobalMessages", "LeaveMessage");
                FirstJoinMessage = config.GetSetting("GlobalMessages", "FirstJoinMessage");
                CountryMessage = config.GetSetting("GlobalMessages", "CountryMessage");
                CountryFailMessage = config.GetSetting("Global", "CountryFailMessage");
                TprSendMessage = config.GetSetting("Teleport", "TprSendMessage");
                TprIncommingMessage = config.GetSetting("Teleport", "TprIncommingMessage");
                TprAcceptedMessage = config.GetSetting("Teleport", "TprAcceptedMessage");
                TprAcceptMessage = config.GetSetting("Teleport", "TprAcceptMessage");
                TprBlockMessage = config.GetSetting("Teleport", "TprBlockMessage");
                TprUnderRockMessage = config.GetSetting("Teleport", "TprUnderRockMessage");
                TprMoveMessage = config.GetSetting("Teleport", "TprMoveMessage");
                TprMaxTokens = config.GetSetting("Teleport", "TprMaxTokens");
                TprNoTokensMessage = config.GetSetting("Teleport", "TprNoTokensMessage");
                TprStructureBlock = bool.Parse(config.GetSetting("Teleport", "TprStructureBlock"));
                TprMovecheck = bool.Parse(config.GetSetting("Teleport", "TprMovecheck"));
                Tokens = int.Parse(config.GetSetting("Teleport", "Tokens"));
                Enabletpr = bool.Parse(config.GetSetting("Teleport", "Enabletpr"));
                TprCooldown = int.Parse(config.GetSetting("Teleport", "TprCooldown"));
                TprDelay = int.Parse(config.GetSetting("Teleport", "TprDelay"));
                HomeSetMessage = config.GetSetting("Home", "HomeSetMessage");
                HomeDelayMessage = config.GetSetting("Home", "HomeDelayMessage");
                HomeCooldownMessage = config.GetSetting("Home", "HomeCooldownMessage");
                MaxHomesMessage = config.GetSetting("Home", "MaxHomeMessage");
                NoHomeMessage = config.GetSetting("Home", "NoHomeMessage");
                EnableHome = bool.Parse(config.GetSetting("Home", "EnableHome"));
                SleepingHome = bool.Parse(config.GetSetting("Home", "SleepingHome"));
                HomeMoveCheck = bool.Parse(config.GetSetting("Home", "HomeMoveCheck"));
                StructureHomeBlock = bool.Parse(config.GetSetting("Home", "StructureHomeBlock"));
                CancelHomeOnDamage = bool.Parse(config.GetSetting("Home", "CancelHomeOnDamage"));
                MaxHomes = int.Parse(config.GetSetting("Home", "MaxHomes"));
                HomeCooldown = int.Parse(config.GetSetting("Home", "HomeCooldown"));
                HomeDelay = int.Parse(config.GetSetting("Home", "HomeDelay"));
                RemoveOnMessage = config.GetSetting("Remover", "RemoveOnMessage");
                RemoveOffMessage = config.GetSetting("Remover", "RemoveOffMessage");
                NotAllowedRemoveMessage = config.GetSetting("Remover", "NotAllowedRemoveMessage");
                Removetime = int.Parse(config.GetSetting("Remover", "Removetime"));
                Refund = bool.Parse(config.GetSetting("Remover", "Refund"));
                WarpDelayMessage = config.GetSetting("Warp", "WarpDelayMessage");
                WarpCooldownMessage = config.GetSetting("Warp", "WarpCooldownMessage");
                WarpNotFoundMessage = config.GetSetting("Warp", "WarpNotFoundMessage");
                WarpNotAllowedMessage = config.GetSetting("Warp", "WarpNotAllowedMessage");
                WarpDelay = int.Parse(config.GetSetting("Warp", "WarpDelay"));
                WarpCooldown = int.Parse(config.GetSetting("Warp", "WarpCooldown"));
                WarpMoveCheck = bool.Parse(config.GetSetting("Warp", "WarpMoveCheck"));
                AddFriendMessage = config.GetSetting("Friends System", "AddFriendMessage");
                FriendAddedMessage = config.GetSetting("Friends System", "FriendAddedMessage");
                UnfriendMessage = config.GetSetting("Friends System", "UnfriendMessage");
                AlreadyFriendedMessage = config.GetSetting("Friends System", "AlreadyFriendedMessage");
                HurtWarningPopup = config.GetSetting("Friends System", "HurtWarningpopup");
                SharedMessage = config.GetSetting("Share System", "SharedMessage");
                UnSharedMessage = config.GetSetting("Share System", "UnSharedMessage");
                AlreadySharedMessage = config.GetSetting("Share System", "AlreadySharedMessage");
                AirdropIncommingMessage = config.GetSetting("Airdrop", "AirdropIncommingMessage");
                MinPlayers = int.Parse(config.GetSetting("Airdrop", "MinPlayers"));
                AirdropTimer = int.Parse(config.GetSetting("Airdrop", "AirdropTimer"));
                AmountOfSpawns = int.Parse(config.GetSetting("SpawnsOverwriter", "AmountOfSpawns"));
            }
            catch
            {
                Logger.LogWarning("[Fougerite-Essentials] Oops an unexpected error happend when reading configuration");
            }          
        }
    }
}
