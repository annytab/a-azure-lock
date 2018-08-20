namespace Annytab.AzureLock
{
    /// <summary>
    /// This class represent bloblock options
    /// </summary>
    public class BlobLockOptions
    {
        #region Variables

        public string connection_string { get; set; }
        public string container_name { get; set; }
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