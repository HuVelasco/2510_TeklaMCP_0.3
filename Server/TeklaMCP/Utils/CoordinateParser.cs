using System;
using System.Collections.Generic;

namespace TeklaMCP.HttpServer.Utils
{
    public static class CoordinateParser
    {
        public static List<double> ParseCoordinateString(string coordString)
        {
            List<double> positions = new List<double>();

            if (string.IsNullOrEmpty(coordString)) return positions;

            string[] parts = coordString.Split(' ');
            double currentPos = 0.0;

            foreach (string part in parts)
            {
                if (part.Contains('*'))
                {
                    // Handle format like "3*6000.00"
                    string[] multiplier = part.Split('*');
                    if (multiplier.Length == 2 &&
                        int.TryParse(multiplier[0], out int count) &&
                        double.TryParse(multiplier[1], out double spacing))
                    {
                        for (int i = 0; i < count; i++)
                        {
                            currentPos += spacing;
                            positions.Add(currentPos);
                        }
                    }
                }
                else if (double.TryParse(part, out double value))
                {
                    currentPos = value;
                    positions.Add(currentPos);
                }
            }

            return positions;
        }

        public static string FormatCoordinateString(List<double> positions)
        {
            if (positions == null || positions.Count == 0)
                return "0.00";

            List<string> parts = new List<string>();
            
            for (int i = 0; i < positions.Count; i++)
            {
                if (i == 0)
                {
                    parts.Add(positions[i].ToString("F2"));
                }
                else
                {
                    double spacing = positions[i] - positions[i - 1];
                    parts.Add(spacing.ToString("F2"));
                }
            }

            return string.Join(" ", parts);
        }

        public static List<double> ParseSimpleSpacing(double startPosition, int count, double spacing)
        {
            List<double> positions = new List<double>();
            
            for (int i = 0; i < count; i++)
            {
                positions.Add(startPosition + (i * spacing));
            }

            return positions;
        }

        public static bool TryParseGridLocation(string location, out string xLabel, out string yLabel)
        {
            xLabel = "";
            yLabel = "";

            if (string.IsNullOrEmpty(location))
                return false;

            var parts = location.Split('-');
            if (parts.Length == 2)
            {
                xLabel = parts[0].Trim();
                yLabel = parts[1].Trim();
                return true;
            }

            return false;
        }

        public static double ConvertFeetToMm(double feet)
        {
            return feet * 304.8;
        }

        public static double ConvertMmToFeet(double mm)
        {
            return mm / 304.8;
        }

        public static double ConvertInchesToMm(double inches)
        {
            return inches * 25.4;
        }

        public static double ConvertMmToInches(double mm)
        {
            return mm / 25.4;
        }
    }
}