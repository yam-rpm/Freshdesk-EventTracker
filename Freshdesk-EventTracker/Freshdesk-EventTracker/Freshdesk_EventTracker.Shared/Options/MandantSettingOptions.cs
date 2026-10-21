using Newtonsoft.Json;

namespace Freshdesk_EventTracker.Shared.Options
{
    public class MandantSettingOptions : IMandantSettingOptions
    {
        [JsonProperty("MandantName")]
        public string MandantName { get; set; }

        [JsonProperty("Encoding")]
        public int Encoding { get; set; }
    }
}
