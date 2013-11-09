using System;
using Microsoft.WindowsAzure.Storage;

namespace Envoc.Azure.Common.Persistance
{
    public class AzureContext
    {
        internal CloudStorageAccount Account { get; private set; }

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
