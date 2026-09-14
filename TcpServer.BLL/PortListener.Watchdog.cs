using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using TcpServer.Common;
using TcpServer.Common.Helpers;
using TcpServer.Model;

namespace TcpServer.BLL
{
    /// <summary>
    /// 端口监听服务（看门狗与线程收尾部分）
    /// 2026-09-14 拆分：PortListener 主体加入看门狗后超过规约"单类不超 800 行"的红线，
    /// 按项目既有惯例（参见 frmMain.cs / frmMain.Console.cs）拆到本 partial 文件。
    /// 端口启停、客户端接入与收发相关成员见 PortListener.cs
    /// </summary>
    public partial class PortListener
    {
        #region 私有方法 —— 线程收尾

        /// <summary>
        /// 等待线程退出（2026-09-14 新增，统一异常与超时处理）
        /// </summary>
        /// <param name="thread">目标线程，可为 null</param>
        /// <param name="timeoutMs">最长等待毫秒数</param>
        private void JoinThread(Thread thread, int timeoutMs)
        {
            if (thread == null) { return; }

            try
            {
                if (thread.IsAlive)
                {
                    thread.Join(timeoutMs);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Warn(string.Format("端口 {0} 等待线程 {1} 退出异常：{2}",
                    Port, thread.Name, ex.Message));
            }
        }

        /// <summary>
        /// 唤醒看门狗线程（2026-09-14 新增）
        /// 用途：Stop 时无需等满巡检间隔（默认 5 秒），立即让看门狗退出，
        ///       把"每停止一个端口白等 1 秒"降到毫秒级。
        /// 说明：异常一律吞掉——即使信号对象已被释放，停止流程也必须继续走完，
        ///       否则端口会停不干净。
        /// </summary>
        private void WakeupWatchdog()
        {
            try
            {
                _watchdogWakeup.Set();
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Warn(string.Format("端口 {0} 唤醒看门狗异常：{1}", Port, ex.Message));
            }
        }

        #endregion

        #region 私有方法 —— 监听看门狗

        /// <summary>
        /// 启动监听线程看门狗（2026-09-14 新增）
        /// 用途：Accept 循环虽已改为遇异常继续，但若线程因其它未预期原因（如线程被强制终止）
        ///       退出，界面仍会显示"监听中"而实际再也接不进连接，只能重启程序。
        ///       看门狗定期巡检，发现线程已死即自动重建监听，把"必须重启程序"变成自愈。
        /// 说明：每次启动都递增代数号并传入巡检循环，旧线程醒来后据此判断自己已过期并退出，
        ///       避免"停止后立刻再启动"时出现两条看门狗并存。
        /// </summary>
        private void StartWatchdog()
        {
            int intervalMs = ConfigHelper.WatchdogIntervalMs;

            // 配置为 0 表示关闭看门狗
            if (intervalMs <= 0)
            {
                return;
            }

            try
            {
                int generation = Interlocked.Increment(ref _watchdogGeneration);

                // 2026-09-14 新增：先清掉上一次停止时留下的唤醒信号，
                // 否则新看门狗线程一启动就会立刻被"唤醒"而直接退出
                _watchdogWakeup.Reset();

                _watchdogRunning = true;

                _watchdogThread = new Thread(delegate() { WatchdogLoop(generation); });
                _watchdogThread.IsBackground = true;
                _watchdogThread.Name = string.Format("TcpWatchdog_{0}", Port);
                _watchdogThread.Start();
            }
            catch (Exception ex)
            {
                _watchdogRunning = false;
                LogHelper.Instance.Warn(string.Format("端口 {0} 启动看门狗失败，不影响监听：{1}",
                    Port, ex.Message));
            }
        }

        /// <summary>
        /// 看门狗巡检循环（2026-09-14 新增）
        /// </summary>
        /// <param name="generation">本线程的代数号，与当前代数不一致即表示已被替换，需退出</param>
        private void WatchdogLoop(int generation)
        {
            int intervalMs = ConfigHelper.WatchdogIntervalMs;
            if (intervalMs <= 0) { intervalMs = AppConstants.WATCHDOG_INTERVAL_MS; }

            while (_watchdogRunning && generation == Volatile.Read(ref _watchdogGeneration))
            {
                bool wokenUp = false;

                // 2026-09-14 修改：原为 Thread.Sleep(intervalMs)，Stop 时 Join 只能等满超时返回，
                // 实测每个端口白等约 1 秒。改为可唤醒等待：Stop 里 Set 后立即返回 true。
                try
                {
                    wokenUp = _watchdogWakeup.Wait(intervalMs);
                }
                catch (Exception)
                {
                }

                // 收到停止唤醒信号，或代数已变（被停止 / 重启过）—— 本线程作废，直接退出
                if (wokenUp || generation != Volatile.Read(ref _watchdogGeneration))
                {
                    break;
                }

                if (!_watchdogRunning || !_running || _disposed)
                {
                    continue;
                }

                // 监听中但 Accept 线程已不在 —— 说明监听已失效，需要重建
                Thread acceptThread = _acceptThread;
                if (acceptThread != null && acceptThread.IsAlive)
                {
                    continue;
                }

                LogHelper.Instance.Warn(string.Format(
                    "端口 {0} 看门狗检测到监听线程已退出，正在自动恢复监听。", Port));

                RestartListener(generation);

                // 重建后本线程继续巡检即可；若期间被停止/重启，下一轮代数校验会退出
            }
        }

        /// <summary>
        /// 重建监听（看门狗调用，2026-09-14 新增）
        /// </summary>
        /// <param name="generation">调用方代数号，与当前代数不一致则放弃重建</param>
        private void RestartListener(int generation)
        {
            try
            {
                // 先释放旧监听器，再重新绑定，避免"已被自己占用"的假故障
                CleanupListener();

                // 停止/重启动作会改变代数号；此处再校验一次，
                // 避免在"已停止"或"已由新看门狗接管"的情况下重复创建监听
                if (generation != Volatile.Read(ref _watchdogGeneration)
                    || !_watchdogRunning || !_running || _disposed)
                {
                    return;
                }

                IPAddress address = IPAddress.Parse(_listenIp);
                _listener = new TcpListener(address, Port);
                _listener.Start();

                // 绑定成功后再校验一次：若这期间被停止，立刻回收刚创建的句柄
                if (generation != Volatile.Read(ref _watchdogGeneration)
                    || !_watchdogRunning || !_running || _disposed)
                {
                    CleanupListener();
                    return;
                }

                _acceptThread = new Thread(AcceptLoop);
                _acceptThread.IsBackground = true;
                _acceptThread.Name = string.Format("TcpAccept_{0}", Port);
                _acceptThread.Start();

                SetState(PortState.Listening, "监听已自动恢复");
                LogHelper.Instance.Info(string.Format("端口 {0} 监听已由看门狗自动恢复。", Port));
            }
            catch (Exception ex)
            {
                SetState(PortState.Faulted, "监听异常且自动恢复失败，请手动重启该端口");
                LogHelper.Instance.Error(string.Format("端口 {0} 自动恢复监听失败：{1}", Port, ex.Message), ex);
            }
        }

        #endregion
    }
}
