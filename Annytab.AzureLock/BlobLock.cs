using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Annytab.AzureLock
{
    /// <summary>
    /// This class is used to aquire, renew and release blob locks
    /// </summary>
    public class BlobLock : IDisposable
    {
        #region Variables

        private BlobLockOptions options { get; set; }
        private CloudBlockBlob blob { get; set; }
        private string leaseId { get; set; }
        private Task renewalTask { get; set; }
        private Random rnd { get; set; }
        private bool disposed { get; set; }
        private bool renewLock { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Create a new blob lock
        /// </summary>
        /// <param name="options">A reference to bloblock options</param>
        public BlobLock(BlobLockOptions options)
        {
            // Set values for instance variables
            this.options = options;
            this.blob = null;
            this.leaseId = "";
            this.renewalTask = null;
            this.rnd = new Random();
            this.disposed = false;
            this.renewLock = true;

        } // End of the constructor

        #endregion

        #region Create methods

        /// <summary>
        /// Create a lock, wait if the lock not could be aquired
        /// </summary>
        public async Task<bool> CreateOrWait()
        {
            // Try to aquire a blob lock
            while (await TryAcquireLease() == false)
            {
                // Sleep between 200 and 1000 millisecondes
                await Task.Delay(this.rnd.Next(200, 1000));
            }

            // Renew the lease until the work is done
            await RenewLease();

            // Return a success boolean
            return true;

        } // End of the CreateOrWait method

        /// <summary>
        /// Create a lock, do not wait on the lock to be released
        /// </summary>
        public async Task<bool> CreateOrSkip()
        {
            // Try to aquire a blob lock
            bool success = await TryAcquireLease();

            // Renew the lease if the lock is taken
            if (success == true)
            {
                // Renew the lease until the work is done
                await RenewLease();
            }

            // Return the success boolean
            return success;

        } // End of the CreateOrSkip method

        #endregion

        #region Lease methods

        /// <summary>
        /// Try to get a lock
        /// </summary>
        private async Task<bool> TryAcquireLease()
        {
            // Set a blob reference if it doesnt exist
            await SetBlobReference();

            try
            {
                // Get the lease id
                this.leaseId = await this.blob.AcquireLeaseAsync(TimeSpan.FromSeconds(60), null);

                // Return true
                return true;
            }
            catch (Exception)
            {
                // There is a lock on the blob, return false
                return false;
            }

        } // End of the TryAcquireLease method

        /// <summary>
        /// Renew the lease every 1000 milliseconds
        /// </summary>
        private async Task RenewLease()
        {
            // Set a blob reference
            await SetBlobReference();

            // Create a renewal task
            this.renewalTask = Task.Run(async () =>
            {
                // Loop while true
                while (this.renewLock == true)
                {
                    // Sleep for 1000 milliseconds
                    await Task.Delay(1000);
                    await this.blob.RenewLeaseAsync(new AccessCondition { LeaseId = this.leaseId });
                }

                // Release the lease
                await this.blob.ReleaseLeaseAsync(new AccessCondition { LeaseId = this.leaseId });
            });

        } // End of the RenewLease method

        #endregion

        #region Read and write methods

        /// <summary>
        /// Read text from the blob
        /// </summary>
        /// <returns>A string with the contents in the blob</returns>
        public async Task<string> ReadFrom()
        {
            // Create the string to return
            string text = "";

            // Set a blob reference if it does not exist
            await SetBlobReference();

            // Create and use a memory stream
            using (MemoryStream stream = new MemoryStream())
            {
                // Download the blob to a stream { RetryPolicy = new LinearRetry(TimeSpan.FromSeconds(10), 3) }
                await this.blob.DownloadToStreamAsync(stream, new AccessCondition { LeaseId = this.leaseId },
                    new BlobRequestOptions(), null);

                // Get the text from the stream
                text = System.Text.Encoding.UTF8.GetString(stream.ToArray());
            }

            // Return the string
            return text;

        } // End of the ReadFrom method

        /// <summary>
        /// Write text to the blob
        /// </summary>
        /// <param name="text">Text to be written to the blob</param>
        public async Task WriteTo(string text)
        {
            // Set a blob reference if it does not exist
            await SetBlobReference();

            // Create and use a memory stream
            using (MemoryStream stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(text), false))
            {
                // Write to the blob  { RetryPolicy = new LinearRetry(TimeSpan.FromSeconds(10), 3) }
                await this.blob.UploadFromStreamAsync(stream, new AccessCondition { LeaseId = this.leaseId },
                    new BlobRequestOptions(), null);
            }

        } // End of the WriteTo method

        /// <summary>
        /// Read content from the blob
        /// </summary>
        public async Task ReadFrom(Stream stream)
        {
            // Set a blob reference if it does not exist
            await SetBlobReference();

            // Download the blob to a stream { RetryPolicy = new LinearRetry(TimeSpan.FromSeconds(10), 3) }
            await this.blob.DownloadToStreamAsync(stream, new AccessCondition { LeaseId = this.leaseId },
                new BlobRequestOptions(), null);

        } // End of the ReadFrom method

        /// <summary>
        /// Write contents to the blob
        /// </summary>
        public async Task WriteTo(Stream stream)
        {
            // Set a blob reference if it does not exist
            await SetBlobReference();

            // Write to the blob { RetryPolicy = new LinearRetry(TimeSpan.FromSeconds(10), 3) }
            await this.blob.UploadFromStreamAsync(stream, new AccessCondition { LeaseId = this.leaseId },
                new BlobRequestOptions(), null);

        } // End of the WriteTo method

        #endregion

        #region Helper methods

        /// <summary>
        /// Make sure that there is a blob reference to an existing blob
        /// </summary>
        private async Task<bool> SetBlobReference()
        {
            // Just return if a blob reference already exists
            if(this.blob != null)
            {
                return true;
            }

            // Get a storage account
            CloudStorageAccount account = CloudStorageAccount.Parse(this.options.connection_string);

            // Get a client
            CloudBlobClient client = account.CreateCloudBlobClient();

            // Get a reference to a cloud blob contaner
            CloudBlobContainer container = client.GetContainerReference(this.options.container_name);

            // Create a container if it does not exist
            await container.CreateIfNotExistsAsync();

            // Get a blob object
            this.blob = container.GetBlockBlobReference(this.options.blob_name);

            // Upload a blob if it does not exist
            if (await this.blob.ExistsAsync() == false)
            {
                // Create and use a memory stream
                using (MemoryStream stream = new MemoryStream())
                {
                    await this.blob.UploadFromStreamAsync(stream);
                }
            }

            // Return true
            return true;

        } // End of the SetBlobReference method

        #endregion

        #region Dispose methods

        /// <summary>
        /// Public implementation of Dispose pattern callable by consumers
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);

        } // End of the Dispose method

        /// <summary>
        /// Protected implementation of Dispose pattern
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
                return;

            if (disposing)
            {
                // Dispose of the renewal task if it exists
                if(this.renewalTask != null)
                {
                    this.renewLock = false;
                    this.renewalTask.Wait();
                }
            }

            // Indicate that the object is disposed
            this.disposed = true;

        } // End of the Dispose method

        /// <summary>
        /// The finalizer
        /// </summary>
        ~BlobLock()
        {
            Dispose(false);

        } // End of the Finalizer

        #endregion

    } // End of the class

} // End of the namespace