using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TcpServer.Common.Helpers
{
    /// <summary>
    /// File helper - centralizes directory creation, read / write and safe persistence.
    /// Convention: for IO operations, check first whether the folder exists and create it when missing;
    ///             unmanaged objects must be released.
    /// </summary>
    public static class FileHelper
    {
        /// <summary>
        /// Ensures the directory exists (creates it when missing).
        /// </summary>
        /// <param name="directoryPath">Absolute directory path.</param>
        /// <returns>true when the directory is usable.</returns>
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
        /// Reads a text file (returns an empty string when the file is missing; never throws).
        /// </summary>
        /// <param name="filePath">Absolute file path.</param>
        /// <returns>File content; an empty string when missing or when reading fails.</returns>
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
        /// Safely writes a text file - writes a temporary file first and then replaces the target,
        /// so an interrupted write cannot corrupt the configuration file.
        /// </summary>
        /// <param name="filePath">Absolute target file path.</param>
        /// <param name="content">Content to write.</param>
        /// <returns>true when the write succeeded.</returns>
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
        /// Safely deletes a file (silently returns when the file does not exist).
        /// </summary>
        /// <param name="filePath">Absolute file path.</param>
        /// <returns>true when the delete succeeded or the file never existed.</returns>
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
        /// Backs up a file - checks the source file and target path before backing up
        /// (convention safety red-line item 9).
        /// </summary>
        /// <param name="sourcePath">Absolute source file path.</param>
        /// <returns>Absolute backup file path; an empty string when no backup is needed or when it fails.</returns>
        public static string BackupFile(string sourcePath)
        {
            return BackupFile(sourcePath, ConfigHelper.ConfigBackupKeepCount);
        }

        /// <summary>
        /// Backs up a file with a retention cap (added 2026-09-14).
        /// Note: the original implementation named backups by timestamp and never cleaned them up, leaving
        ///       one .bak file per configuration save. Over a long uptime this accumulates tens of thousands
        ///       of small files in the Config directory, so a retention cap was added.
        /// </summary>
        /// <param name="sourcePath">Absolute source file path.</param>
        /// <param name="keepCount">Number of backups to keep for the same name prefix; values below 1 use the default.</param>
        /// <returns>Absolute backup file path; an empty string when no backup is needed or when it fails.</returns>
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

                // Duplicate backups within the same second in an extreme case: append an index to avoid overwriting.
                int index = 1;
                while (File.Exists(backupPath))
                {
                    backupPath = Path.Combine(directory ?? string.Empty,
                        string.Format("{0}_{1}_{2}{3}.bak", fileName, stamp, index, extension));
                    index++;
                }

                File.Copy(sourcePath, backupPath, false);
                LogHelper.Instance.Info(string.Format("File backed up: {0} -> {1}", sourcePath, backupPath));

                // Clean up excess historical backups (a failure does not affect this backup result).
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
        /// Cleans up excess historical backup files, keeping only the most recent N (added 2026-09-14).
        /// </summary>
        /// <param name="directory">Directory containing the backup files.</param>
        /// <param name="fileBaseName">Source file name (without extension).</param>
        /// <param name="extension">Source file extension.</param>
        /// <param name="keepCount">Number of backups to keep.</param>
        /// <returns>Number of files actually deleted.</returns>
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

                // Sort by write time descending (by file name descending when times are equal) so that
                // the most recent keepCount files are the ones retained.
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
                        // A single failed backup deletion does not affect the remaining cleanup.
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
