using System;
using System.Collections.Generic;
using System.IO;
using TcpServer.Common;
using TcpServer.Common.Helpers;
using TcpServer.Model;

namespace TcpServer.DAL
{
    /// <summary>
    /// JSON 文件配置仓储实现 —— 业务配置持久化到 Config\portconfig.json
    /// </summary>
    public class JsonConfigRepository : IConfigRepository
    {
        #region 字段

        private readonly string _configDirectory;
        private readonly string _configFilePath;

        #endregion

        #region 属性

        /// <summary>
        /// 配置文件绝对路径
        /// </summary>
        public string ConfigFilePath
        {
            get { return _configFilePath; }
        }

        /// <summary>
        /// 配置文件所在目录
        /// </summary>
        public string ConfigDirectory
        {
            get { return _configDirectory; }
        }

        #endregion

        #region 构造函数

        /// <summary>
        /// 默认构造函数 —— 配置文件固定放在程序运行目录的 Config 子目录下
        /// </summary>
        public JsonConfigRepository()
            : this(AppDomain.CurrentDomain.BaseDirectory)
        {
        }

        /// <summary>
        /// 指定根目录的构造函数
        /// </summary>
        /// <param name="baseDirectory">程序根目录，为空时使用当前运行目录</param>
        public JsonConfigRepository(string baseDirectory)
        {
            string root = string.IsNullOrWhiteSpace(baseDirectory)
                ? AppDomain.CurrentDomain.BaseDirectory
                : baseDirectory;

            _configDirectory = Path.Combine(root, AppConstants.CONFIG_FOLDER_NAME);
            _configFilePath = Path.Combine(_configDirectory, AppConstants.CONFIG_FILE_NAME);

            // 规约要求：IO 操作先判断文件夹存不存在，不存在自动创建
            FileHelper.EnsureDirectory(_configDirectory);
        }

        #endregion

        #region 对外方法

        /// <summary>
        /// 判断配置文件是否已存在
        /// </summary>
        /// <returns>存在返回 true</returns>
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
        /// 读取配置 —— 文件不存在、为空或解析失败时均返回带默认值的空配置，绝不返回 null
        /// </summary>
        /// <returns>配置实体</returns>
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
        /// 保存配置 —— 保存前自动备份上一版文件，写入失败可回滚
        /// </summary>
        /// <param name="config">待保存的配置实体</param>
        /// <returns>保存成功返回 true</returns>
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

                // 已有旧文件时先备份，避免写坏后无法回退（规约安全红线第 9 条）
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
        /// 手动备份当前配置文件
        /// </summary>
        /// <returns>备份文件绝对路径；文件不存在或失败时返回空串</returns>
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

        #region 私有方法

        /// <summary>
        /// 创建带默认值的空配置
        /// </summary>
        /// <returns>默认配置对象</returns>
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
        /// 修正配置中的非法值 —— 防止手工改坏 json 导致界面异常
        /// </summary>
        /// <param name="config">配置对象</param>
        /// <returns>修正后的配置对象，永不为 null</returns>
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

            // 剔除配置文件里被手工改坏的非法端口项
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

            // 剔除非法应答规则
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
