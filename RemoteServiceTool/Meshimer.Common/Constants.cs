﻿using System;

namespace Meshimer.Common
{
    public class Constants
    {
        public static string LogFolderLocation { get => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments); }
        public const string LogFolderName = "Meshimer service logs";

        public const string ServiceName = "Meshimer.Scrapper";
        public const string ServiceDescription = "";
        public const string ServiceDisplayName = "Meshimer Scrapper Service";

        public const string wb_BlockDetails = "wb_BlockDetails";
        public const string UserNameFromMeshimerPage = "Username from meshimer page < {0} >";
    }

    public class Tags
    {
        public const string Browser = "-browser";
    }

    public class Browsers
    {
        public const string Chrome = "chrome";
        public const string Firefox = "firefox";
    }
}
