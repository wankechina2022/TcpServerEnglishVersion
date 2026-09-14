using TcpServer.Model;

namespace TcpServer.DAL
{
    /// <summary>
    /// 业务配置仓储接口 —— 定义端口清单与应答规则的持久化契约
    /// 后续如改用 SQLite / 数据库，只需替换实现类，BLL 与 UI 无需改动
    /// </summary>
    public interface IConfigRepository
    {
        /// <summary>
        /// 配置文件绝对路径
        /// </summary>
        string ConfigFilePath { get; }

        /// <summary>
        /// 判断配置文件是否已存在
        /// </summary>
        /// <returns>存在返回 true</returns>
        bool Exists();

        /// <summary>
        /// 读取配置
        /// </summary>
        /// <returns>配置实体；文件不存在或损坏时返回带默认值的空配置（永不为 null）</returns>
        AppConfigModel Load();

        /// <summary>
        /// 保存配置（保存前自动备份上一版文件）
        /// </summary>
        /// <param name="config">待保存的配置实体</param>
        /// <returns>保存成功返回 true</returns>
        bool Save(AppConfigModel config);

        /// <summary>
        /// 手动备份当前配置文件
        /// </summary>
        /// <returns>备份文件路径；无需备份或失败时返回空串</returns>
        string Backup();
    }
}
