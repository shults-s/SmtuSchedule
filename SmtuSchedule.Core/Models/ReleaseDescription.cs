using System;
using Newtonsoft.Json;

namespace SmtuSchedule.Core.Models
{
    public class ReleaseDescription
    {
        public String GooglePlayMarketPackageId { get; private set; }

        [JsonProperty(Required = Required.Always)]
        public String VersionName { get; private set; }

        [JsonProperty(Required = Required.Always)]
        public Int32 VersionCode { get; private set; }

        public static ReleaseDescription FromJson(String json)
        {
            return JsonConvert.DeserializeObject<ReleaseDescription>(json);
        }
    }
}