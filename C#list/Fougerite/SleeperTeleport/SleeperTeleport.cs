using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fougerite;
using UnityEngine;
using System.IO;

namespace SleeperTeleport
{
    public class SleeperTeleport : Fougerite.Module
    {
        public IniParser locs;
        public override string Name { get { return "SleeperTeleport"; } }
        public override string Author { get { return "ice cold"; } }
        public override string Description { get { return "Allows you to teleport to offline players"; } }
        public override Version Version { get { return new Version("1.0"); } }

        public override void Initialize()
        {
            Fougerite.Hooks.OnPlayerDisconnected += Leave;
            Fougerite.Hooks.OnCommand += Command;

            if (!File.Exists(Path.Combine(ModuleFolder, "Locs")))
            {
                File.Create(Path.Combine(ModuleFolder, "locs")).Dispose();
                locs = new IniParser(Path.Combine(ModuleFolder, "locs"));
                locs.Save();
            }
            else
            {
                locs = new IniParser(Path.Combine(ModuleFolder, "locs"));
            }
        }
        public override void DeInitialize()
        {
            Fougerite.Hooks.OnPlayerDisconnected -= Leave;
            Fougerite.Hooks.OnCommand -= Command;
        }
        void Command(Player pl, string cmd, string[] args)
        {
            if (cmd == "stp" && pl.Admin)
            {
                if (args.Length < 1)
                {
                    pl.Notice("!", "/stp Player ID");

                    string[] en = locs.EnumSection("Locs");
                    // Loops trough the ini folder and sends all id into a list
                    foreach (var n in en)
                    {
                        pl.MessageFrom(Name, n);
                    }
                }
                else
                {
                    if (locs.ContainsSetting("Locs", args[0]))
                    {
                        string j = locs.GetSetting("Locs", args[0]);
                        Vector3 loc = Util.GetUtil().ConvertStringToVector3(j); // makes vector3 of the location which is saved into string
                        pl.TeleportTo(loc);
                        pl.InventoryNotice(loc.ToString());
                    }
                }
            }
        }
        void Leave(Player pl)
        {
            if(locs.ContainsSetting("Locs", pl.SteamID))
            {
                locs.DeleteSetting("Locs", pl.SteamID);
                locs.AddSetting("Locs", pl.SteamID, pl.DisconnectLocation.ToString());
                locs.Save();
            }
            else
            {
                locs.AddSetting("Locs", pl.SteamID, pl.DisconnectLocation.ToString());
                locs.Save();
            }
        }
    }
}
