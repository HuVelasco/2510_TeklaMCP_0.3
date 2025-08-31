using System;
using System.Collections.Generic;

namespace TeklaMCP.HttpServer.Models
{
    public class ModelInfo
    {
        public string ModelName { get; set; }
        public Dictionary<string, int> ObjectCounts { get; set; }
        public int GridPointCount { get; set; }
        public string ConnectionStatus { get; set; }
    }

    public class ApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
        public string Timestamp { get; set; }
    }

    public class ErrorResponse
    {
        public bool Success { get; set; }
        public string Error { get; set; }
        public int StatusCode { get; set; }
        public string Timestamp { get; set; }
    }

    public class GridCreateRequest
    {
        public string Name { get; set; }
        public string XCoordinates { get; set; }
        public string YCoordinates { get; set; }
        public string ZCoordinates { get; set; }
        public string XLabels { get; set; }
        public string YLabels { get; set; }
        public string ZLabels { get; set; }
        public double ExtensionLeftX { get; set; }
        public double ExtensionRightX { get; set; }
        public double ExtensionLeftY { get; set; }
        public double ExtensionRightY { get; set; }
        public bool IsMagnetic { get; set; }
    }

    public class BeamCreateRequest
    {
        public string Name { get; set; }
        public string Profile { get; set; }
        public string Material { get; set; }
        public string Class { get; set; }
        public string Finish { get; set; }
        public BeamGeometry StartPoint { get; set; }
        public BeamGeometry EndPoint { get; set; }
        public string GridLocation { get; set; }
        public double Height { get; set; }
    }

    public class BeamGeometry
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
    }

    public class BeamModifyRequest
    {
        public int ObjectId { get; set; }
        public string Location { get; set; }
        public string NewProfile { get; set; }
        public string NewMaterial { get; set; }
        public string NewClass { get; set; }
        public string NewFinish { get; set; }
    }
}