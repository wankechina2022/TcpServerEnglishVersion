using System;
using System.Threading;

namespace TcpServer.Common.Helpers
{
    /// <summary>
    /// 单实例控制帮助类 —— 借助系统互斥体保证程序只能启动一次
    /// 规约约定：窗体程序只能启动一次，不能二次启动
    /// </summary>
    public sealed class SingleInstanceHelper : IDisposable
    {
        #region 字段

        private Mutex _mutex;
        private bool _ownsMutex;
        private bool _disposed;

        #endregion

        #region 属性

        /// <summary>
        /// 是否为当前首个实例（true 表示可以正常启动）
        /// </summary>
        public bool IsFirstInstance { get; private set; }

        #endregion

        #region 构造与释放

        /// <summary>
        /// 构造函数 —— 尝试获取全局互斥体
        /// </summary>
        /// <param name="mutexName">互斥体名称，默认使用全局常量</param>
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
                    // 已有实例在运行，释放本次句柄避免资源泄漏
                    ReleaseMutexInternal();
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                // 权限异常时不阻断启动，降级为允许运行
                LogHelper.Instance.Warn("单实例互斥体创建被拒绝，已降级运行：" + ex.Message);
                IsFirstInstance = true;
                _mutex = null;
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Error("单实例互斥体创建失败，已降级运行：" + ex.Message, ex);
                IsFirstInstance = true;
                _mutex = null;
            }
        }

        /// <summary>
        /// 默认构造函数 —— 使用全局互斥体名称
        /// </summary>
        public SingleInstanceHelper()
            : this(AppConstants.MUTEX_NAME)
        {
        }

        /// <summary>
        /// 释放互斥体资源
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

        #region 私有方法

        /// <summary>
        /// 释放互斥体（内部调用，保证只释放一次且不抛异常）
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
                // 释放失败不影响退出流程
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
