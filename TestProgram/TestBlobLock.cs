using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Annytab.AzureLock;

/// <summary>
/// This class is used to test blob locks
/// </summary>
[TestClass]
public class TestBlobLock
{
    // Variables
    private static string CONNECTION_STRING = "DefaultEndpointsProtocol=https;AccountName=XXX;AccountKey=XXX;";

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

    /// <summary>
    /// Create or wait for a lock
    /// </summary>
    /// <param name="threadId">A thread id</param>
    private void CreateOrWaitForLock(Int32 threadId)
    {
        // Use a blob lock, the lock is disposed by the using block
        using (BlobLock blobLock = new BlobLock(CONNECTION_STRING, "locks", "test.lck"))
        {
            // Do work inside a blob lock
            if (blobLock.CreateOrWait() == true)
            {
                Debug.WriteLine("Thread " + threadId.ToString() + ": Has lock for 1 minute.");

                // Read from the blob
                Debug.WriteLine("Text: " + blobLock.ReadFrom());

                // Sleep for 1 minute
                Thread.Sleep(TimeSpan.FromMinutes(1));
            }
        }

        //// Create a blob lock variable
        //BlobLock blobLock = null;

        //try
        //{
        //    // Create a blob lock
        //    blobLock = new BlobLock(CONNECTION_STRING, "locks", "locations.lck");

        //    // Do work inside a blob lock
        //    if (blobLock.CreateOrWait() == true)
        //    {
        //        Debug.WriteLine("Thread " + threadId.ToString() + ": Has lock for 1 minute.");

        //        // Sleep for 1 minute
        //        Thread.Sleep(TimeSpan.FromMinutes(1));
        //    }
        //}
        //catch (Exception ex)
        //{
        //    throw ex;
        //}
        //finally
        //{
        //    // Dispose of the blob lock
        //    if (blobLock != null)
        //    {
        //        blobLock.Dispose();
        //    }
        //}

    } // End of the CreateOrWaitForLock method

    /// <summary>
    /// Create a lock or skip if the lock is taken
    /// </summary>
    /// <param name="threadId">A thread id</param>
    private void CreateOrSkipLock(Int32 threadId)
    {
        // Use a blob lock, the lock is disposed by the using block
        using (BlobLock blobLock = new BlobLock(CONNECTION_STRING, "locks", "test.lck"))
        {
            // Do work inside a blob lock
            if (blobLock.CreateOrSkip() == true)
            {
                Debug.WriteLine("Thread " + threadId.ToString() + ": Has lock for 1 minute.");

                // Write to the blob
                blobLock.WriteTo("54");

                // Sleep for 1 minute
                Thread.Sleep(TimeSpan.FromMinutes(1));
            }
            else
            {
                Debug.WriteLine("Thread " + threadId.ToString() + ": Does not wait for the lock to be released.");
            }
        }

        //// Create a blob lock variable
        //BlobLock blobLock = null;

        //try
        //{
        //    // Create a blob lock
        //    blobLock = new BlobLock(CONNECTION_STRING, "locks", "locations.lck");

        //    // Do work inside a blob lock
        //    if (blobLock.CreateOrSkip() == true)
        //    {
        //        Debug.WriteLine("Thread " + threadId.ToString() + ": Has lock for 1 minute.");

        //        // Sleep for 1 minute
        //        Thread.Sleep(TimeSpan.FromMinutes(1));
        //    }
        //    else
        //    {
        //        Debug.WriteLine("Thread " + threadId.ToString() + ": Does not wait for the lock to be released.");
        //    }
        //}
        //catch (Exception ex)
        //{
        //    throw ex;
        //}
        //finally
        //{
        //    // Dispose of the blob lock
        //    if (blobLock != null)
        //    {
        //        blobLock.Dispose();
        //    }
        //}

    } // End of the CreateOrSkipLock method

} // End of the class