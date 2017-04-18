using System;
using System.Threading;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.RetryPolicies;

namespace Annytab.AzureLock
{
    /// <summary>
    /// This class is used to aquire, renew and release blob locks
    /// </summary>
    public class BlobLock : IDisposable
    {
        #region Variables

        // Blob variables
        private BlobLockOptions options { get; set; }
        private CloudBlobContainer container { get; set; }
        private CloudBlockBlob blob { get; set; }
        private string leaseId { get; set; }
        private Thread renewalThread { get; set; }

        // Disposing
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
            this.disposed = false;
            this.renewLock = true;

            // Get a storage account
            CloudStorageAccount account = CloudStorageAccount.Parse(this.options.connection_string);

            // Get a client
            CloudBlobClient client = account.CreateCloudBlobClient();

            // Get a reference to a cloud blob contaner
            this.container = client.GetContainerReference(this.options.container_name);

            // Create a task to run async initialization
            Task task = Task.Run(() => InitializeAsync());

            // Wait for the task to complete
            task.Wait();

        } // End of the constructor

        /// <summary>
        /// Helper method to create a blob lock, is used to call async methods
        /// </summary>
        private async Task InitializeAsync()
        {
            // Create a container if it does not exist
            await this.container.CreateIfNotExistsAsync();

            // Get a blob object
            this.blob = this.container.GetBlockBlobReference(this.options.blob_name);

            // Upload a blob if it does not exist
            if (await this.blob.ExistsAsync() == false)
            {
                // Create and use a memory stream
                using (MemoryStream stream = new MemoryStream())
                {
                    await this.blob.UploadFromStreamAsync(stream);
                }
            }

        } // End of the InitializeAsync method

        #endregion

        #region Create methods

        /// <summary>
        /// Create a lock, wait if the lock not could be aquired
        /// </summary>
        /// <returns>A boolean that indicates if the lock is taken</returns>
        public bool CreateOrWait()
        {
            // Try to aquire a blob lock
            while (TryAcquireLease() == false)
            {
                // Sleep times should be random
                Random rnd = new Random();

                // Sleep between 30 and 60 seconds
                Thread.Sleep(rnd.Next(30000, 60000));
            }

            // Renew the lease until the work is done
            RenewLease();

            // Return a success boolean
            return true;

        } // End of the CreateOrWait method

        /// <summary>
        /// Create a lock, do not wait on the lock to be released
        /// </summary>
        /// <returns>A boolean that indicates if the lock is taken</returns>
        public bool CreateOrSkip()
        {
            // Try to aquire a blob lock
            bool success = TryAcquireLease();

            // Renew the lease if the lock is taken
            if (success == true)
            {
                // Renew the lease until the work is done
                RenewLease();
            }

            // Return the success boolean
            return success;

        } // End of the CreateOrSkip method

        #endregion

        #region Lease methods

        /// <summary>
        /// Try to get a lock
        /// </summary>
        /// <returns>A boolean that indicates if the lease was acquired</returns>
        private bool TryAcquireLease()
        {
            // Create a task to acquire a lease on the blob
            Task<string> task = Task.Run<string>(() => this.blob.AcquireLeaseAsync(TimeSpan.FromSeconds(60), null));

            try
            {
                // Wait for the task to complete
                task.Wait();
                
                // Get the lease id
                this.leaseId = task.Result;

                // Return true
                return true;
            }
            catch (AggregateException)
            {
                // There is a lock on the blob, return false
                return false;
            }
            catch (Exception)
            {
                // There is a lock on the blob, return false
                return false;
            }

        } // End of the TryAcquireLease method

        /// <summary>
        /// Renew the lease every 30 seconds
        /// </summary>
        private void RenewLease()
        {
            this.renewalThread = new Thread(() =>
            {
                while (this.renewLock == true)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(30));
                    this.blob.RenewLeaseAsync(new AccessCondition { LeaseId = this.leaseId });
                }
            });
            this.renewalThread.Start();

        } // End of the RenewLease method

        #endregion

        #region Read and write methods

        /// <summary>
        /// Read text from the blob
        /// </summary>
        /// <returns>A string with the contents in the blob</returns>
        public string ReadFrom()
        {
            // Create the string to return
            string text = "";

            // Create and use a memory stream
            using (MemoryStream stream = new MemoryStream())
            {
                // Download the blob to a stream
                Task task = Task.Run(() => this.blob.DownloadToStreamAsync(stream, new AccessCondition { LeaseId = this.leaseId },
                    new BlobRequestOptions { RetryPolicy = new LinearRetry(TimeSpan.FromSeconds(10), 3) }, null));

                // Wait for the task to complete
                task.Wait();

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
        public void WriteTo(string text)
        {
            // Create and use a memory stream
            using (MemoryStream stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(text), false))
            {
                // Write to the blob
                Task task = Task.Run(() => this.blob.UploadFromStreamAsync(stream, new AccessCondition { LeaseId = this.leaseId },
                    new BlobRequestOptions { RetryPolicy = new LinearRetry(TimeSpan.FromSeconds(10), 3) }, null));

                // Wait for the task to complete
                task.Wait();
            }

        } // End of the WriteTo method

        /// <summary>
        /// Read content from the blob
        /// </summary>
        /// <param name="stream">A reference to a stream</param>
        public void ReadFrom(Stream stream)
        {
            // Download the blob to a stream
            Task task = Task.Run(() => this.blob.DownloadToStreamAsync(stream, new AccessCondition { LeaseId = this.leaseId },
                new BlobRequestOptions { RetryPolicy = new LinearRetry(TimeSpan.FromSeconds(10), 3) }, null));

            // Wait for the task to complete
            task.Wait();

        } // End of the ReadFrom method

        /// <summary>
        /// Write content to the blob
        /// </summary>
        public void WriteTo(Stream stream)
        {
            // Write to the blob
            Task task = Task.Run(() => this.blob.UploadFromStreamAsync(stream, new AccessCondition { LeaseId = this.leaseId },
                new BlobRequestOptions { RetryPolicy = new LinearRetry(TimeSpan.FromSeconds(10), 3) }, null));

            // Wait for the task to complete
            task.Wait();

        } // End of the WriteTo method

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
        /// <param name="disposing">True or false</param>
        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
                return;

            if (disposing)
            {
                if (this.renewalThread != null)
                {
                    this.renewLock = false;
                    this.renewalThread.Join();
                    this.blob.ReleaseLeaseAsync(new AccessCondition { LeaseId = this.leaseId });
                    //this.renewalThread = null;
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