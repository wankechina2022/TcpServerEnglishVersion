using System;
using System.Collections.Generic;
using TcpServer.Common;
using TcpServer.Common.Helpers;
using TcpServer.DAL;
using TcpServer.Model;

namespace TcpServer.BLL
{
    /// <summary>
    /// 配置业务逻辑 —— UI 与 DAL 之间的唯一通道，负责配置的加载、保存与端口区间推导
    /// </summary>
    public class ConfigBLL
    {
        #region 字段

        private readonly IConfigRepository _repository;

        #endregion

        #region 属性

        /// <summary>
        /// 配置文件绝对路径
        /// </summary>
        public string ConfigFilePath
        {
            get { return _repository == null ? string.Empty : _repository.ConfigFilePath; }
        }

        #endregion

        #region 构造函数

        /// <summary>
        /// 默认构造函数 —— 使用 JSON 文件仓储
        /// </summary>
        public ConfigBLL()
            : this(new JsonConfigRepository())
        {
        }

        /// <summary>
        /// 指定仓储的构造函数（便于后续切换存储介质与单元测试）
        /// </summary>
        /// <param name="repository">配置仓储实现，为 null 时使用默认 JSON 仓储</param>
        public ConfigBLL(IConfigRepository repository)
        {
            _repository = repository ?? new JsonConfigRepository();
        }

        #endregion

        #region 对外方法

        /// <summary>
        /// 读取配置 —— 失败时返回带默认值的空配置，绝不返回 null
        /// </summary>
        /// <returns>配置实体</returns>
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
                LogHelper.Instance.Error("加载配置失败：" + ex.Message, ex);
                return CreateEmptyConfig();
            }
        }

        /// <summary>
        /// 保存配置
        /// </summary>
        /// <param name="config">待保存配置</param>
        /// <returns>保存成功返回 true</returns>
        public bool SaveConfig(AppConfigModel config)
        {
            if (config == null)
            {
                LogHelper.Instance.Warn("保存配置失败：配置对象为 null。");
                return false;
            }

            try
            {
                return _repository.Save(config);
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Error("保存配置失败：" + ex.Message, ex);
                return false;
            }
        }

        /// <summary>
        /// 备份当前配置文件
        /// </summary>
        /// <returns>备份文件路径；失败时返回空串</returns>
        public string BackupConfig()
        {
            try
            {
                return _repository.Backup();
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Error("备份配置失败：" + ex.Message, ex);
                return string.Empty;
            }
        }

        /// <summary>
        /// 按"起始端口 + 数量"推导端口清单，并保留已有端口的备注与启用状态
        /// </summary>
        /// <param name="startPort">起始端口</param>
        /// <param name="count">端口数量</param>
        /// <param name="existingPorts">已有端口清单，用于继承备注，可为 null</param>
        /// <param name="errorMessage">失败原因</param>
        /// <returns>端口清单，永不为 null（失败时返回空集合）</returns>
        public List<PortConfig> BuildPortList(int startPort, int count,
            List<PortConfig> existingPorts, out string errorMessage)
        {
            errorMessage = string.Empty;
            List<PortConfig> result = new List<PortConfig>();

            if (!ValidationHelper.IsValidPort(startPort))
            {
                errorMessage = string.Format("起始端口必须在 {0} ~ {1} 之间",
                    AppConstants.MIN_PORT, AppConstants.MAX_PORT);
                return result;
            }

            if (!ValidationHelper.IsValidPortCount(count))
            {
                errorMessage = string.Format("端口数量必须在 {0} ~ {1} 之间",
                    AppConstants.MIN_PORT_COUNT, AppConstants.MAX_PORT_COUNT);
                return result;
            }

            int endPort = startPort + count - 1;
            if (endPort > AppConstants.MAX_PORT)
            {
                errorMessage = string.Format("端口区间越界：{0} ~ {1}，请减小数量或起始端口",
                    startPort, endPort);
                return result;
            }

            // 建立已有备注索引，便于继承
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
        /// 检查端口清单中是否存在重复项
        /// </summary>
        /// <param name="ports">端口清单</param>
        /// <returns>重复的端口清单，永不为 null</returns>
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

        #region 私有方法

        /// <summary>
        /// 创建空配置
        /// </summary>
        /// <returns>带默认值的配置对象</returns>
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
