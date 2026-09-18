using System;
using System.Collections.Generic;
using System.IO;
using TcpServer.Common;
using TcpServer.Common.Helpers;
using TcpServer.Model;

namespace TcpServer.DAL
{
    /// <summary>
    /// JSON file configuration repository implementation - persists business configuration to
    /// Config\portconfig.json.
    /// </summary>
    public class JsonConfigRepository : IConfigRepository
    {
        #region Fields

        private readonly string _configDirectory;
        private readonly string _configFilePath;

        #endregion

        #region Properties

        /// <summary>
        /// Absolute path of the configuration file.
        /// </summary>
        public string ConfigFilePath
        {
            get { return _configFilePath; }
        }

        /// <summary>
        /// Directory containing the configuration file.
        /// </summary>
        public string ConfigDirectory
        {
            get { return _configDirectory; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor - the configuration file always lives in the Config subdirectory of the
        /// application directory.
        /// </summary>
        public JsonConfigRepository()
            : this(AppDomain.CurrentDomain.BaseDirectory)
        {
        }

        /// <summary>
        /// Constructor taking an explicit root directory.
        /// </summary>
        /// <param name="baseDirectory">Application root directory; uses the current working directory when empty.</param>
        public JsonConfigRepository(string baseDirectory)
        {
            string root = string.IsNullOrWhiteSpace(baseDirectory)
                ? AppDomain.CurrentDomain.BaseDirectory
                : baseDirectory;

            _configDirectory = Path.Combine(root, AppConstants.CONFIG_FOLDER_NAME);
            _configFilePath = Path.Combine(_configDirectory, AppConstants.CONFIG_FILE_NAME);

            // Convention: for IO operations, check first whether the folder exists and create it when missing.
            FileHelper.EnsureDirectory(_configDirectory);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Determines whether the configuration file already exists.
        /// </summary>
        /// <returns>true when it exists.</returns>
        public bool Exists()
        {
            try
            {
                return File.Exists(_configFilePath);
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Error("Failed to check whether the config file exists:" + ex.Message, ex);
                return false;
            }
        }

        /// <summary>
        /// Loads the configuration - when the file is missing, empty or fails to parse, an empty
        /// configuration with default values is returned; null is never returned.
        /// </summary>
        /// <returns>Configuration entity.</returns>
        public AppConfigModel Load()
        {
            AppConfigModel config = null;

            try
            {
                if (!Exists())
                {
                    LogHelper.Instance.Info("Config file not found, using default config:" + _configFilePath);
                    return CreateDefaultConfig();
                }

                string json = FileHelper.ReadAllTextSafe(_configFilePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    LogHelper.Instance.Warn("Config file is empty, using default config:" + _configFilePath);
                    return CreateDefaultConfig();
                }

                config = JsonHelper.Deserialize<AppConfigModel>(json);
                if (config == null)
                {
                    LogHelper.Instance.Warn("Failed to parse config file, using default config:" + _configFilePath);
                    return CreateDefaultConfig();
                }
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Error("Exception while reading the config file:" + ex.Message, ex);
                return CreateDefaultConfig();
            }

            return NormalizeConfig(config);
        }

        /// <summary>
        /// Saves the configuration - the previous file is backed up automatically before saving,
        /// and a failed write can be rolled back.
        /// </summary>
        /// <param name="config">Configuration entity to save.</param>
        /// <returns>true when the save succeeded.</returns>
        public bool Save(AppConfigModel config)
        {
            if (config == null)
            {
                LogHelper.Instance.Warn("Failed to save config: the config object is null.");
                return false;
            }

            try
            {
                if (!FileHelper.EnsureDirectory(_configDirectory))
                {
                    return false;
                }

                // Back up the existing file first so a corrupt write can still be rolled back
                // (convention safety red-line item 9).
                if (Exists())
                {
                    FileHelper.BackupFile(_configFilePath);
                }

                config.ConfigVersion = AppConstants.CONFIG_VERSION;
                config.LastSavedTime = DateTime.Now;

                string json = JsonHelper.Serialize(config);
                if (string.IsNullOrWhiteSpace(json))
                {
                    LogHelper.Instance.Error("Failed to save config: the serialization result is empty.");
                    return false;
                }

                bool success = FileHelper.WriteAllTextSafe(_configFilePath, json);
                if (success)
                {
                    LogHelper.Instance.Info(string.Format("Config saved: {0} ({1} port(s), {2} rule(s))",
                        _configFilePath,
                        config.Ports == null ? 0 : config.Ports.Count,
                        config.Rules == null ? 0 : config.Rules.Count));
                }

                return success;
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Error("Exception while saving the config file:" + ex.Message, ex);
                return false;
            }
        }

        /// <summary>
        /// Manually backs up the current configuration file.
        /// </summary>
        /// <returns>Absolute backup file path; an empty string when the file is missing or when it fails.</returns>
        public string Backup()
        {
            try
            {
                if (!Exists())
                {
                    return string.Empty;
                }

                return FileHelper.BackupFile(_configFilePath);
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Error("Exception while backing up the config file:" + ex.Message, ex);
                return string.Empty;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Creates an empty configuration with default values.
        /// </summary>
        /// <returns>Default configuration object.</returns>
        private AppConfigModel CreateDefaultConfig()
        {
            AppConfigModel config = new AppConfigModel();
            config.ListenIp = ConfigHelper.DefaultListenIp;
            config.StartPort = ConfigHelper.DefaultStartPort;
            config.PortCount = 0;
            config.AutoStartOnLaunch = ConfigHelper.AutoStartOnLaunch;
            config.DisplayAsHex = false;
            config.Ports = new List<PortConfig>();
            config.Rules = new List<AutoReplyRule>();
            return config;
        }

        /// <summary>
        /// Corrects illegal values in the configuration - prevents a hand-edited, broken JSON file from
        /// making the UI misbehave.
        /// </summary>
        /// <param name="config">Configuration object.</param>
        /// <returns>Corrected configuration object, never null.</returns>
        private AppConfigModel NormalizeConfig(AppConfigModel config)
        {
            if (config == null)
            {
                return CreateDefaultConfig();
            }

            if (string.IsNullOrWhiteSpace(config.ListenIp)
                || !ValidationHelper.IsValidIpAddress(config.ListenIp))
            {
                config.ListenIp = ConfigHelper.DefaultListenIp;
            }

            if (!ValidationHelper.IsValidPort(config.StartPort))
            {
                config.StartPort = ConfigHelper.DefaultStartPort;
            }

            if (config.PortCount < 0)
            {
                config.PortCount = 0;
            }

            if (config.Ports == null)
            {
                config.Ports = new List<PortConfig>();
            }

            if (config.Rules == null)
            {
                config.Rules = new List<AutoReplyRule>();
            }

            // Drop port entries that were broken by hand-editing the configuration file.
            List<PortConfig> validPorts = new List<PortConfig>();
            foreach (PortConfig item in config.Ports)
            {
                if (item == null)
                {
                    continue;
                }

                if (!ValidationHelper.IsValidPort(item.Port))
                {
                    LogHelper.Instance.Warn(string.Format("Invalid port entry ({0}) in the config file. Ignored.", item.Port));
                    continue;
                }

                if (item.Remark == null)
                {
                    item.Remark = string.Empty;
                }

                validPorts.Add(item);
            }
            config.Ports = validPorts;

            // Drop illegal auto-reply rules.
            List<AutoReplyRule> validRules = new List<AutoReplyRule>();
            foreach (AutoReplyRule rule in config.Rules)
            {
                if (rule == null)
                {
                    continue;
                }

                if (rule.RuleName == null) { rule.RuleName = string.Empty; }
                if (rule.MatchText == null) { rule.MatchText = string.Empty; }
                if (rule.ReplyText == null) { rule.ReplyText = string.Empty; }
                if (rule.DelayMs < 0) { rule.DelayMs = 0; }

                validRules.Add(rule);
            }
            config.Rules = validRules;

            return config;
        }

        #endregion
    }
}
