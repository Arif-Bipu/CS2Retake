﻿using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CS2Retake.Entities;
using CS2Retake.Managers;
using CS2Retake.Utils;

namespace CS2Retake
{
    public class CS2Retake : BasePlugin  
    {
        public override string ModuleName => "CS2Retake";
        public override string ModuleVersion => "1.0.0";
        public override string ModuleAuthor => "LordFetznschaedl";
        public override string ModuleDescription => "Retake Plugin implementation for CS2";

        public override void Load(bool hotReload)
        {
            this.Log(PluginInfo());
            this.Log(this.ModuleDescription);

            MessageUtils.ModuleName = this.ModuleName;
            WeaponManager.Instance.ModuleDirectory = this.ModuleDirectory;

            if (MapManager.Instance.CurrentMap == null)
            {
                this.OnMapStart(Server.MapName);
            }

            _ = new CounterStrikeSharp.API.Modules.Timers.Timer(7 * 60, MessageUtils.ThankYouMessage, CounterStrikeSharp.API.Modules.Timers.TimerFlags.REPEAT);

            this.RegisterListener<Listeners.OnMapStart>(mapname => this.OnMapStart(mapname));

            this.RegisterEventHandler<EventRoundFreezeEnd>(OnRoundFreezeEnd);
            this.RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
            this.RegisterEventHandler<EventCsPreRestart>(OnCsPreRestart);
            this.RegisterEventHandler<EventBombBeginplant>(OnBombBeginPlant);
            this.RegisterEventHandler<EventBombPlanted>(OnBombPlanted);
        }


        [ConsoleCommand("css_retakeinfo", "This command prints the plugin information")]
        public void OnCommandInfo(CCSPlayerController? player, CommandInfo command)
        {
            command.ReplyToCommand(PluginInfo());
        }

        [ConsoleCommand("css_retakespawn", "This command teleports the player to a spawn with the given index in the args")]
        [RequiresPermissions("@cs2retake/admin")]
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
                command.ReplyToCommand($"One argument with a valid spawn index is needed! Example: !retakespawn <index (int)>");
                return;
            }

            if(!int.TryParse(command.ArgByIndex(1), out int spawnIndex))
            {
                this.Log("Argument index not a valid integer!");
                return;
            }

            MapManager.Instance.TeleportPlayerToSpawn(player, spawnIndex);
        }

        [ConsoleCommand("css_retakewrite", "This command writes the spawns for the current map")]
        [RequiresPermissions("@cs2retake/admin")]
        public void OnCommandWrite(CCSPlayerController? player, CommandInfo command)
        {
            MapManager.Instance.CurrentMap.SaveSpawns();
        }

        [ConsoleCommand("css_retakeread", "This command reads the spawns for the current map")]
        [RequiresPermissions("@cs2retake/admin")]
        public void OnCommandRead(CCSPlayerController? player, CommandInfo command)
        {
            MapManager.Instance.CurrentMap.LoadSpawns();
            this.Log($"{MapManager.Instance.CurrentMap.SpawnPoints.Count} spawnpoints read");
        }

        [ConsoleCommand("css_retakescramble", "This command scrambles the teams")]
        [RequiresPermissions("@cs2retake/admin")]
        public void OnCommandScramble(CCSPlayerController? player, CommandInfo command)
        {
            RetakeManager.Instance.ScrambleTeams();
        }

        [ConsoleCommand("css_retaketeleport", "This command teleports the player to the given coordinates")]
        [RequiresPermissions("@cs2retake/admin")]
        public void OnCommandTeleport(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null)
            {
                this.Log("Command has been called by the server.");
                return;
            }
            if (!player.PlayerPawn.IsValid)
            {
                this.Log("PlayerPawn not valid");
                return;
            }

            if (command.ArgCount != 4)
            {
                this.Log($"ArgCount: {command.ArgCount} - ArgString: {command.ArgString}");
                command.ReplyToCommand($"Command format: !retaketeleport <position X float> <position Y float> <position Z float>");
                return;
            }

            if (!float.TryParse(command.ArgByIndex(1), out float positionX))
            {
                this.Log("Argument position X not a valid float!");
                return;
            }

            if (!float.TryParse(command.ArgByIndex(2), out float positionY))
            {
                this.Log("Argument position Y not a valid float!");
                return;
            }

            if (!float.TryParse(command.ArgByIndex(3), out float positionZ))
            {
                this.Log("Argument position Z not a valid float!");
                return;
            }

            player.PlayerPawn.Value.Teleport(new Vector(positionX, positionY, positionZ), new QAngle(0f,0f,0f), new Vector(0f, 0f, 0f));
        }

        [ConsoleCommand("css_retakeaddspawn", "This command adds a new spawn to the current map")]
        [RequiresPermissions("@cs2retake/admin")]
        public void OnCommandAdd(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null)
            {
                this.Log("Command has been called by the server.");
                return;
            }
            if (!player.PlayerPawn.IsValid)
            {
                this.Log("PlayerPawn not valid");
                return;
            }

            if (command.ArgCount != 3)
            {
                this.Log($"ArgCount: {command.ArgCount} - ArgString: {command.ArgString}");
                command.ReplyToCommand($"Command format: !retakeaddspawn <2/3 - 2 = T; 3 = CT> <0/1 - 0 = A; 1 = B>");
                return;
            }

            if (!int.TryParse(command.ArgByIndex(1), out int team))
            {
                this.Log("Team could not be parsed!");
                return;
            }

            if(team != 2 && team != 3) 
            {
                this.Log("Team index is not in 2 or 3");
                return;
            }

            if (!int.TryParse(command.ArgByIndex(2), out int bombSite))
            {
                this.Log("Team could not be parsed!");
                return;
            }

            if (bombSite != 0 && bombSite != 1)
            {
                this.Log("BombSite index is not in 0 or 1");
                return;
            }

            MapManager.Instance.AddSpawn(player, (CsTeam)team, (BombSiteEnum)bombSite);
        }

        [GameEventHandler]
        public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
        {
            if (@event == null)
            {
                return HookResult.Continue;
            }
            if(@event.Userid == null || !@event.Userid.IsValid)
            {
                return HookResult.Continue;
            }
            if(@event.Userid.PlayerPawn == null || !@event.Userid.PlayerPawn.IsValid)
            {
                return HookResult.Continue;
            }
            if (@event.Userid.PlayerPawn.Value == null || !@event.Userid.PlayerPawn.Value.IsValid)
            {
                return HookResult.Continue;
            }

            MapManager.Instance.TeleportPlayerToSpawn(@event.Userid);
            WeaponManager.Instance.RemoveWeapons(@event.Userid);
            WeaponManager.Instance.AssignWeapon(@event.Userid);

            return HookResult.Continue;
        }

        [GameEventHandler]
        private HookResult OnRoundFreezeEnd(EventRoundFreezeEnd @event, GameEventInfo info)
        {
            RetakeManager.Instance.GiveBombToPlayerRandomPlayerInBombZone();

            return HookResult.Continue;
        }

        [GameEventHandler]
        private HookResult OnBombBeginPlant(EventBombBeginplant @event, GameEventInfo info)
        {
            RetakeManager.Instance.FastPlantBomb();

            return HookResult.Continue;
        }

        [GameEventHandler]
        private HookResult OnBombPlanted(EventBombPlanted @event, GameEventInfo info)
        {
            RetakeManager.Instance.BombHasBeenPlanted = true;

            return HookResult.Continue;
        }

        [GameEventHandler]
        private HookResult OnCsPreRestart(EventCsPreRestart @event, GameEventInfo info)
        {
            MapManager.Instance.ResetForNextRound(false);
            RetakeManager.Instance.ResetForNextRound();

            MessageUtils.PrintToChatAll($"Bombsite: {ChatColors.Darkred}{MapManager.Instance.BombSite}{ChatColors.White} - Roundtype: {ChatColors.Darkred}{WeaponManager.Instance.RoundType}{ChatColors.White}");

            return HookResult.Continue;
        }

        [GameEventHandler]
        private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
        {
            this.Log("OnRoundEnd");

            if (@event.Winner == (int)CsTeam.Terrorist)
            {
                MapManager.Instance.TerroristRoundWinStreak++;
                MessageUtils.PrintToChatAll($"The Terrorists have won {ChatColors.Darkred}{MapManager.Instance.TerroristRoundWinStreak}{ChatColors.White} rounds subsequently.");
            }
            else
            {
                MessageUtils.PrintToChatAll($"The Counter-Terrorists have won!");
                MapManager.Instance.TerroristRoundWinStreak = 0;
                RetakeManager.Instance.SwitchTeams();
            }

            if(MapManager.Instance.TerroristRoundWinStreak == 5)
            {
                MessageUtils.PrintToChatAll($"Teams will be scrambled now!");
                MapManager.Instance.TerroristRoundWinStreak = 0;
                RetakeManager.Instance.ScrambleTeams();
            }

            MapManager.Instance.ResetForNextRound();
            WeaponManager.Instance.ResetForNextRound();

            return HookResult.Continue;
        }

        public void OnMapStart(string mapName)
        {
            this.Log($"Map changed to {mapName}");
            MapManager.Instance.CurrentMap = new MapEntity(Server.MapName, this.ModuleDirectory);
            RetakeManager.Instance.ConfigureForRetake();
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