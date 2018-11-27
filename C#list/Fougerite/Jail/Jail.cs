using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//refrences
using Fougerite;
using Fougerite.Events;
using UnityEngine;
using System.IO;

namespace Jail
{
    public class Jail : Module
    {
        public IniParser ini;
        public IniParser list;

        DataStore ds = DataStore.GetInstance();
        Server server = Server.GetServer();
        Util util = Util.GetUtil();

        public string ArrestMessage = "You have been sended to jail";
        public string FreeMessage = "{inmate} is now free from jail";
        public string GlobalSendMessage = "{inmate} Adlı Oyuncu {reason} Sebebinden {time} Dakika Hapse Atıldı !";
        public string ResCommands = "tpr,tpa,home,kit,kits,hg,warp";

        public bool AllowMods = false;
        public bool AdminProtection = true;
        public bool ProtectJail = true;

        //colors
        private string cadet1 = "[color #8ee5ee]";
        private string cornflower = "[color #6495ed]";
        private string green = "[color #b4eeb4]";

        public List<string> RestrictedCommands;

        public Dictionary<string, int> radius = new Dictionary<string, int> { };

        public override string Name { get { return "Jail"; } }
        public override string Author { get { return "ice cold"; } }
        public override string Description { get { return "jail"; } }
        public override Version Version { get { return new Version("1.2"); } }

        public override void Initialize()
        {
            try
            {
                RestrictedCommands = new List<string>();

                Hooks.OnCommand += Command;
                Hooks.OnPlayerSpawned += Spawned;
                Hooks.OnPlayerHurt += PlayerHurt;
                Hooks.OnEntityDeployedWithPlacer += Deploy;
                Hooks.OnEntityHurt += EntityHurt;
                Resendinmates();
                CheckConfig();

                if (ini.ContainsSetting("JailRadius", "radius"))
                {
                    int rad = Convert.ToInt32(ini.GetSetting("JailRadius", "radius"));

                    if (radius.ContainsKey("radius"))
                    {
                        radius.Remove("radius");
                        radius.Add("radius", rad);
                    }
                    else
                    {
                        radius.Add("radius", rad);
                    }
                }
           
            }
            catch(Exception ex)
            {
                Logger.Log("Error happend at Initialize " + ex);
            }
        }

        public void CheckConfig()
        {
            try
            {
                if (!File.Exists(Path.Combine(ModuleFolder, "Settings.cfg")))
                {
                    File.Create(Path.Combine(ModuleFolder, "Settings.cfg")).Dispose();
                    ini = new IniParser(Path.Combine(ModuleFolder, "Settings.cfg"));
                    ini.AddSetting("Options", "AllowMods", AllowMods.ToString());
                    ini.AddSetting("Options", "AdminProtection", AdminProtection.ToString());
                    ini.AddSetting("Options", "ProtectJail", ProtectJail.ToString());
                    ini.AddSetting("Options", "ResCommands", ResCommands.ToString());
                    ini.AddSetting("Messages", "ArrestMessage", ArrestMessage.ToString());
                    ini.AddSetting("Messages", "FreeMessage", FreeMessage.ToString());
                    ini.AddSetting("Messages", "GlobalSendMessage", GlobalSendMessage.ToString());
                    ini.Save();
                }
                else
                { 
                    ini = new IniParser(Path.Combine(ModuleFolder, "Settings.cfg"));
                    AllowMods = bool.Parse(ini.GetSetting("Options", "AllowMods"));
                    AdminProtection = bool.Parse(ini.GetSetting("Options", "AdminProtection"));
                    ProtectJail = bool.Parse(ini.GetSetting("Options", "ProtectJail"));
                    var cmds = ini.GetSetting("Options", "ResCommands").Split(Convert.ToChar(","));
                    ArrestMessage = ini.GetSetting("Messages", "ArrestMessage");
                    FreeMessage = ini.GetSetting("Messages", "FreeMessage");
                    GlobalSendMessage = ini.GetSetting("Messages", "GlobalSendMessage");
                    foreach (var x in cmds)
                    {
                        RestrictedCommands.Add(x);
                    }
                }  
                if(!File.Exists(Path.Combine(ModuleFolder, "List.ini")))
                {
                    File.Create(Path.Combine(ModuleFolder, "List.ini")).Dispose();
                    list = new IniParser(Path.Combine(ModuleFolder, "List.ini"));
                    list.Save();
                }
                else
                {
                    list = new IniParser(Path.Combine(ModuleFolder, "List.ini"));
                }
            }
            catch(Exception ex)
            {
                Logger.Log("Error at making jail config   " + ex);
            }
        }

        public override void DeInitialize()
        {
            Hooks.OnCommand -= Command;
            Hooks.OnPlayerSpawned -= Spawned;
            Hooks.OnPlayerHurt -= PlayerHurt;
            Hooks.OnEntityDeployedWithPlacer -= Deploy;
            Hooks.OnEntityHurt -= EntityHurt;
        }

        public void Resendinmates()
        {
            foreach (var pl in server.Players)
            {
                if (list.ContainsSetting("inmates", pl.SteamID))
                {
                    if (ini.ContainsSetting("jailloc", "location"))
                    {
                        string j = ini.GetSetting("jailloc", "location");
                        Vector3 loc = util.ConvertStringToVector3(j);
                        pl.TeleportTo(loc);
                        pl.MessageFrom(Name, "You have been teleported to jail");
                        Notify("All inmates are resended to jail");
                    }
                }
            }
        }

        private void Notify(string v)
        {
            foreach (var pl in server.Players)
            {
                if (pl.Admin || pl.Moderator && AllowMods)
                {
                    pl.MessageFrom(Name, v);
                }
            }
        }

        public void Command(Player sender, string cmd, string[] args)
        {
            if (cmd == "jail")
            {
                if (sender.Admin || sender.Moderator && AllowMods)
                {
                    sender.MessageFrom(Name, cornflower + "/jail_send player time reason - Oyuncuyu Hapse Gönderir.");
                    sender.MessageFrom(Name, cornflower + "/jail_set radius - Yeni Hapis Ayarlar.");
                    sender.MessageFrom(Name, cornflower + "/jail_del - Hapsi Kaldırır.");
                    sender.MessageFrom(Name, cornflower + "/jail_free player - Oyuncuyu Serbest Bırakır.");
                }
            }
            else if (cmd == "jail_send")
            {
                if (sender.Admin || sender.Moderator && AllowMods)
                {
                    if (args.Length != 3)
                    {
                        sender.Notice("☢", "Hapis için - /jail_send player time reason", 20f);
                        return;
                    }
                    Player inmate = server.FindPlayer(args[0]);
                    if (inmate == null) { sender.Notice("☢", "Hedef Oyuncu Bulunamadı !", 10f); return; }
                    if (!inmate.IsAlive) { sender.Notice("☢", inmate.Name + " seems to be dead wait for him to respawn", 10f); return; }
                    if (list.ContainsSetting("inmates", inmate.SteamID)) { sender.Notice("☢", inmate.Name + " Adlı Oyuncu Zaten Hapiste !", 10f); return; }
                    if (!ini.ContainsSetting("jailloc", "location")) { sender.Notice("☢", "Hapiste Olmalısın !", 10f); return; }
                    if (!radius.ContainsKey("radius")) { sender.Notice("☢", "Hapihane Yarı Çapı Bulunamadı, Lütfen Tekrar Ayarlayın..", 10f); return; }
                    if (inmate.Admin && AdminProtection) { sender.Notice("☢", "Adminler Hapise Giremez !", 10f); return; }
                    int time = Convert.ToInt32(args[1]);
                    Vector3 loc = util.ConvertStringToVector3(ini.GetSetting("jailloc", "location"));
                    string message = GlobalSendMessage.Replace("{inmate}", inmate.Name).Replace("{sender}", sender.Name).Replace("{time}", args[1]).Replace("{reason}", args[2]);
                    list.AddSetting("inmates", inmate.SteamID, inmate.Name);
                    list.AddSetting("jailreasons", inmate.SteamID, args[2]);
                    list.AddSetting("previouslocations", inmate.SteamID, inmate.Location.ToString());
                    list.Save();
                    ds.Add("inmates", inmate.SteamID, "injail");
                    ds.Save();
                    inmate.MessageFrom(Name, ArrestMessage);
                    server.BroadcastFrom(Name, message);
                    inmate.TeleportTo(loc);
                    inmate.AdjustCalorieLevel(100f);
                    var dict = new Dictionary<string, object>();
                    dict["inmate"] = inmate;
                    Jailtimer(time * 60000, dict).Start();
                    BlockCommands(inmate);
                    Logger.Log("[Jail] " + inmate.Name + " was sended to jail by " + sender.Name + " for " + time + " minutes, for " + args[2]);
                }
                else
                {
                    sender.Notice("✘", "Bu Komut için Yeterli Yetkin Yok !", 10f);
                }
            }
            else if (cmd == "jail_set")
            {
                if (sender.Admin || sender.Moderator && AllowMods)
                {
                    if (args.Length != 1)
                    {
                        sender.Notice("☢", "/jail_set Radius");
                        return;
                    }
                    if (ini.ContainsSetting("jailloc", "location"))
                    {
                        sender.Notice("☢", "Yeni Bir Hapisane Yaratmadan Bir Öncekini Kaldırın !");
                        return;
                    }
                    int rad = Convert.ToInt32(args[0]);
                    Vector3 loc = sender.Location;
                    SetJail(loc, rad);
                    sender.MessageFrom(Name, "Jail saved at " + sender.Location + " with a radius of " + rad);
                }
            }
            else if (cmd == "jail_del")
            {
                if (sender.Admin || sender.Moderator && AllowMods)
                {
                    if (!ini.ContainsSetting("jailloc", "location"))
                    {
                        sender.Notice("☢", "Hapishane Bulunamadı !", 10f);
                        return;
                    }
                    ini.DeleteSetting("jailloc", "location");
                    ini.DeleteSetting("JailRadius", "radius");
                    radius.Remove("radius");
                    ini.Save();
                    sender.Notice("☢", "Hapishane Başarıyla Kaldırıldı !", 10f);
                }
            }
            else if (cmd == "jail_free")
            {
                if (sender.Admin || sender.Moderator && AllowMods)
                {
                    if (args.Length != 1)
                    {
                        sender.Notice("☢", "Usage /jail_free player", 10f);
                        return;
                    }
                    Player inmate = server.FindPlayer(args[0]);
                    if (inmate == null) { sender.Notice("☢", "Hedef Oyuncu Bulunamadı !", 10f); return; }
                    if (!ds.ContainsKey("inmates", inmate.SteamID)) { sender.Notice("☢", inmate.Name + " isnt in jail", 10f); return; }
                    list.DeleteSetting("inmates", inmate.SteamID);
                    list.DeleteSetting("jailreasons", inmate.SteamID);
                    Vector3 loc = util.ConvertStringToVector3(list.GetSetting("previouslocations", inmate.SteamID));
                    inmate.TeleportTo(loc);
                    list.DeleteSetting("previouslocations", inmate.SteamID);
                    string message = FreeMessage.Replace("inmate", inmate.Name);
                    server.BroadcastFrom(Name, message);
                    ini.Save();
                    ds.Remove("inmates", inmate.SteamID);
                    ds.Save();
                }
            }
        }
        private void SetJail(Vector3 loc, int rad)
        {
            radius.Add("radius", rad);
            ini.AddSetting("JailRadius", "radius", rad.ToString());
            ini.AddSetting("jailloc", "location", loc.ToString());
            ini.Save();
        }
        private void BlockCommands(Player inmate)
        {
            foreach (var x in RestrictedCommands)
            {
                inmate.RestrictCommand(x);
            }
        }
        public void Spawned(Player pl, SpawnEvent se)
        {
            if (ds.ContainsKey("inmates", pl.SteamID))
            {
                var dict = new Dictionary<string, object>();
                dict["user"] = pl;
                Spawntimer(5 * 1000, dict).Start();
                pl.MessageFrom(Name, "Tutuklandın ve Hapishaneye Gönderiliyorsun - 5 Saniye");
            }
        }
        public void PlayerHurt(HurtEvent he)
        {
            if (he.AttackerIsPlayer && he.VictimIsPlayer)
            {
                Player attacker = (Player)he.Attacker;

                if (ds.ContainsKey("inmates", attacker.SteamID))
                {
                    he.DamageAmount = 0f;
                    attacker.MessageFrom(Name, "Hapisteyken Kimseyi Öldüremezsin !");
                }
            }
        }
        public void Deploy(Player pl, Entity ent, Player actualplayer)
        {
            if (ds.ContainsKey("inmates", actualplayer.SteamID))
            {
                ent.Destroy();
                actualplayer.MessageFrom(Name, "Hapisteyken Yapı Yapamazsın !");
            }
            Vector3 pos = (Vector3)ds.Get("jailloc", "location");
            float dist = Vector3.Distance(actualplayer.Location, pos);
            int dist2 = (int)ds.Get("JailRadius", "radius");
            int d = Convert.ToInt32(dist);
            if (d < dist2)
            {
                ent.Destroy();
                actualplayer.MessageFrom(Name, "Hapishane Bölgesinde Yapı Yapılması Yasaktır !");
            }
        }
        public void EntityHurt(HurtEvent he)
        {
            if (he.AttackerIsPlayer)
            {
                Player attacker = (Player)he.Attacker;
                Entity ent = he.Entity;

                string weapon = he.DamageType;

                if (weapon == "Explosion")
                {
                    Vector3 pos = (Vector3)ds.Get("jailloc", "location");
                    float dist = Vector3.Distance(attacker.Location, pos);
                    int dist2 = (int)ds.Get("JailRadius", "radius");
                    int d = Convert.ToInt32(dist);

                    if (d < dist2)
                    {
                        he.DamageAmount = 0f;
                        attacker.MessageFrom(Name, "Hapishaneye RAİD Atamazsın !");
                    }
                }
            }
        }
        public TimedEvent Spawntimer(int timeoutDelay, Dictionary<string, object> args)
        {
            TimedEvent timedEvent = new TimedEvent(timeoutDelay);
            timedEvent.Args = args;
            timedEvent.OnFire += CallBack;
            return timedEvent;
        }
        public void CallBack(TimedEvent e)
        {
            var dict = e.Args;
            e.Kill();
            Player pl = (Player)dict["user"];
            Vector3 loc = (Vector3)ds.Get("jailloc", "location");
            pl.TeleportTo(loc);
            pl.MessageFrom(Name, "Telported to jail");
        }
        public TimedEvent Jailtimer(int timeoutDelay, Dictionary<string, object> args)
        {
            TimedEvent timedEvent = new TimedEvent(timeoutDelay);
            timedEvent.Args = args;
            timedEvent.OnFire += CallBack1;
            return timedEvent;
        }
        public void CallBack1(TimedEvent e)
        {
            var dict = e.Args;
            e.Kill();
            Player pl = (Player)dict["inmate"];
            if(ds.ContainsKey("inmates", pl.SteamID))
            {
                Vector3 loc = util.ConvertStringToVector3(list.GetSetting("previouslocations", pl.SteamID));
                pl.TeleportTo(loc);
                list.DeleteSetting("previouslocations", pl.SteamID);
                ds.Remove("inmates", pl.SteamID);
                list.DeleteSetting("inmates", pl.SteamID);
                list.DeleteSetting("jailreasons", pl.SteamID);
                list.Save();
                string message = FreeMessage.Replace("{inmate}", pl.Name);
                server.BroadcastFrom(Name, message);
            }
        }
    }
}
