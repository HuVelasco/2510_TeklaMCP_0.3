using System;
using System.Text.RegularExpressions;

namespace TeklaDrawingExtractor.Utilities
{
    /// <summary>
    /// Parses dimension values and converts to mm
    /// </summary>
    public static class DimensionParser
    {
        /// <summary>
        /// Parse dimension text and extract value in mm
        /// Handles formats: "1524", "5'-0\"", "1.524m", etc.
        /// </summary>
        public static double ParseToMm(string dimensionText)
        {
            if (string.IsNullOrEmpty(dimensionText))
                return 0.0;

            try
            {
                // Remove prefix/postfix and clean up
                string cleaned = dimensionText.Trim();

                // Check for feet-inches format: 5'-0", 12'6", etc.
                var feetInchesMatch = Regex.Match(cleaned, @"(\d+)'\s*-?\s*(\d+(?:\.\d+)?)\""?");
                if (feetInchesMatch.Success)
                {
                    double feet = double.Parse(feetInchesMatch.Groups[1].Value);
                    double inches = double.Parse(feetInchesMatch.Groups[2].Value);
                    return (feet * 12 + inches) * 25.4; // Convert to mm
                }

                // Check for feet only: 5'
                var feetOnlyMatch = Regex.Match(cleaned, @"(\d+(?:\.\d+)?)'");
                if (feetOnlyMatch.Success)
                {
                    double feet = double.Parse(feetOnlyMatch.Groups[1].Value);
                    return feet * 12 * 25.4; // Convert to mm
                }

                // Check for inches: 12", 12.5"
                var inchesMatch = Regex.Match(cleaned, @"(\d+(?:\.\d+?)\""");
                if (inchesMatch.Success)
                {
                    double inches = double.Parse(inchesMatch.Groups[1].Value);
                    return inches * 25.4; // Convert to mm
                }

                // Check for meters: 1.524m, 1.5 m
                var metersMatch = Regex.Match(cleaned, @"(\d+(?:\.\d+)?)\s*m\b", RegexOptions.IgnoreCase);
                if (metersMatch.Success)
                {
                    double meters = double.Parse(metersMatch.Groups[1].Value);
                    return meters * 1000.0; // Convert to mm
                }

                // Check for centimeters: 152.4cm, 152 cm
                var centimetersMatch = Regex.Match(cleaned, @"(\d+(?:\.\d+)?)\s*cm\b", RegexOptions.IgnoreCase);
                if (centimetersMatch.Success)
                {
                    double centimeters = double.Parse(centimetersMatch.Groups[1].Value);
                    return centimeters * 10.0; // Convert to mm
                }

                // Check for millimeters: 1524mm, 1524 mm
                var millimetersMatch = Regex.Match(cleaned, @"(\d+(?:\.\d+)?)\s*mm\b", RegexOptions.IgnoreCase);
                if (millimetersMatch.Success)
                {
                    double millimeters = double.Parse(millimetersMatch.Groups[1].Value);
                    return millimeters; // Already in mm
                }

                // Try to parse as plain number (assume mm)
                var numberMatch = Regex.Match(cleaned, @"(\d+(?:\.\d+)?)");
                if (numberMatch.Success)
                {
                    double value = double.Parse(numberMatch.Groups[1].Value);
                    // If value < 100, might be in meters or feet, but default to mm
                    return value;
                }
            }
            catch
            {
                // Parsing failed
            }

            return 0.0;
        }

        /// <summary>
        /// Extract prefix from dimension text
        /// </summary>
        public static string ExtractPrefix(string fullText, string valueText)
        {
            if (string.IsNullOrEmpty(fullText) || string.IsNullOrEmpty(valueText))
                return "";

            int valueIndex = fullText.IndexOf(valueText);
            if (valueIndex > 0)
            {
                return fullText.Substring(0, valueIndex).Trim();
            }

            return "";
        }

        /// <summary>
        /// Extract postfix from dimension text
        /// </summary>
        public static string ExtractPostfix(string fullText, string valueText)
        {
            if (string.IsNullOrEmpty(fullText) || string.IsNullOrEmpty(valueText))
                return "";

            int valueIndex = fullText.IndexOf(valueText);
            if (valueIndex >= 0)
            {
                int endIndex = valueIndex + valueText.Length;
                if (endIndex < fullText.Length)
                {
                    return fullText.Substring(endIndex).Trim();
                }
            }

            return "";
        }

        /// <summary>
        /// Calculate direction vector from two points (normalized)
        /// </summary>
        public static (double x, double y, double z) CalculateDirectionVector(
            double x1, double y1, double z1,
            double x2, double y2, double z2)
        {
            double dx = x2 - x1;
            double dy = y2 - y1;
            double dz = z2 - z1;

            // Normalize
            double length = Math.Sqrt(dx * dx + dy * dy + dz * dz);
            if (length > 0.001)
            {
                dx /= length;
                dy /= length;
                dz /= length;
            }

            return (Math.Round(dx, 4), Math.Round(dy, 4), Math.Round(dz, 4));
        }
    }
}
