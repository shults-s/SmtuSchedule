using System;
using System.Text.Json;
// using Newtonsoft.Json;
using SmtuSchedule.Core.Exceptions;

namespace SmtuSchedule.Core.Models
{
    public class ReleaseDescription
    {
        // [JsonProperty(Required = Required.DisallowNull)]
        public String GooglePlayStorePackageId { get; set; }

        // [JsonProperty(Required = Required.DisallowNull)]
        public String VersionNotes { get; set; }

        // [JsonProperty(Required = Required.Always)]
        public String VersionName { get; set; }

        // [JsonProperty(Required = Required.Always)]
        public Int32 VersionCode { get; set; }

        // [JsonProperty(Required = Required.Always)]
        public Boolean IsCriticalUpdate { get; set; }

        public static ReleaseDescription FromJson(String json)
        {
            return JsonSerializer.Deserialize<ReleaseDescription>(json);
        }

        public ReleaseDescription Validate()
        {
            if (VersionCode == default(Int32))
            {
                throw new ValidationException("Property VersionCode must be set");
            }

            if (String.IsNullOrEmpty(VersionName))
            {
                throw new ValidationException("Property VersionName must be set");
            }

            return this;
        }
    }
}