using System;
using System.Collections.Generic;
using Tekla.Structures.Model;
using TeklaMCP.Models;

namespace TeklaMCP.Controllers
{
    /// <summary>
    /// Controller for utility operations following Phase 1 architecture
    /// Handles documentation, patterns, code execution, and system status
    /// </summary>
    public class UtilsController
    {
        public UtilsController()
        {
        }

        /// <summary>
        /// Check Tekla connection status and model information
        /// </summary>
        public ApiResponse GetTeklaStatus()
        {
            try
            {
                var model = new Model();
                var connected = model.GetConnectionStatus();
                
                if (connected)
                {
                    var info = model.GetInfo();
                    return new ApiResponse
                    {
                        Success = true,
                        Data = new
                        {
                            connected = true,
                            modelName = info.ModelName,
                            modelPath = info.ModelPath,
                            currentPhase = info.CurrentPhase,
                            message = "Tekla Structures connected successfully"
                        }
                    };
                }
                
                return new ApiResponse
                {
                    Success = false,
                    Data = new
                    {
                        connected = false,
                        message = "Tekla Structures not connected"
                    }
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    Success = false,
                    Error = $"Exception checking Tekla status: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Get available profiles for specified element type
        /// </summary>
        public ApiResponse GetAvailableProfiles(string elementType)
        {
            try
            {
                var profiles = new Dictionary<string, string[]>
                {
                    ["BEAM"] = new[] { "W21X50", "W24X55", "W27X84", "W30X90", "W33X118" },
                    ["COLUMN"] = new[] { "W14X90", "W14X120", "W14X159", "W14X193", "W14X257" },
                    ["BRACE"] = new[] { "L4X4X1/2", "L5X5X1/2", "L6X6X1/2", "HSS4X4X1/4", "HSS6X6X3/8" }
                };
                
                var normalizedType = elementType?.ToUpper() ?? "BEAM";
                
                return new ApiResponse
                {
                    Success = true,
                    Data = new
                    {
                        elementType = normalizedType,
                        profiles = profiles.ContainsKey(normalizedType) 
                            ? profiles[normalizedType] 
                            : profiles["BEAM"],
                        defaultMaterial = "A992"
                    }
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    Success = false,
                    Error = $"Exception getting profiles: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Execute custom C# code in Tekla environment
        /// Phase 1: Simple placeholder - will be enhanced in Phase 2
        /// </summary>
        public ApiResponse ExecuteCode(string code)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(code))
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Error = "Code cannot be empty"
                    };
                }

                // Phase 1: Basic validation and mock execution
                // Full code execution will be implemented in Phase 2
                var model = new Model();
                if (!model.GetConnectionStatus())
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Error = "Tekla Structures not connected - cannot execute code"
                    };
                }

                // For Phase 1, return success with code validation
                return new ApiResponse
                {
                    Success = true,
                    Data = new
                    {
                        result = "Phase 1: Code validated and ready for execution",
                        codeLength = code.Length,
                        teklaConnected = true,
                        note = "Full code execution will be available in Phase 2"
                    }
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    Success = false,
                    Error = $"Exception executing code: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Get Tekla API patterns and examples
        /// Future enhancement for Phase 2
        /// </summary>
        public ApiResponse GetTeklaPatterns(string topic)
        {
            try
            {
                // Placeholder for future pattern documentation
                var patterns = new Dictionary<string, string>
                {
                    ["beam_creation"] = "Basic beam creation pattern with start/end points",
                    ["column_creation"] = "Vertical column creation pattern",
                    ["connection_creation"] = "Element connection creation pattern"
                };

                return new ApiResponse
                {
                    Success = true,
                    Data = new
                    {
                        topic = topic,
                        available = patterns.ContainsKey(topic?.ToLower() ?? ""),
                        pattern = patterns.ContainsKey(topic?.ToLower() ?? "") 
                            ? patterns[topic.ToLower()] 
                            : "Pattern not found",
                        allPatterns = patterns.Keys
                    }
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    Success = false,
                    Error = $"Exception getting patterns: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Get code examples for specific operations
        /// Future enhancement for Phase 2
        /// </summary>
        public ApiResponse GetCodeExamples(string elementType)
        {
            try
            {
                var examples = new Dictionary<string, string>
                {
                    ["BEAM"] = @"var beam = new Tekla.Structures.Model.Beam();
beam.StartPoint = new Point(0, 0, 0);
beam.EndPoint = new Point(1000, 0, 0);
beam.Profile.ProfileString = ""W21X50"";
beam.Insert();",
                    
                    ["COLUMN"] = @"var column = new Tekla.Structures.Model.Beam();
column.StartPoint = new Point(0, 0, 0);
column.EndPoint = new Point(0, 0, 3000);
column.Profile.ProfileString = ""W14X90"";
column.Insert();"
                };

                var normalizedType = elementType?.ToUpper() ?? "BEAM";

                return new ApiResponse
                {
                    Success = true,
                    Data = new
                    {
                        elementType = normalizedType,
                        example = examples.ContainsKey(normalizedType)
                            ? examples[normalizedType]
                            : examples["BEAM"],
                        availableTypes = examples.Keys
                    }
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    Success = false,
                    Error = $"Exception getting code examples: {ex.Message}"
                };
            }
        }
    }
}