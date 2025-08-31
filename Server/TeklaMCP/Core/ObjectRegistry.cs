using System;
using System.Collections.Generic;
using System.Linq;
using Tekla.Structures.Model;

namespace TeklaMCP.HttpServer.Core
{
    public class ObjectRegistry
    {
        private Dictionary<int, TrackedObject> objectsById;
        private Dictionary<string, TrackedObject> objectsByName;
        private int nextAutoId;

        public ObjectRegistry()
        {
            objectsById = new Dictionary<int, TrackedObject>();
            objectsByName = new Dictionary<string, TrackedObject>();
            nextAutoId = 1000; // Start auto-naming from 1000
        }

        public void RegisterObject(ModelObject obj)
        {
            try
            {
                string objectType = obj.GetType().Name;
                string autoName = "";

                if (obj is Beam beam)
                {
                    // Determine if beam or column
                    bool isColumn = IsVerticalBeam(beam);
                    autoName = isColumn ? $"Column-{nextAutoId++}" : $"Beam-{nextAutoId++}";
                }
                else if (obj is Grid grid)
                {
                    autoName = $"Grid-{grid.Name ?? nextAutoId++.ToString()}";
                }
                else
                {
                    autoName = $"{objectType}-{nextAutoId++}";
                }

                var trackedObj = new TrackedObject
                {
                    Id = obj.Identifier.ID,
                    Name = autoName,
                    Type = objectType,
                    CreatedTime = DateTime.Now,
                    ModifiedTime = DateTime.Now,
                    TeklaId = obj.Identifier.ID
                };

                objectsById[obj.Identifier.ID] = trackedObj;
                objectsByName[autoName] = trackedObj;

                Console.WriteLine($"📝 Registered {objectType}: {autoName} (ID: {obj.Identifier.ID})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error registering object: {ex.Message}");
            }
        }

        private bool IsVerticalBeam(Beam beam)
        {
            try
            {
                double deltaZ = Math.Abs(beam.EndPoint.Z - beam.StartPoint.Z);
                double deltaXY = Math.Sqrt(Math.Pow(beam.EndPoint.X - beam.StartPoint.X, 2) +
                                         Math.Pow(beam.EndPoint.Y - beam.StartPoint.Y, 2));
                return deltaZ > deltaXY * 2; // More vertical than horizontal
            }
            catch
            {
                return false;
            }
        }

        public TrackedObject GetObjectById(int id)
        {
            objectsById.TryGetValue(id, out TrackedObject obj);
            return obj;
        }

        public TrackedObject GetObjectByName(string name)
        {
            objectsByName.TryGetValue(name, out TrackedObject obj);
            return obj;
        }

        public List<TrackedObject> GetAllObjects()
        {
            return objectsById.Values.ToList();
        }

        public Dictionary<string, int> GetObjectCounts()
        {
            var counts = new Dictionary<string, int>();
            foreach (var obj in objectsById.Values)
            {
                if (counts.ContainsKey(obj.Type))
                    counts[obj.Type]++;
                else
                    counts[obj.Type] = 1;
            }
            return counts;
        }

        public int GetTotalCount()
        {
            return objectsById.Count;
        }
    }

    public class TrackedObject
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime ModifiedTime { get; set; }
        public int TeklaId { get; set; }
        public string Status { get; set; } = "Active";
    }
}