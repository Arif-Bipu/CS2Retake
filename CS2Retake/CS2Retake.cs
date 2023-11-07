﻿using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using CS2Retake.Entity;
using CS2Retake.Logic;

namespace CS2Retake
{
    public class CS2Retake : BasePlugin  
    {
        public override string ModuleName => "CS2Retake";
        public override string ModuleVersion => "0.0.1";
        public override string ModuleAuthor => "LordFetznschaedl";
        public override string ModuleDescription => "Retake Plugin implementation for CS2";
       

        public override void Load(bool hotReload)
        {
            this.Log(PluginInfo());
            this.Log(this.ModuleDescription);

            RetakeLogic.GetInstance().ModuleName= this.ModuleName;
            MapLogic.GetInstance().ModuleName= this.ModuleName;

            this.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
            this.RegisterEventHandler<EventRoundStart>(OnRoundStart);

            if (MapLogic.GetInstance().CurrentMap == null)
            {
                MapLogic.GetInstance().CurrentMap = new MapEntity(Server.MapName, this.ModuleDirectory, this.ModuleName);
            }

            this.RegisterListener<Listeners.OnMapStart>((mapName) =>
            {
                this.Log($"Map changed to {mapName}");
                MapLogic.GetInstance().CurrentMap = new MapEntity(Server.MapName, this.ModuleDirectory, this.ModuleName);
            });
        }


        [ConsoleCommand("css_retakeinfo", "This command prints the plugin information")]
        public void OnCommandInfo(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null)
            {
                this.Log("Command has been called by the server.");
                return;
            }

            command.ReplyToCommand(PluginInfo());
        }

        [ConsoleCommand("css_retakespawn", "This command teleports the player to a spawn with the given index in the args")]
        public void OnCommandSpawn(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null)
            {
                this.Log("Command has been called by the server.");
                return;
            }
            if(!player.PlayerPawn.IsValid)
            {
                this.Log("PlayerPawn not valid");
                return;
            }

            if (command.ArgCount != 2)
            {
                this.Log($"ArgCount: {command.ArgCount} - ArgString: {command.ArgString}");
                command.ReplyToCommand($"One argument with a valid spawn index is needed! Example: !retakespawn 0");
                return;
            }

            if(!int.TryParse(command.ArgByIndex(1), out int spawnIndex))
            {
                this.Log("Argument index not a valid integer!");
                return;
            }

            MapLogic.GetInstance().CurrentMap.TeleportPlayerToSpawn(player, spawnIndex);
        }

        [ConsoleCommand("css_retakewrite", "This command writes the spawns for the current map")]
        public void OnCommandWrite(CCSPlayerController? player, CommandInfo command)
        {
            MapLogic.GetInstance().CurrentMap.WriteSpawns();
        }

        [ConsoleCommand("css_retakeread", "This command reads the spawns for the current map")]
        public void OnCommandRead(CCSPlayerController? player, CommandInfo command)
        {
            MapLogic.GetInstance().CurrentMap.ReadSpawns();
            this.Log($"{MapLogic.GetInstance().CurrentMap.SpawnPoints.Count} spawnpoints read");
        }

        [ConsoleCommand("css_retakescramble", "This command reads the spawns for the current map")]
        public void OnCommandScramble(CCSPlayerController? player, CommandInfo command)
        {
            RetakeLogic.GetInstance().ScrambleTeams();
        }

        [GameEventHandler]
        public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
        {
            if (@event == null)
            {
                return HookResult.Continue;
            }
            if(!@event.Userid.IsValid)
            {
                return HookResult.Continue;
            }

            MapLogic.GetInstance().CurrentMap.TeleportPlayerToSpawn(@event.Userid);

            return HookResult.Continue;
        }

        [GameEventHandler]
        private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            MapLogic.GetInstance().CurrentMap.ResetSpawnInUse();

            return HookResult.Continue;
        }


        private string PluginInfo()
        {
            return $"Plugin: {this.ModuleName} - Version: {this.ModuleVersion} by {this.ModuleAuthor}";
        }

        private void Log(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[{this.ModuleName}] {message}");
            Console.ResetColor();
        }
    }
}