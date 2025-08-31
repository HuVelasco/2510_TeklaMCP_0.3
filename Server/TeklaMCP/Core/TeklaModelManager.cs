using System;
using System.Collections.Generic;
using Tekla.Structures.Model;
using Tekla.Structures.Geometry3d;
using TeklaMCP.Models;

namespace TeklaMCP.Core
{
    public class TeklaModelManager
    {
        private Model teklaModel = null!;
        private ObjectRegistry objectRegistry = null!;
        private GridManager gridManager = null!;

        public Model Model => teklaModel;
        public ObjectRegistry ObjectRegistry => objectRegistry;
        public GridManager GridManager => gridManager;

        public bool IsConnected => teklaModel != null && teklaModel.GetConnectionStatus();

        public bool Initialize()
        {
            try
            {
                teklaModel = new Model();
                if (!teklaModel.GetConnectionStatus())
                {
                    Console.WriteLine("ERROR: Could not connect to Tekla Structures!");
                    Console.WriteLine("Make sure Tekla is running and try again.");
                    return false;
                }
                Console.WriteLine("✅ Connected to Tekla Structures!");

                // Initialize systems
                objectRegistry = new ObjectRegistry();
                gridManager = new GridManager();

                // Cache existing objects
                CacheExistingObjects();
                gridManager.CacheGridPoints(teklaModel);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR connecting to Tekla: {ex.Message}");
                return false;
            }
        }

        public void CommitChanges()
        {
            try
            {
                if (IsConnected)
                {
                    teklaModel.CommitChanges();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error committing changes: {ex.Message}");
            }
        }

        public Models.ModelInfo GetModelInfo()
        {
            if (!IsConnected) return null!;

            try
            {
                var modelInfo = teklaModel.GetInfo();
                return new Models.ModelInfo
                {
                    ModelName = modelInfo?.ModelName ?? "Unknown",
                    ObjectCounts = objectRegistry.GetObjectCounts(),
                    GridPointCount = gridManager.GetGridPointCount(),
                    ConnectionStatus = "Connected"
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting model info: {ex.Message}");
                return null!;
            }
        }

        private void CacheExistingObjects()
        {
            try
            {
                Console.WriteLine("🔍 Scanning existing model objects...");

                var modelObjects = teklaModel.GetModelObjectSelector().GetAllObjects();
                int beamCount = 0, gridCount = 0;

                while (modelObjects.MoveNext())
                {
                    if (modelObjects.Current is Beam beam)
                    {
                        objectRegistry.RegisterObject(beam);
                        beamCount++;
                    }
                    else if (modelObjects.Current is Grid grid)
                    {
                        objectRegistry.RegisterObject(grid);
                        gridCount++;
                    }
                }

                Console.WriteLine($"✅ Cached {beamCount} beams, {gridCount} grids, {objectRegistry.GetTotalCount()} total objects");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error caching existing objects: {ex.Message}");
            }
        }
    }
}