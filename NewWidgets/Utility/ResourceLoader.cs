﻿using System.Collections.Generic;

namespace NewWidgets.Utility
{
    /// <summary>
    /// Helper class for resource localization
    /// </summary>
    public sealed class ResourceLoader
    {
        private static readonly ResourceLoader s_instance = new ResourceLoader(string.Empty);

        public static ResourceLoader Instance
        {
            get { return s_instance; }
        }

        private readonly Dictionary<string, string> m_strings;
        private string m_language;
        
        public string Language
        {
            get { return m_language; }
            set { m_language = value; }
        }

        private ResourceLoader(string lang)
        {
            m_strings = new Dictionary<string, string>();
            m_language = lang;
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

            switch (m_language)
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