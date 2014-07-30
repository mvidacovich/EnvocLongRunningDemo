using System;
using System.Configuration;
using System.Text;
using Envoc.AzureLongRunningTask.AzureCommon.Persistance;
using Envoc.AzureLongRunningTask.Web.Properties;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;

namespace Envoc.AzureLongRunningTask.Web
{
    public class BlobConfig
    {
        /// <summary>
        /// Registers CORS for silverlight and html5 runtimes
        /// </summary>
        public static void RegisterCors()
        {
            var storageCredentials = new AzureContext();
            var blobClient = storageCredentials.Account.CreateCloudBlobClient();
            var properties = blobClient.GetServiceProperties();
            properties.DefaultServiceVersion = "2013-08-15";
            properties.Cors.CorsRules.Clear();
            var corsRule = new CorsRule
            {
                AllowedMethods = CorsHttpMethods.Post | CorsHttpMethods.Merge | CorsHttpMethods.Put | CorsHttpMethods.Options,
                MaxAgeInSeconds = (int)TimeSpan.FromHours(1).TotalSeconds
            };
            properties.Cors.CorsRules.Add(corsRule);

            var allowedOrigin = ConfigurationManager.AppSettings["AllowedOrigins"];
            var allowedHeader = ConfigurationManager.AppSettings["AllowedHeaders"];
            var exposedHeader = ConfigurationManager.AppSettings["ExposedHeaders"];

            var allowedOrigins = allowedOrigin.Split(',');
            var allowedHeaders = allowedHeader.Split(',');
            var exposedHeaders = exposedHeader.Split(',');

            foreach (var item in allowedOrigins)
            {
                corsRule.AllowedOrigins.Add(item);
            }
            foreach (var item in allowedHeaders)
            {
                corsRule.AllowedHeaders.Add(item);
            }
            foreach (var item in exposedHeaders)
            {
                corsRule.ExposedHeaders.Add(item);
            }

            var cloudBlobContainer = blobClient.GetContainerReference("$root");
            cloudBlobContainer.CreateIfNotExists(BlobContainerPublicAccessType.Blob);
            cloudBlobContainer.SetPermissions(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });
            blobClient.SetServiceProperties(properties);

            var sb = new StringBuilder();
            foreach (var origin in allowedOrigins)
            {
                sb.Append(string.Format(" <domain uri=\"{0}\"/>", origin));
            }

            var policy = Resources.clientaccesspolicy;
            policy = string.Format(policy, sb, allowedHeader);
            var utf8 = Encoding.UTF8.GetBytes(policy);

            // ISSUE: This policy is overly permissive and should be secured based on application - silverlight only
            // See: http://msdn.microsoft.com/en-us/library/cc197955(v=vs.95).aspx
            var blob = cloudBlobContainer.GetBlockBlobReference("clientaccesspolicy.xml");
            blob.Properties.ContentType = "application/xml";
            blob.Properties.ContentEncoding = "utf-8";
            blob.UploadFromByteArray(utf8, 0, utf8.Length);
            blob.SetProperties();
        }
    }
}