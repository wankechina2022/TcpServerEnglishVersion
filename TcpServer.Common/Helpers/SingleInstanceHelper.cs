using System;
using System.Threading;

namespace TcpServer.Common.Helpers
{
    /// <summary>
    /// Single-instance control helper - relies on a system mutex to ensure the application starts only once.
    /// Convention: a windowed application may only be started once; a second launch is not allowed.
    /// </summary>
    public sealed class SingleInstanceHelper : IDisposable
    {
        #region Fields

        private Mutex _mutex;
        private bool _ownsMutex;
        private bool _disposed;

        #endregion

        #region Properties

        /// <summary>
        /// Whether this is the first instance (true means the application may start normally).
        /// </summary>
        public bool IsFirstInstance { get; private set; }

        #endregion

        #region Constructor and Dispose

        /// <summary>
        /// Constructor - attempts to acquire the global mutex.
        /// </summary>
        /// <param name="mutexName">Mutex name; defaults to the global constant.</param>
        public SingleInstanceHelper(string mutexName)
        {
            IsFirstInstance = false;
            _ownsMutex = false;

            string name = string.IsNullOrWhiteSpace(mutexName) ? AppConstants.MUTEX_NAME : mutexName;

            try
            {
                bool createdNew;
                _mutex = new Mutex(true, name, out createdNew);
                _ownsMutex = createdNew;
                IsFirstInstance = createdNew;

                if (!createdNew)
                {
                    // An instance is already running; release this handle to avoid a resource leak.
                    ReleaseMutexInternal();
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                // Do not block startup on a permission exception; degrade to allowing the run.
                LogHelper.Instance.Warn("Single-instance mutex creation was denied, running in degraded mode:" + ex.Message);
                IsFirstInstance = true;
                _mutex = null;
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Error("Failed to create the single-instance mutex, running in degraded mode:" + ex.Message, ex);
                IsFirstInstance = true;
                _mutex = null;
            }
        }

        /// <summary>
        /// Default constructor - uses the global mutex name.
        /// </summary>
        public SingleInstanceHelper()
            : this(AppConstants.MUTEX_NAME)
        {
        }

        /// <summary>
        /// Releases the mutex resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            ReleaseMutexInternal();
            _disposed = true;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Releases the mutex (called internally; guarantees a single release and never throws).
        /// </summary>
        private void ReleaseMutexInternal()
        {
            if (_mutex == null)
            {
                return;
            }

            try
            {
                if (_ownsMutex)
                {
                    _mutex.ReleaseMutex();
                    _ownsMutex = false;
                }
            }
            catch (Exception)
            {
                // A failed release does not affect the shutdown flow.
            }
            finally
            {
                try
                {
                    _mutex.Close();
                }
                catch (Exception)
                {
                }

                _mutex = null;
            }
        }

        #endregion
    }
}
