using System;
using System.Threading;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;

namespace Annytab.AzureLock
{
    /// <summary>
    /// This class is used to aquire, renew and release blob locks
    /// </summary>
    public class BlobLock : IDisposable
    {
        #region Variables

        private CloudBlockBlob blob;
        private string leaseId;
        private Thread renewalThread;

        // Used for the disposable interface
        private bool disposed = false;

        #endregion

        #region Constructors

        /// <summary>
        /// Create a new blob lock
        /// </summary>
        /// <param name="connectionString">A connection string to a azure storage account</param>
        /// <param name="containerName">A name for the container</param>
        /// <param name="blobName">A name for the blob</param>
        public BlobLock(string connectionString, string containerName, string blobName)
        {
            // Get a storage account
            CloudStorageAccount account = CloudStorageAccount.Parse(connectionString);

            // Get a client
            CloudBlobClient client = account.CreateCloudBlobClient();

            // Get a reference to a cloud blob contaner
            CloudBlobContainer container = client.GetContainerReference(containerName);

            // Create a container if it does not exist
            container.CreateIfNotExists();

            // Get a blob object
            blob = container.GetBlockBlobReference(blobName);

            // Upload a blob if it does not exist
            if (blob.Exists() == false)
            {
                // Create and use a memory stream
                using (MemoryStream stream = new MemoryStream())
                {
                    blob.UploadFromStream(stream);
                }
            }

        } // End of the constructor

        #endregion

        #region Create methods

        /// <summary>
        /// Create a lock, wait if the lock not could be aquired
        /// </summary>
        /// <returns>A boolean that indicates if the lock is taken</returns>
        public bool CreateOrWait()
        {
            // Try to aquire a blob lock
            while (TryAcquireLease(this.blob) == false)
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
            bool success = TryAcquireLease(this.blob);

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
        /// <param name="blob">A reference to a blob</param>
        /// <returns>A boolean that indicates if the lease was acquired</returns>
        private bool TryAcquireLease(CloudBlockBlob blob)
        {
            try
            {
                // Acquire a lease on the blob
                this.leaseId = blob.AcquireLease(TimeSpan.FromSeconds(60), null);

                // Return true
                return true;
            }
            catch (Exception ex)
            {
                // There is a lock on the blob, return false
                string exMessage = ex.Message;
                return false;
            }

        } // End of the TryAcquireLease method

        /// <summary>
        /// Renew the lease every 40 seconds
        /// </summary>
        private void RenewLease()
        {
            renewalThread = new Thread(() =>
            {
                while (true)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(40));
                    blob.RenewLease(new AccessCondition { LeaseId = this.leaseId });
                }
            });
            renewalThread.Start();

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
                this.blob.DownloadToStream(stream, new AccessCondition { LeaseId = this.leaseId });

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
                this.blob.UploadFromStream(stream, new AccessCondition { LeaseId = this.leaseId });
            }

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
            if (disposed)
                return;

            if (disposing)
            {
                if (renewalThread != null)
                {
                    renewalThread.Abort();
                    blob.ReleaseLease(new AccessCondition { LeaseId = this.leaseId });
                    renewalThread = null;
                }
            }

            // Indicate that the object is disposed
            disposed = true;

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