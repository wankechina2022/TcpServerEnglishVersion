using TcpServer.Model;

namespace TcpServer.DAL
{
    /// <summary>
    /// Business configuration repository interface - defines the persistence contract for the port list
    /// and the auto-reply rules.
    /// If persistence later moves to SQLite / a database, only the implementation class needs replacing;
    /// BLL and UI remain unchanged.
    /// </summary>
    public interface IConfigRepository
    {
        /// <summary>
        /// Absolute path of the configuration file.
        /// </summary>
        string ConfigFilePath { get; }

        /// <summary>
        /// Determines whether the configuration file already exists.
        /// </summary>
        /// <returns>true when it exists.</returns>
        bool Exists();

        /// <summary>
        /// Loads the configuration.
        /// </summary>
        /// <returns>Configuration entity; when the file is missing or corrupt, an empty configuration with
        ///          default values is returned (never null).</returns>
        AppConfigModel Load();

        /// <summary>
        /// Saves the configuration (the previous file is backed up automatically before saving).
        /// </summary>
        /// <param name="config">Configuration entity to save.</param>
        /// <returns>true when the save succeeded.</returns>
        bool Save(AppConfigModel config);

        /// <summary>
        /// Manually backs up the current configuration file.
        /// </summary>
        /// <returns>Backup file path; an empty string when no backup is needed or when it fails.</returns>
        string Backup();
    }
}
