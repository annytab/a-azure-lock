# a-azure-lock
A blob lock written in ASP.NET and C# to use in multi instance applications on Azure. A blob lock needs a connection string to the storage account, a container name and a name for the blob file.

Example, Create a lock or wait for the lock to be released:
<pre>
// Use a blob lock, the lock is disposed by the using block
using (BlobLock blobLock = new BlobLock("DefaultEndpointsProtocol=https;AccountName=XXX;AccountKey=XXX", "locks", "locations.lck"))
{
    // Do work inside a blob lock
    if (blobLock.CreateOrWait() == true)
    {
        Debug.WriteLine("Thread " + threadId.ToString() + ": Has lock for 1 minute.");

        // Sleep for 1 minute
        Thread.Sleep(TimeSpan.FromMinutes(1));
    }
}
</pre>

Example, Create a lock or skip if the lock is taken:
<pre>
// Use a blob lock, the lock is disposed by the using block
using (BlobLock blobLock = new BlobLock("DefaultEndpointsProtocol=https;AccountName=XXX;AccountKey=XXX", "locks", "locations.lck"))
{
    // Do work inside a blob lock
    if (blobLock.CreateOrSkip() == true)
    {
        Debug.WriteLine("Thread " + threadId.ToString() + ": Has lock for 1 minute.");

        // Sleep for 1 minute
        Thread.Sleep(TimeSpan.FromMinutes(1));
    }
    else
    {
        Debug.WriteLine("Thread " + threadId.ToString() + ": Does not wait for the lock to be released.");
    }
}
</pre>
