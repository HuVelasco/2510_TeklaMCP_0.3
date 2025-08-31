using System;
using Newtonsoft.Json;
using TeklaMCP.HttpServer.Core;

namespace TeklaMCP.HttpServer.Endpoints
{
    public abstract class BaseEndpoint
    {
        protected readonly TeklaModelManager modelManager;

        protected BaseEndpoint(TeklaModelManager modelManager)
        {
            this.modelManager = modelManager;
        }

        protected bool IsModelConnected()
        {
            return modelManager?.IsConnected ?? false;
        }

        protected void CommitChanges()
        {
            modelManager?.CommitChanges();
        }

        public static string CreateSuccessResponse(string message, object? data = null)
        {
            var response = new
            {
                success = true,
                message = message,
                data = data,
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            return JsonConvert.SerializeObject(response, Formatting.Indented);
        }

        public static string CreateErrorResponse(string error, int statusCode = 400)
        {
            var response = new
            {
                success = false,
                error = error,
                statusCode = statusCode,
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            return JsonConvert.SerializeObject(response, Formatting.Indented);
        }
    }
}