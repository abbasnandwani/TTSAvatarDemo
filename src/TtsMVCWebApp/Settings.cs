namespace TtsMVCWebApp
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


        public string DemoKBApiEndpoint { get; set; }

    }
}
