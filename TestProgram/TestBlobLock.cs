using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
using Microsoft.Extensions.Configuration;
using Annytab.AzureLock;

namespace TestProgram
{
    /// <summary>
    /// This class is used to test blob locks
    /// </summary>
    [TestClass]
    public class TestBlobLock
    {
        #region Variables

        private IConfigurationRoot configuration { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Create a new test instance
        /// </summary>
        public TestBlobLock()
        {
            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.SetBasePath(Directory.GetCurrentDirectory());
            builder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            builder.AddJsonFile($"appsettings.Development.json", optional: true);
            this.configuration = builder.Build();

        } // End of the constructor

        #endregion

        [TestMethod]
        public void TestToCreateOrWaitForLock()
        {
            Task[] tasks = new[]
            {
                Task.Factory.StartNew(() => CreateOrWaitForLock(1)),
                Task.Factory.StartNew(() => CreateOrWaitForLock(2)),
                Task.Factory.StartNew(() => CreateOrWaitForLock(3)),
                Task.Factory.StartNew(() => CreateOrWaitForLock(4))
            };
            Task.WaitAll(tasks);

        } // End of the TestToCreateOrWaitForLock method

        [TestMethod]
        public void TestToCreateOrSkipLock()
        {
            Task[] tasks = new[]
            {
                Task.Factory.StartNew(() => CreateOrSkipLock(1)),
                Task.Factory.StartNew(() => CreateOrSkipLock(2)),
                Task.Factory.StartNew(() => CreateOrSkipLock(3)),
                Task.Factory.StartNew(() => CreateOrSkipLock(4))
            };
            Task.WaitAll(tasks);

        } // End of the TestToCreateOrSkipLock method

        [TestMethod]
        public void TestToUploadImage()
        {
            Task[] tasks = new[]
            {
                Task.Factory.StartNew(() => UploadImage(1)),
                Task.Factory.StartNew(() => UploadImage(2)),
                Task.Factory.StartNew(() => UploadImage(3)),
                Task.Factory.StartNew(() => UploadImage(4))
            };
            Task.WaitAll(tasks);

        } // End of the TestToUploadImage method

        [TestMethod]
        public void TestToDownloadImage()
        {
            Task[] tasks = new[]
            {
                Task.Factory.StartNew(() => DownloadImage(1)),
                Task.Factory.StartNew(() => DownloadImage(2)),
                Task.Factory.StartNew(() => DownloadImage(3)),
                Task.Factory.StartNew(() => DownloadImage(4))
            };
            Task.WaitAll(tasks);

        } // End of the TestToDownloadImage method

        /// <summary>
        /// Create or wait for a lock
        /// </summary>
        /// <param name="threadId">A thread id</param>
        private void CreateOrWaitForLock(Int32 threadId)
        {
            // Add options
            BlobLockOptions options = new BlobLockOptions();
            options.connection_string = this.configuration.GetSection("AppSettings")["AzureStorageAccount"];
            options.container_name = "locks";
            options.blob_name = "test.lck";

            // Use a blob lock, the lock is disposed by the using block
            using (BlobLock blobLock = new BlobLock(options))
            {
                // Do work inside a blob lock
                if (blobLock.CreateOrWait() == true)
                {
                    Logger.LogMessage("Thread " + threadId.ToString() + ": Has lock for 1 minute. Date: " + DateTime.UtcNow.ToString("s"));

                    // Read from the blob
                    Logger.LogMessage("Text: " + blobLock.ReadFrom());

                    // Sleep for 1 minute
                    Thread.Sleep(TimeSpan.FromMinutes(1));
                }
            }

        } // End of the CreateOrWaitForLock method

        /// <summary>
        /// Create a lock or skip if the lock is taken
        /// </summary>
        /// <param name="threadId">A thread id</param>
        private void CreateOrSkipLock(Int32 threadId)
        {
            // Add options
            BlobLockOptions options = new BlobLockOptions();
            options.connection_string = this.configuration.GetSection("AppSettings")["AzureStorageAccount"];
            options.container_name = "locks";
            options.blob_name = "test.lck";

            // Use a blob lock, the lock is disposed by the using block
            using (BlobLock blobLock = new BlobLock(options))
            {
                // Do work inside a blob lock
                if (blobLock.CreateOrSkip() == true)
                {
                    Logger.LogMessage("Thread " + threadId.ToString() + ": Has lock for 1 minute.");

                    // Sleep for 1 minute
                    Thread.Sleep(TimeSpan.FromMinutes(1));

                    // Write to the blob
                    blobLock.WriteTo("65");

                }
                else
                {
                    Logger.LogMessage("Thread " + threadId.ToString() + ": Does not wait for the lock to be released.");
                }
            }

        } // End of the CreateOrSkipLock method

        /// <summary>
        /// Upload an image to the blob
        /// </summary>
        /// <param name="threadId">A thread id</param>
        private void UploadImage(Int32 threadId)
        {
            // Add options
            BlobLockOptions options = new BlobLockOptions()
            {
                connection_string = this.configuration.GetSection("AppSettings")["AzureStorageAccount"],
                container_name = "locks",
                blob_name = "image.jpg"
            };

            // Use a blob lock, the lock is disposed by the using block
            using (BlobLock blobLock = new BlobLock(options))
            {
                // Do work inside a blob lock
                if (blobLock.CreateOrSkip() == true)
                {
                    Logger.LogMessage("Thread " + threadId.ToString() + ": Has lock for 1 minute.");

                    // Upload the image
                    using (FileStream fileStream = File.OpenRead(@"D:\Bilder\1960.jpg"))
                    {
                        blobLock.WriteTo(fileStream);
                    }

                    Logger.LogMessage("Thread " + threadId.ToString() + ": Image has been uploaded.");
                }
                else
                {
                    Logger.LogMessage("Thread " + threadId.ToString() + ": Does not wait for the lock to be released.");
                }
            }

        } // End of the UploadImage method

        /// <summary>
        /// Download an image from the blob
        /// </summary>
        /// <param name="threadId">A thread id</param>
        private void DownloadImage(Int32 threadId)
        {
            // Add options
            BlobLockOptions options = new BlobLockOptions()
            {
                connection_string = this.configuration.GetSection("AppSettings")["AzureStorageAccount"],
                container_name = "locks",
                blob_name = "image.jpg"
            };

            // Use a blob lock, the lock is disposed by the using block
            using (BlobLock blobLock = new BlobLock(options))
            {
                // Do work inside a blob lock
                if (blobLock.CreateOrSkip() == true)
                {
                    Logger.LogMessage("Thread " + threadId.ToString() + ": Has lock for 1 minute.");

                    // Download an image to a file
                    using (FileStream fileStream = File.OpenWrite(@"D:\Bilder\Azure-blob-image.jpg"))
                    {
                        blobLock.ReadFrom(fileStream);
                    }

                    Logger.LogMessage("Thread " + threadId.ToString() + ": Image has been downloaded.");
                }
                else
                {
                    Logger.LogMessage("Thread " + threadId.ToString() + ": Does not wait for the lock to be released.");
                }
            }

        } // End of the DownloadImage method

    } // End of the class
    
} // End of the namespace