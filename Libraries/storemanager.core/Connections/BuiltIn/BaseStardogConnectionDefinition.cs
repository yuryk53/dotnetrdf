using System;
using System.ComponentModel;
using VDS.RDF.Configuration;
using VDS.RDF.Storage;

namespace VDS.RDF.Utilities.StoreManager.Connections.BuiltIn
{
    /// <summary>
    /// Base connection definition for Stardog stores
    /// </summary>
    public abstract class BaseStardogConnectionDefinition
        : BaseHttpServerConnectionDefinition
    {
        protected BaseStardogConnectionDefinition(string storeName, string storeDescrip, Type t) 
            : base(storeName, storeDescrip, t) { }

        /// <summary>
        /// Gets/Sets the Server URI
        /// </summary>
        [Connection(DisplayName = "Server URI", IsRequired = true, AllowEmptyString = false, DisplayOrder = -1, PopulateFrom = ConfigurationLoader.PropertyServer),
         DefaultValue("http://localhost:5820/")]
        public override String Server
        {
            get { return base.Server; }
            set { base.Server = value; }
        }

        /// <summary>
        /// Gets/Sets the Store ID
        /// </summary>
        [Connection(DisplayName = "Store ID", DisplayOrder = 1, AllowEmptyString = false, IsRequired = true, PopulateFrom = ConfigurationLoader.PropertyStore)]
        public String StoreID { get; set; }

        /// <summary>
        /// Gets/Sets the Reasoning Mode for queries
        /// </summary>
        [Connection(DisplayName = "Query Reasoning Mode", DisplayOrder = 2, Type = ConnectionSettingType.Enum, PopulateFrom = ConfigurationLoader.PropertyLoadMode),
         DefaultValue(StardogReasoningMode.None)]
        public StardogReasoningMode ReasoningMode { get; set; }

        /// <summary>
        /// Gets/Sets the Username
        /// </summary>
        [Connection(DisplayName = "Username", DisplayOrder = 3, IsRequired = false, AllowEmptyString = true, PopulateFrom = ConfigurationLoader.PropertyUser)]
        public String Username { get; set; }

        /// <summary>
        /// Gets/Sets the Password
        /// </summary>
        [Connection(DisplayName = "Password", DisplayOrder = 4, IsRequired = false, AllowEmptyString = true, Type = ConnectionSettingType.Password, PopulateFrom = ConfigurationLoader.PropertyPassword)]
        public String Password { get; set; }

        /// <summary>
        /// Gets/Sets whether to use the Stardog anonymous account
        /// </summary>
        [Connection(DisplayName = "Use the Stardog default anonymous user account instead of an explicit Username and Password?", DisplayOrder = 5, Type = ConnectionSettingType.Boolean),
         DefaultValue(false)]
        public bool UseAnonymousAccount { get; set; }
    }
}