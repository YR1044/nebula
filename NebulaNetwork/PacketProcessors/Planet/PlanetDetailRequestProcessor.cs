﻿using NebulaAPI;
using NebulaModel.Logger;
using NebulaModel.Networking;
using NebulaModel.Packets;
using NebulaModel.Packets.Planet;
using System;
using System.Threading.Tasks;

namespace NebulaNetwork.PacketProcessors.Planet
{
    [RegisterPacketProcessor]
    public class PlanetDetailRequestProcessor : PacketProcessor<PlanetDetailRequest>
    {
        public override void ProcessPacket(PlanetDetailRequest packet, NebulaConnection conn)
        {
            if (IsClient)
            {
                return;
            }
            PlanetData planetData = GameMain.galaxy.PlanetById(packet.PlanetID);
            if (!planetData.calculated)
            {
                planetData.calculating = true;
                Task.Run(() =>
                {
                    try
                    {
                        // Modify from PlanetModelingManager.PlanetCalculateThreadMain()
                        HighStopwatch highStopwatch = new HighStopwatch();
                        highStopwatch.Begin();
                        planetData.data = new PlanetRawData(planetData.precision);
                        planetData.modData = planetData.data.InitModData(planetData.modData);
                        planetData.data.CalcVerts();
                        planetData.aux = new PlanetAuxData(planetData);
                        PlanetAlgorithm planetAlgorithm = PlanetModelingManager.Algorithm(planetData);
                        planetAlgorithm.GenerateTerrain(planetData.mod_x, planetData.mod_y);
                        planetAlgorithm.CalcWaterPercent();
                        if (planetData.type != EPlanetType.Gas)
                        {
                            planetAlgorithm.GenerateVegetables();
                            planetAlgorithm.GenerateVeins();
                        }
                        planetData.CalculateVeinGroups();
                        planetData.GenBirthPoints();
                        planetData.NotifyCalculated();
                        conn.SendPacket(new PlanetDetailResponse(planetData.id, planetData.runtimeVeinGroups ?? Array.Empty<VeinGroup>(), planetData.landPercent));
                        Log.Info($"PlanetCalculateThread:{planetData.displayName} time:{highStopwatch.duration:F4}s");
                    }
                    catch (Exception e)
                    {
                        Log.Warn($"Error when calculating {planetData.displayName}");
                        Log.Warn(e);
                    }
                });
                return;
            }
            conn.SendPacket(new PlanetDetailResponse(planetData.id, planetData.runtimeVeinGroups ?? Array.Empty<VeinGroup>(), planetData.landPercent));
        }
    }
}
