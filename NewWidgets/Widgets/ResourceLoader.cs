using System.Collections.Generic;

namespace NewWidgets.Widgets
{
    public class ResourceLoader
    {
        private static ResourceLoader s_instance;

        public static ResourceLoader Instance
        {
            get
            {
                return s_instance;
            }
        }


        private readonly string m_lang;
        private readonly Dictionary<string, string> m_strings;
        
        public string Language
        {
            get { return m_lang; }
        }

        public ResourceLoader(string lang)
        {
            m_strings = new Dictionary<string, string>();
            m_lang = lang;
            s_instance = this;
        }

        public string GetString(string str, params object[] parameters)
        {
            return string.Format(GetString(str), parameters);
        }

        public string GetString(string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            if (str[0] == '@')
                str = str.Substring(1);

            if (m_strings != null)
            {
                string result;
                if (m_strings.TryGetValue(str, out result))
                    return result;
            }

            switch (m_lang)
            {
            case "en-us":
            case "en":
            default:
                switch (str)
                {
                case "Locale":
                    return "en-us";
                }
                break;
            case "ru-ru":
            case "ru":
                switch (str)
                {
                case "Locale":
                    return "ru-ru";
                }
                break;
            case "fr-fr":
            case "fr":
                switch (str)
                {
                case "Locale":
                    return "fr-fr";
                }
                break;
            }

            return str;
        }

        public void RegisterString(string key, string value)
        {
            m_strings[key] = value;
        }
    }
}