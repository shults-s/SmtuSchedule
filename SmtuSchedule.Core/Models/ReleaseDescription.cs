using System;
using System.Text.Json;
using SmtuSchedule.Core.Exceptions;

namespace SmtuSchedule.Core.Models
{
    // При сборке с параметром Связывание = Сборки пакета SDK и пользователя,
    // компилятор неведомым образом удаляет все свойства класса, в результате
    // чего при десереализации значения в них не записываются.
    [Android.Runtime.Preserve(AllMembers = true)]
    public sealed class ReleaseDescription
    {
        public String? GooglePlayStorePackageId { get; set; }

        public String? VersionNotes { get; set; }

        public String VersionName { get; set; }

        public Int32 VersionCode { get; set; }

        public Boolean IsCriticalUpdate { get; set; }

        public ReleaseDescription() => VersionName = String.Empty;

        public static ReleaseDescription FromJson(String json)
        {
            return JsonSerializer.Deserialize<ReleaseDescription>(json);
        }

        public ReleaseDescription Validate()
        {
            if (VersionCode == default(Int32))
            {
                throw new ValidationException("Property 'VersionCode' must be set");
            }

            if (String.IsNullOrWhiteSpace(VersionName))
            {
                throw new ValidationException("Property 'VersionName' must be set");
            }

            return this;
        }
    }
}