namespace Annytab.AzureLock
{
    /// <summary>
    /// This class represent bloblock options
    /// </summary>
    public class BlobLockOptions
    {
        #region Variables

        /// <summary>
        /// A connection string to an azure storage account
        /// </summary>
        public string connection_string { get; set; }

        /// <summary>
        /// A name for the container
        /// </summary>
        public string container_name { get; set; }

        /// <summary>
        /// A name for the blob
        /// </summary>
        public string blob_name { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Create new bloblock options with default properties
        /// </summary>
        public BlobLockOptions()
        {
            // Set values for instance variables
            this.connection_string = "";
            this.container_name = "";
            this.blob_name = "";

        } // End of the constructor

        #endregion

    } // End of the class

} // End of the namespace