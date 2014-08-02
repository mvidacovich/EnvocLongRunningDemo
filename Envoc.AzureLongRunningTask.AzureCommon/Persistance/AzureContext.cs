using System;
using Microsoft.WindowsAzure.Storage;

namespace Envoc.AzureLongRunningTask.AzureCommon.Persistance
{
    public class AzureContext
    {
        public CloudStorageAccount Account { get; private set; }

        public AzureContext(string connectionString)
        {
            CloudStorageAccount account;
            var result = CloudStorageAccount.TryParse(connectionString, out account);
            if (!result)
            {
                throw new ArgumentException(string.Format("{0} is not a valid azure configuration.", connectionString));
            }
            Account = account;
        }

        public AzureContext()
        {
            Account = CloudStorageAccount.DevelopmentStorageAccount;
        }
    }
}
