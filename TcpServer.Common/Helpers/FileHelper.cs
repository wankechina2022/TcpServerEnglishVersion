using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TcpServer.Common.Helpers
{
    /// <summary>
    /// 文件帮助类 —— 统一处理目录创建、读写与安全落盘
    /// 规约要求：IO 操作先判断文件夹存不存在，不存在自动创建；非托管对象必须释放
    /// </summary>
    public static class FileHelper
    {
        /// <summary>
        /// 确保目录存在（不存在则自动创建）
        /// </summary>
        /// <param name="directoryPath">目录绝对路径</param>
        /// <returns>目录可用返回 true</returns>
        public static bool EnsureDirectory(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                return false;
            }

            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Error(string.Format("Failed to create directory: {0}, reason: {1}", directoryPath, ex.Message), ex);
                return false;
            }
        }

        /// <summary>
        /// 读取文本文件（文件不存在时返回空串，不抛异常）
        /// </summary>
        /// <param name="filePath">文件绝对路径</param>
        /// <returns>文件内容；不存在或读取失败返回空串</returns>
        public static string ReadAllTextSafe(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return string.Empty;
            }

            try
            {
                return File.ReadAllText(filePath, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Error(string.Format("Failed to read file: {0}, reason: {1}", filePath, ex.Message), ex);
                return string.Empty;
            }
        }

        /// <summary>
        /// 安全写入文本文件 —— 先写临时文件再替换，避免写入过程中断导致配置文件损坏
        /// </summary>
        /// <param name="filePath">目标文件绝对路径</param>
        /// <param name="content">写入内容</param>
        /// <returns>写入成功返回 true</returns>
        public static bool WriteAllTextSafe(string filePath, string content)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return false;
            }

            string tempPath = filePath + ".tmp";

            try
            {
                string directory = Path.GetDirectoryName(filePath);
                if (!EnsureDirectory(directory))
                {
                    return false;
                }

                File.WriteAllText(tempPath, content ?? string.Empty, new UTF8Encoding(false));

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                File.Move(tempPath, filePath);
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Error(string.Format("Failed to write file: {0}, reason: {1}", filePath, ex.Message), ex);
                SafeDelete(tempPath);
                return false;
            }
        }

        /// <summary>
        /// 安全删除文件（文件不存在时静默返回）
        /// </summary>
        /// <param name="filePath">文件绝对路径</param>
        /// <returns>删除成功或文件本就不存在返回 true</returns>
        public static bool SafeDelete(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return false;
            }

            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Warn(string.Format("Failed to delete file: {0}, reason: {1}", filePath, ex.Message));
                return false;
            }
        }

        /// <summary>
        /// 备份文件 —— 备份前检查源文件与目标路径是否已存在（规约安全红线第 9 条）
        /// </summary>
        /// <param name="sourcePath">源文件绝对路径</param>
        /// <returns>备份文件绝对路径；无需备份或失败时返回空串</returns>
        public static string BackupFile(string sourcePath)
        {
            return BackupFile(sourcePath, ConfigHelper.ConfigBackupKeepCount);
        }

        /// <summary>
        /// 备份文件并限制保留份数（2026-09-14 新增）
        /// 说明：原实现按时间戳命名且从不清理，每次保存配置都留一份 .bak，
        ///       长期运行会在 Config 目录累积上万个小文件，故增加保留上限。
        /// </summary>
        /// <param name="sourcePath">源文件绝对路径</param>
        /// <param name="keepCount">同名前缀备份的保留份数，小于 1 时使用默认值</param>
        /// <returns>备份文件绝对路径；无需备份或失败时返回空串</returns>
        public static string BackupFile(string sourcePath, int keepCount)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            {
                return string.Empty;
            }

            try
            {
                string directory = Path.GetDirectoryName(sourcePath);
                string fileName = Path.GetFileNameWithoutExtension(sourcePath);
                string extension = Path.GetExtension(sourcePath);
                string stamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                string backupPath = Path.Combine(directory ?? string.Empty,
                    string.Format("{0}_{1}{2}.bak", fileName, stamp, extension));

                // 极端情况下同秒重复备份，追加序号避免覆盖
                int index = 1;
                while (File.Exists(backupPath))
                {
                    backupPath = Path.Combine(directory ?? string.Empty,
                        string.Format("{0}_{1}_{2}{3}.bak", fileName, stamp, index, extension));
                    index++;
                }

                File.Copy(sourcePath, backupPath, false);
                LogHelper.Instance.Info(string.Format("File backed up: {0} -> {1}", sourcePath, backupPath));

                // 清理超量历史备份（失败不影响本次备份结果）
                CleanupExpiredBackups(directory, fileName, extension,
                    keepCount < 1 ? ConfigHelper.ConfigBackupKeepCount : keepCount);

                return backupPath;
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Error("Failed to back up file:" + ex.Message, ex);
                return string.Empty;
            }
        }

        /// <summary>
        /// 清理超量的历史备份文件，仅保留最近 N 份（2026-09-14 新增）
        /// </summary>
        /// <param name="directory">备份文件所在目录</param>
        /// <param name="fileBaseName">源文件名（不含扩展名）</param>
        /// <param name="extension">源文件扩展名</param>
        /// <param name="keepCount">保留份数</param>
        /// <returns>实际删除的文件数量</returns>
        public static int CleanupExpiredBackups(string directory, string fileBaseName, string extension, int keepCount)
        {
            if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(fileBaseName))
            {
                return 0;
            }

            if (keepCount < 1)
            {
                return 0;
            }

            int deletedCount = 0;

            try
            {
                if (!Directory.Exists(directory))
                {
                    return 0;
                }

                string pattern = string.Format("{0}_*{1}.bak", fileBaseName, extension ?? string.Empty);
                string[] files = Directory.GetFiles(directory, pattern);

                if (files.Length <= keepCount)
                {
                    return 0;
                }

                List<FileInfo> infos = new List<FileInfo>();
                foreach (string file in files)
                {
                    try { infos.Add(new FileInfo(file)); }
                    catch (Exception) { }
                }

                // 按写入时间倒序（时间相同则按文件名倒序），保证保留的是最近的 keepCount 份
                infos.Sort(delegate(FileInfo left, FileInfo right)
                {
                    int result = right.LastWriteTime.CompareTo(left.LastWriteTime);
                    return result != 0 ? result : string.CompareOrdinal(right.Name, left.Name);
                });

                for (int i = keepCount; i < infos.Count; i++)
                {
                    try
                    {
                        infos[i].Delete();
                        deletedCount++;
                    }
                    catch (Exception)
                    {
                        // 单个备份删除失败不影响其余清理
                    }
                }

                if (deletedCount > 0)
                {
                    LogHelper.Instance.Info(string.Format(
                        "Cleaned up {0} excess config backup(s), keeping the latest {1}.", deletedCount, keepCount));
                }
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Warn("Exception while cleaning up excess backup files:" + ex.Message);
            }

            return deletedCount;
        }
    }
}
