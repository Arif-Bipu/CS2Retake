﻿using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CS2Retake.Entities;
using CS2Retake.Managers.Base;
using CS2Retake.Managers.Interfaces;
using CS2Retake.Utils;
using Microsoft.Extensions.Logging;

namespace CS2Retake.Managers;

public class MapManager : BaseManager, IMapManager
{
    private static MapManager? _instance;

    private MapManager()
    {
    }

    public MapEntity CurrentMap { get; set; }

    public BombSiteEnum BombSite { get; private set; } = BombSiteEnum.Undefined;
    public bool HasToBeInBombZone { get; set; } = true;

    public int TerroristRoundWinStreak { get; set; } = 0;


    public static MapManager Instance
    {
        get
        {
            if (_instance == null) _instance = new MapManager();
            return _instance;
        }
    }


    public void RandomBombSite()
    {
        BombSite = (BombSiteEnum)new Random().Next(0, 2);
    }

    public void AddSpawn(CCSPlayerController player, CsTeam team, BombSiteEnum bombSite)
    {
        if (player == null)
        {
            MessageUtils.Log(LogLevel.Error, "player is null");
            return;
        }

        var isInBombZone = player?.PlayerPawn?.Value?.InBombZone;
        var playerPawn = player?.PlayerPawn.Value;

        if (playerPawn == null)
        {
            MessageUtils.Log(LogLevel.Error, "playerPawn is null");
            return;
        }

        if (playerPawn.AbsOrigin == null)
        {
            MessageUtils.Log(LogLevel.Error, "playerPawn position is null");
            return;
        }

        if (playerPawn.AbsRotation == null)
        {
            MessageUtils.Log(LogLevel.Error, "playerPawn rotation is null");
            return;
        }

        if (isInBombZone == null || !isInBombZone.HasValue)
        {
            MessageUtils.Log(LogLevel.Error, "isInBombZone is null");
            return;
        }

        var newSpawnPoint = new SpawnPointEntity(playerPawn.AbsOrigin, playerPawn.AbsRotation, bombSite, team,
            isInBombZone.Value);

        CurrentMap.SpawnPoints.Add(newSpawnPoint);

        player?.PrintToChat($"SpawnPoint added! BombSite: {bombSite} - Team: {team} - isInBombZone: {isInBombZone}");
    }

    public void TeleportPlayerToSpawn(CCSPlayerController player, int? spawnIndex = null)
    {
        if (BombSite == BombSiteEnum.Undefined) RandomBombSite();

        var team = (CsTeam)player.TeamNum;
        SpawnPointEntity? spawn;
        if (!spawnIndex.HasValue)
        {
            spawn = CurrentMap.GetRandomSpawn(team, BombSite,
                team == CsTeam.CounterTerrorist ? false : HasToBeInBombZone);

            if (team == CsTeam.Terrorist && HasToBeInBombZone) HasToBeInBombZone = false;
        }
        else if (spawnIndex.Value > CurrentMap.SpawnPoints.Count - 1 || spawnIndex.Value < 0)
        {
            return;
        }
        else
        {
            spawn = CurrentMap.SpawnPoints[spawnIndex.Value];
        }

        if (spawn == null)
        {
            MessageUtils.Log(LogLevel.Error, "Spawn is null. Moving player to Spectator");
            player.SwitchTeam(CsTeam.Spectator);
            return;
        }

        spawn.SpawnUsedBy = player;
        player.PrintToConsole($"Spawnpoint: {spawn.SpawnId}");
        player?.PlayerPawn?.Value?.Teleport(spawn.Position, spawn.QAngle, new Vector(0f, 0f, 0f));
    }

    public override void ResetForNextRound(bool completeReset = true)
    {
        if (completeReset)
        {
            RandomBombSite();
            CurrentMap?.ResetSpawnInUse();
        }

        HasToBeInBombZone = true;
    }
}