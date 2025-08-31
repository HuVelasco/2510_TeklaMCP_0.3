namespace TeklaMCP.Models
{
    /// <summary>
    /// Generic element creation request as specified in Phase 1 manual
    /// </summary>
    public class ElementRequest
    {
        public string ElementType { get; set; }  // "BEAM", "COLUMN", "BRACE"
        public Point3D StartPoint { get; set; }
        public Point3D EndPoint { get; set; }
        public string Profile { get; set; }
        public string Material { get; set; }
        public string Class { get; set; }
    }

    /// <summary>
    /// 3D Point representation
    /// </summary>
    public class Point3D
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
    }

    /// <summary>
    /// Standardized API response format
    /// </summary>
    public class ApiResponse
    {
        public bool Success { get; set; }
        public object Data { get; set; }
        public string Error { get; set; }
    }
}