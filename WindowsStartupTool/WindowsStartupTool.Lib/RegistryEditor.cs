﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;

namespace WindowsStartupTool.Lib
{
    /// <summary>
    /// Author: Narek Yegoryan
    /// </summary>
    public class RegistryEditor : IDisposable
    {
        private readonly string _machine;
        private readonly bool _startServiceIfNeeded;
        private readonly RegistryLookupSourceEnum _source;
        private RegistryKey _registry;
        const string StartupSubKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
        const string RemoteRegistryService = "Remote Registry";

        public List<string> DefaultStartupApps => new List<string> { "igfxtray", "hotkeyscmds", "persistence", "AvastUI.exe" };

        public RegistryEditor(string machine, RegistryLookupSourceEnum source = RegistryLookupSourceEnum.Machine, bool startServiceIfNeeded = false)
        {
            _source = source;
            _startServiceIfNeeded = startServiceIfNeeded;
            _machine = machine;
            if (string.IsNullOrEmpty(machine))
                _machine = Environment.MachineName;
        }

        public Dictionary<string, string> GetAllStartupAppsFromRegistry(SkippableSourceEnum skipSource = SkippableSourceEnum.None)
        {
            var result = GetStartupAppsFromRegistry(TargetPlatformEnum.x32);
            var result64 = GetStartupAppsFromRegistry(TargetPlatformEnum.x64);

            foreach (var item64 in result64)
            {
                if (!result.ContainsKey(item64.Key))
                    result.Add(item64.Key, item64.Value);
            }

            switch (skipSource)
            {
                case SkippableSourceEnum.None:
                    return result;
                case SkippableSourceEnum.Default:
                    return result.Where(x => !DefaultStartupApps.Any(z => z == x.Key.ToLower())).ToDictionary(x => x.Key, y => y.Value);
                case SkippableSourceEnum.File:
                    var fileManager = new FileManager();
                    var skipApps = fileManager.GetFileContent();
                    return result.Where(x => !skipApps.Any(z => z.ToLower() == x.Key.ToLower())).ToDictionary(x => x.Key, y => y.Value);
                case SkippableSourceEnum.DefaultWithFile:
                    var appsToSkip = new List<string>(DefaultStartupApps);
                    var fManager = new FileManager();
                    var skipAppsFromFile = fManager.GetFileContent();

                    foreach (var item in skipAppsFromFile)
                        if (!appsToSkip.Any(x => x.ToLower() == item.ToLower()))
                            appsToSkip.Add(item);

                    return result.Where(x => !appsToSkip.Any(z => z.ToLower() == x.Key.ToLower())).ToDictionary(x => x.Key, y => y.Value);
            }

            return result;
        }

        /// <summary>
        /// Returns the list of startup programs on remote machine, with executable file path's
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string> GetStartupAppsFromRegistry(TargetPlatformEnum platform = TargetPlatformEnum.x32)
        {
            InitializeRegistry(StartupSubKey, platform);

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (_registry != null)
            {
                foreach (var app in _registry.GetValueNames().AsEnumerable())
                {
                    if (!result.ContainsKey(app))
                    {
                        var value = _registry.GetValue(app);
                        result.Add(app, value.ToString());
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// If application with the same key is exists function will replace the value
        /// </summary>
        /// <param name="source"></param>
        /// <param name="key"></param>
        /// <param name="executeablePath"></param>
        /// <param name="machine"></param>
        public void SetStartupAppToRegistry(string key, string executeablePath)
        {
            SetRegistry(StartupSubKey, key, executeablePath);
        }

        /// <summary>
        /// Initializes RegistryEditor to passed in registry key value
        /// </summary>
        /// <param name="subKey"></param>
        public void InitializeRegistry(string subKey = "", TargetPlatformEnum targetPlatform = TargetPlatformEnum.x32)
        {
            RegistryView platform = targetPlatform == TargetPlatformEnum.x32 ? RegistryView.Registry32 : RegistryView.Registry64;

            if (string.IsNullOrWhiteSpace(_machine) || _machine == Environment.MachineName)
            {
                switch (_source)
                {
                    case RegistryLookupSourceEnum.Machine:
                        _registry = Registry.LocalMachine.OpenSubKey(subKey, RegistryKeyPermissionCheck.ReadWriteSubTree, System.Security.AccessControl.RegistryRights.FullControl);
                        break;
                    case RegistryLookupSourceEnum.User:
                        _registry = Registry.CurrentUser.OpenSubKey(subKey, RegistryKeyPermissionCheck.ReadWriteSubTree, System.Security.AccessControl.RegistryRights.FullControl);
                        break;
                }
            }
            else
            {
                try
                {
                    if (_startServiceIfNeeded)
                    {
                        ServiceController serviceController = new ServiceController(RemoteRegistryService, _machine);
                        if (serviceController.Status != ServiceControllerStatus.Running)
                            serviceController.Start();
                        serviceController.Dispose();
                    }
                    switch (_source)
                    {
                        case RegistryLookupSourceEnum.Machine:
                            _registry = RegistryKey
                                .OpenRemoteBaseKey(RegistryHive.LocalMachine, _machine, platform)
                                .OpenSubKey(subKey, RegistryKeyPermissionCheck.ReadWriteSubTree);
                            break;
                        default:
                            _registry = RegistryKey
                                .OpenRemoteBaseKey(RegistryHive.CurrentUser, _machine, platform)
                                .OpenSubKey(subKey, RegistryKeyPermissionCheck.ReadWriteSubTree);
                            break;
                    }
                }
                catch (System.IO.IOException)
                {
                    var exceptionMessage = $"On < {_machine} >, < {RemoteRegistryService} > service is not running.";
                    throw new Exception(exceptionMessage);
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
        }

        /// <summary>
        /// Sets the registry with specified sub key
        /// </summary>
        /// <param name="subKey"></param>
        /// <param name="registryKey"></param>
        /// <param name="registryValue"></param>
        public void SetRegistry(string subKey, string registryKey, string registryValue)
        {
            InitializeRegistry(subKey);

            if (_registry != null)
            {
                _registry.SetValue(registryKey, registryValue, RegistryValueKind.String);
            }
        }

        /// <summary>
        /// Removes an app with spacified key from startup
        /// </summary>
        /// <param name="key"></param>
        public void RemoveStartupAppByKey(string key)
        {
            RemoveRegistryValueByKey(StartupSubKey, key);
        }

        /// <summary>
        /// Removes specified key value pair from registry sub key
        /// </summary>
        /// <param name="subKey"></param>
        /// <param name="registryKey"></param>
        public void RemoveRegistryValueByKey(string subKey, string registryKey)
        {
            try
            {
                InitializeRegistry(subKey);
                if (_registry != null)
                    _registry.DeleteValue(registryKey, true);
            }
            catch (ArgumentException)
            {
                InitializeRegistry(subKey, TargetPlatformEnum.x64);
                if (_registry != null)
                    _registry.DeleteValue(registryKey, true);
            }
        }

        /// <summary>
        /// IDisposable support
        /// </summary>
        public void Dispose()
        {
            if (_registry != null)
                _registry.Close();
            GC.SuppressFinalize(this);
        }
    }
}
