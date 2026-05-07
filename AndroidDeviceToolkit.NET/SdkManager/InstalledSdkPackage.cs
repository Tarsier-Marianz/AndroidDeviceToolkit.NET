
using System.Runtime.Serialization;

namespace AndroidDeviceToolkit.NET
{
    public partial class SdkManager
    {
        /// <summary>
        /// Installed Android SDK Package information.
        /// </summary>
        [DataContract]
        public class InstalledSdkPackage : SdkPackage
        {
            /// <summary>
            /// Gets or sets the Installed SDK package location.
            /// </summary>
            /// <value>The location.</value>
            [DataMember(Name = "location")]
            public string Location { get; set; }
        }
    }
}
