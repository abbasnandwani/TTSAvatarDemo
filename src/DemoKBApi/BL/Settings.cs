using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace DemoKBApi.BL
{
    public class Settings
    {
        private static Settings _settings = null;
        private Settings()
        {

        }

        public static Settings GetSettings()
        {
            if (_settings == null)
            {
                _settings = new Settings();
            }

            return _settings;
        }


        public string AISearchEndpoint { get; set; }
        public string AISearchApiKey { get; set; }
        public string AISearchIndex { get; set; }

    }
}
