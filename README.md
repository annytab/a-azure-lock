# a-azure-lock
A blob lock written in ASP.NET and C# to use in multi-instance applications on Azure. A blob lock needs a connection string to the storage account, a container name and a name for the blob file, these settings is added as options to the constructor.

You can upload a stream of content to the locked cloud block blob and download the contents from the locked cloud block blob as a stream. You can write text to and read text from the locked cloud block blob.

If you want to test the lock with the test program, add a appsettings.Development.json (copy of appsettings.json) or modify the appsettings.json file.

This blob lock is available as a NuGet package: <a href="https://www.nuget.org/packages/Annytab.AzureLock/">a-azure-lock (NuGet Gallery)</a>

Example, Create a lock or wait for the lock to be released:
<pre>
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
        Thread.Sleep(TimeSpan.FromMinutes(3));
    }
}
</pre>

Example, Create a lock or skip if the lock is taken:
<pre>
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
</pre>

Example, Upload a stream (image):
<pre>
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
</pre>

Example, Download a stream (image):
<pre>
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
</pre>
