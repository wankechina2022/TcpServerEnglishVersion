using System;
using System.Collections.Generic;
using TcpServer.Common;
using TcpServer.Common.Helpers;
using TcpServer.DAL;
using TcpServer.Model;

namespace TcpServer.BLL
{
    /// <summary>
    /// Configuration business logic - the only channel between the UI and the DAL; responsible for
    /// loading and saving the configuration and deriving the port range.
    /// </summary>
    public class ConfigBLL
    {
        #region Fields

        private readonly IConfigRepository _repository;

        #endregion

        #region Properties

        /// <summary>
        /// Absolute path of the configuration file.
        /// </summary>
        public string ConfigFilePath
        {
            get { return _repository == null ? string.Empty : _repository.ConfigFilePath; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor - uses the JSON file repository.
        /// </summary>
        public ConfigBLL()
            : this(new JsonConfigRepository())
        {
        }

        /// <summary>
        /// Constructor taking an explicit repository (eases a later switch of storage medium and unit testing).
        /// </summary>
        /// <param name="repository">Configuration repository implementation; uses the default JSON repository when null.</param>
        public ConfigBLL(IConfigRepository repository)
        {
            _repository = repository ?? new JsonConfigRepository();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Loads the configuration - returns an empty configuration with default values on failure;
        /// null is never returned.
        /// </summary>
        /// <returns>Configuration entity.</returns>
        public AppConfigModel LoadConfig()
        {
            try
            {
                AppConfigModel config = _repository.Load();
                if (config == null)
                {
                    return CreateEmptyConfig();
                }

                return config;
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Error("Failed to load config:" + ex.Message, ex);
                return CreateEmptyConfig();
            }
        }

        /// <summary>
        /// Saves the configuration.
        /// </summary>
        /// <param name="config">Configuration to save.</param>
        /// <returns>true when the save succeeded.</returns>
        public bool SaveConfig(AppConfigModel config)
        {
            if (config == null)
            {
                LogHelper.Instance.Warn("Failed to save config: the config object is null.");
                return false;
            }

            try
            {
                return _repository.Save(config);
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Error("Failed to save config:" + ex.Message, ex);
                return false;
            }
        }

        /// <summary>
        /// Backs up the current configuration file.
        /// </summary>
        /// <returns>Backup file path; an empty string on failure.</returns>
        public string BackupConfig()
        {
            try
            {
                return _repository.Backup();
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Error("Failed to back up config:" + ex.Message, ex);
                return string.Empty;
            }
        }

        /// <summary>
        /// Derives the port list from "start port + count", preserving the remark and enabled state
        /// of ports that already existed.
        /// </summary>
        /// <param name="startPort">Start port.</param>
        /// <param name="count">Number of ports.</param>
        /// <param name="existingPorts">Existing port list used to inherit remarks; may be null.</param>
        /// <param name="errorMessage">Failure reason.</param>
        /// <returns>Port list, never null (an empty collection on failure).</returns>
        public List<PortConfig> BuildPortList(int startPort, int count,
            List<PortConfig> existingPorts, out string errorMessage)
        {
            errorMessage = string.Empty;
            List<PortConfig> result = new List<PortConfig>();

            if (!ValidationHelper.IsValidPort(startPort))
            {
                errorMessage = string.Format("Start port must be between {0} and {1}",
                    AppConstants.MIN_PORT, AppConstants.MAX_PORT);
                return result;
            }

            if (!ValidationHelper.IsValidPortCount(count))
            {
                errorMessage = string.Format("Port count must be between {0} and {1}",
                    AppConstants.MIN_PORT_COUNT, AppConstants.MAX_PORT_COUNT);
                return result;
            }

            int endPort = startPort + count - 1;
            if (endPort > AppConstants.MAX_PORT)
            {
                errorMessage = string.Format("Port range out of bounds: {0} ~ {1}. Reduce the count or the start port",
                    startPort, endPort);
                return result;
            }

            // Build an index of existing remarks so they can be inherited.
            Dictionary<int, PortConfig> existingMap = new Dictionary<int, PortConfig>();
            if (existingPorts != null)
            {
                foreach (PortConfig old in existingPorts)
                {
                    if (old == null || !ValidationHelper.IsValidPort(old.Port))
                    {
                        continue;
                    }

                    if (!existingMap.ContainsKey(old.Port))
                    {
                        existingMap[old.Port] = old;
                    }
                }
            }

            for (int i = 0; i < count; i++)
            {
                int port = startPort + i;
                PortConfig cfg = new PortConfig(port);

                PortConfig old;
                if (existingMap.TryGetValue(port, out old) && old != null)
                {
                    cfg.Enabled = old.Enabled;
                    cfg.Remark = old.Remark ?? string.Empty;
                }

                result.Add(cfg);
            }

            return result;
        }

        /// <summary>
        /// Checks the port list for duplicates.
        /// </summary>
        /// <param name="ports">Port list.</param>
        /// <returns>List of duplicated ports, never null.</returns>
        public List<int> FindDuplicatedPorts(List<PortConfig> ports)
        {
            List<int> duplicated = new List<int>();
            if (ports == null || ports.Count == 0)
            {
                return duplicated;
            }

            HashSet<int> seen = new HashSet<int>();
            foreach (PortConfig cfg in ports)
            {
                if (cfg == null) { continue; }
                if (!seen.Add(cfg.Port) && !duplicated.Contains(cfg.Port))
                {
                    duplicated.Add(cfg.Port);
                }
            }

            return duplicated;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Creates an empty configuration.
        /// </summary>
        /// <returns>Configuration object with default values.</returns>
        private AppConfigModel CreateEmptyConfig()
        {
            AppConfigModel config = new AppConfigModel();
            config.ListenIp = ConfigHelper.DefaultListenIp;
            config.StartPort = ConfigHelper.DefaultStartPort;
            config.PortCount = 0;
            config.AutoStartOnLaunch = ConfigHelper.AutoStartOnLaunch;
            config.Ports = new List<PortConfig>();
            config.Rules = new List<AutoReplyRule>();
            return config;
        }

        #endregion
    }
}
