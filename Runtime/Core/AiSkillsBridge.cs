using UnityEngine;
using UnityEditor;
using UnityEditor.Scripting.Python;
using UnityEditor.Compilation; // 新增命名空间引用
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Debug = UnityEngine.Debug;

namespace Observater.AiSkills.Runtime.Core
{
    /// <summary>
    /// Unity 与外部 Python 进程通信的核心桥接类。
    /// <para>负责管理 Python 进程生命周期、Socket 服务器以及主线程回调分发。</para>
    /// </summary>
    [InitializeOnLoad]
    public static class AiSkillsBridge
    {
        // ================= 事件系统 =================
        
        /// <summary>
        /// 当产生状态日志时触发
        /// </summary>
        public static event Action<string> OnStatusLog;

        /// <summary>
        /// 线程安全的日志队列，用于将后台线程的日志转送给主线程
        /// </summary>
        private static readonly ConcurrentQueue<string> _logQueue = new ConcurrentQueue<string>();

        /// <summary>
        /// 内部日志记录方法，将消息入队
        /// </summary>
        private static void LogToUI(string msg) => _logQueue.Enqueue(msg);

        // ================= 常量配置 =================

        /// <summary>
        /// Unity 端监听的 Socket 端口（供 Python 回连）
        /// </summary>
        private const int UNITY_PORT = 8081;

        /// <summary>
        /// 内部协议指令：用于优雅关闭 Socket 监听循环
        /// </summary>
        private const string CMD_SHUTDOWN_LISTENER = "@@INTERNAL_STOP@@";

        /// <summary>
        /// 插件包名，用于路径搜索
        /// </summary>
        private const string PACKAGE_NAME = "com.observater.aiskills";
        
        /// <summary>
        /// Python 服务器脚本相对于包根目录的路径
        /// </summary>
        private const string RELATIVE_SCRIPT_PATH = "Runtime/Python/Core/ai_server.py";
        
        /// <summary>
        /// 内置 Python 解释器相对于包根目录的路径
        /// </summary>
        private const string RELATIVE_PYTHON_EXE_PATH = "Runtime/Python/python.exe";

        /// <summary>
        /// 配置文件在项目中的存储路径
        /// </summary>
        private static string ConfigPath => Path.Combine(Application.dataPath, "../ProjectSettings/AiSkillsConfig.json");

        /// <summary>
        /// 当前加载的配置实例
        /// </summary>
        public static AiSkillsConfig Config { get; private set; }

        // ================= 运行时状态 =================

        private static TcpListener _listener;
        private static Thread _serverThread;
        private static volatile bool _isRunning = false;
        private static NetworkStream _currentStream;
        private static Process _pythonProcess;

        /// <summary>
        /// 接收到的 Python 指令队列（代码字符串）
        /// </summary>
        private static readonly ConcurrentQueue<string> _commandQueue = new ConcurrentQueue<string>();

        /// <summary>
        /// 静态构造函数，初始化并挂载编辑器事件
        /// </summary>
        static AiSkillsBridge()
        {
            LoadConfig();
            EditorApplication.update += OnUpdate;
            
            // 编辑器退出时清理
            EditorApplication.quitting += () => RestartPython(true, false);

            AssemblyReloadEvents.beforeAssemblyReload += () => RestartPython(true, false);

            EditorApplication.delayCall += () => RestartPython(true);
        }

        // ================= 配置管理 =================

        /// <summary>
        /// 从磁盘加载配置，如果不存在则创建默认配置
        /// </summary>
        public static void LoadConfig()
        {
            if (File.Exists(ConfigPath))
            {
                try
                {
                    Config = JsonConvert.DeserializeObject<AiSkillsConfig>(File.ReadAllText(ConfigPath));
                }
                catch
                {
                    Config = new AiSkillsConfig();
                }
            }
            else
            {
                Config = new AiSkillsConfig();
                SaveConfig();
            }
        }

        /// <summary>
        /// 将当前配置保存到磁盘
        /// </summary>
        public static void SaveConfig()
        {
            try
            {
                File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(Config, Formatting.Indented));
            }
            catch (Exception e)
            {
                Debug.LogError($"[AiSkills] Save Config Failed: {e.Message}");
            }
        }

        // ================= 核心生命周期控制 =================

        /// <summary>
        /// 重启 Python 服务环境
        /// </summary>
        /// <param name="killAndRestart">是否先杀掉旧进程并停止服务</param>
        /// <param name="startNew">是否启动新服务</param>
        public static void RestartPython(bool killAndRestart = true, bool startNew = true)
        {
            if (killAndRestart)
            {
                LogToUI("[System] Stopping services...");
                KillPythonProcess();
                StopServer();
                TryKillOldListener();
            }

            if (startNew)
            {
                // 重新加载配置，确保最新的 ShowConsole 设置生效
                LoadConfig();
                LogToUI($"[System] Starting services (Port {Config.Port} <-> {UNITY_PORT})...");
                StartServer();
                CheckAndStartPython();
            }
        }

        /// <summary>
        /// 获取包的根目录路径（自动适配 UPM 引用或 Assets 源码模式）
        /// </summary>
        /// <returns>包的绝对路径，如果找不到返回 null</returns>
        private static string GetPackageRootPath()
        {
            // 1. 尝试 UPM 路径
            string packagePath = Path.GetFullPath($"Packages/{PACKAGE_NAME}");
            if (Directory.Exists(packagePath)) return packagePath;

            // 2. 尝试 Assets 开发路径
            string assetsPath = Path.GetFullPath($"Assets/{PACKAGE_NAME}");
            if (Directory.Exists(assetsPath)) return assetsPath;
            
            // 3. 暴力搜索核心文件 (防止文件夹改名)
            var guids = AssetDatabase.FindAssets("ai_server"); 
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                // 回退到包根目录: .../Runtime/Python/Core/ai_server.py -> ../../../..
                return Path.GetFullPath(path + "/../../../../");
            }

            return null;
        }

        /// <summary>
        /// 检查路径并启动 Python 进程
        /// </summary>
        private static void CheckAndStartPython()
        {
            string rootPath = GetPackageRootPath();
            if (string.IsNullOrEmpty(rootPath))
            {
                LogToUI($"[Error] Cannot find package directory for {PACKAGE_NAME}");
                return;
            }

            string scriptPath = Path.Combine(rootPath, RELATIVE_SCRIPT_PATH);
            string pythonExePath = Path.Combine(rootPath, RELATIVE_PYTHON_EXE_PATH);

            if (!File.Exists(scriptPath))
            {
                LogToUI($"[Error] Script not found at: {scriptPath}");
                return;
            }

            // 如果找不到内置 Python，回退到系统环境变量
            if (!File.Exists(pythonExePath))
            {
                LogToUI($"[Warn] Embedded Python not found. Trying system 'python'...");
                pythonExePath = "python"; 
            }

            StartPythonProcess(pythonExePath, scriptPath, Config.Port);
        }

        /// <summary>
        /// 启动 Python 子进程 (包含黑窗控制逻辑)
        /// </summary>
        /// <param name="pythonExe">Python解释器路径</param>
        /// <param name="scriptPath">脚本路径</param>
        /// <param name="port">端口参数</param>
        private static void StartPythonProcess(string pythonExe, string scriptPath, int port)
        {
            try
            {
                string workingDir = Path.GetDirectoryName(scriptPath);
                string args = $"\"{scriptPath}\" --port {port}";

                LogToUI($"[System] Launching Python: {pythonExe} (Console: {Config.ShowConsole})");
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = pythonExe,
                    Arguments = args,
                    UseShellExecute = Config.ShowConsole,
                    WorkingDirectory = workingDir
                };

                _pythonProcess = new Process();
                _pythonProcess.StartInfo = startInfo;

                _pythonProcess.Start();
            }
            catch (Exception e)
            {
                Debug.LogError($"[AiSkills] Start Python Failed: {e.Message}");
                LogToUI($"[Error] Start Python Failed: {e.Message}");
            }
        }

        /// <summary>
        /// 终止 Python 进程（尝试 HTTP 优雅关闭，失败则强杀）
        /// </summary>
        private static void KillPythonProcess()
        {
            // [关键] 即使 _pythonProcess 本地引用已丢失（如刷新后），也尝试通过 HTTP 关闭旧进程
            try
            {
                using (var c = new WebClient())
                {
                    c.Headers.Add("Content-Type", "application/json");
                    // 发送 shutdown 指令
                    c.UploadString($"http://127.0.0.1:{Config.Port}/shutdown", "POST", "{}");
                }
            }
            catch { /* 忽略连接错误，可能进程早已不存在 */ }

            if (_pythonProcess != null)
            {
                try
                {
                    if (!_pythonProcess.HasExited) _pythonProcess.Kill();
                    _pythonProcess.Dispose();
                }
                catch { }
                _pythonProcess = null;
            }
        }

        // ================= Socket 服务端逻辑 =================

        /// <summary>
        /// 启动 Unity 端的 Socket 服务器
        /// </summary>
        private static void StartServer()
        {
            if (_isRunning) return;

            int retry = 0;
            while (retry < 3)
            {
                try
                {
                    _listener = new TcpListener(IPAddress.Parse("127.0.0.1"), UNITY_PORT);
                    _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    _listener.Start();

                    _isRunning = true;
                    _serverThread = new Thread(ListenLoop) { IsBackground = true };
                    _serverThread.Start();

                    LogToUI($"[System] Unity Socket Server Started (Port: {UNITY_PORT})");
                    return;
                }
                catch (SocketException)
                {
                    retry++;
                    LogToUI($"[Warn] Port {UNITY_PORT} busy, retrying ({retry}/3)...");
                    TryKillOldListener();
                    Thread.Sleep(500);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    return;
                }
            }

            LogToUI($"[Error] Startup failed: Port {UNITY_PORT} cannot be bound.");
        }

        /// <summary>
        /// 尝试连接并关闭可能残留的旧监听器
        /// </summary>
        private static void TryKillOldListener()
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var result = client.BeginConnect("127.0.0.1", UNITY_PORT, null, null);
                    if (result.AsyncWaitHandle.WaitOne(200))
                    {
                        client.EndConnect(result);
                        using (var stream = client.GetStream())
                        {
                            byte[] data = Encoding.UTF8.GetBytes(CMD_SHUTDOWN_LISTENER);
                            stream.Write(data, 0, data.Length);
                        }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// 停止 Socket 服务器并清理资源
        /// </summary>
        private static void StopServer()
        {
            _isRunning = false;

            try { _listener?.Stop(); } catch { }
            try { _currentStream?.Close(); _currentStream?.Dispose(); } catch { }
            _listener = null;
            _currentStream = null;

            // 清理队列
            while (_commandQueue.TryDequeue(out _)) { }
            while (_logQueue.TryDequeue(out _)) { }

            if (_serverThread != null && _serverThread.IsAlive)
            {
                _serverThread.Join(200);
                _serverThread = null;
            }
        }

        /// <summary>
        /// 后台线程监听循环
        /// </summary>
        private static void ListenLoop()
        {
            while (_isRunning)
            {
                try
                {
                    var client = _listener.AcceptTcpClient();
                    _currentStream = client.GetStream();

                    byte[] buffer = new byte[1024 * 1024]; // 1MB buffer
                    int bytesRead = _currentStream.Read(buffer, 0, buffer.Length);

                    if (bytesRead > 0)
                    {
                        string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                        if (data == CMD_SHUTDOWN_LISTENER)
                        {
                            _isRunning = false;
                            client.Close();
                            break;
                        }

                        LogToUI($"[In] Received Python Command ({bytesRead} bytes)");
                        // 将指令推入队列，等待主线程执行
                        _commandQueue.Enqueue(data);
                    }
                    else
                    {
                        client.Close();
                    }
                }
                catch { /* 忽略监听中断异常 */ }
            }
        }

        // ================= 主线程更新循环 =================

        /// <summary>
        /// EditorUpdate 回调，处理主线程逻辑
        /// </summary>
        private static void OnUpdate()
        {
            // 1. 处理日志队列
            while (_logQueue.TryDequeue(out string logMsg))
            {
                OnStatusLog?.Invoke(logMsg);
            }

            // 2. 处理指令队列
            if (_commandQueue.TryDequeue(out string pythonCode))
            {
                LogToUI("[Run] Executing Python Code...");
                RunPythonCode(pythonCode);
            }
        }

        /// <summary>
        /// 使用 Unity Python (Python.NET) 运行代码
        /// </summary>
        /// <param name="code">要执行的 Python 代码</param>
        private static void RunPythonCode(string code)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                // 注意：这里使用的是 Unity 内置的 PythonRunner，而非外部进程
                // 外部进程通过 Socket 发送代码字符串，由这里在 Unity 内部上下文中执行
                PythonRunner.RunString(code);
            }
            catch (Exception e)
            {
                LogToUI($"[Error] Execution Failed: {e.GetType().Name}");
                Debug.LogError(e);
                SendError($"Unity Error: {e.Message}");
            }
            finally
            {
                sw.Stop();
                LogToUI($"[OK] Execution Finished ({sw.ElapsedMilliseconds}ms)");
            }
        }

        // ================= 消息发送辅助方法 =================

        /// <summary>
        /// 发送标准成功消息给 Python
        /// </summary>
        public static void SendMessage(string msg) =>
            SendResponse(JsonConvert.SerializeObject(new { status = "ok", message = msg }));

        /// <summary>
        /// 发送 JSON 数据结果给 Python
        /// </summary>
        public static void SendResult(string jsonContent) =>
            SendResponse($"{{\"status\":\"ok\", \"data\": {jsonContent}}}");

        /// <summary>
        /// 发送错误信息给 Python
        /// </summary>
        public static void SendError(string error) =>
            SendResponse(JsonConvert.SerializeObject(new { status = "error", message = error }));

        /// <summary>
        /// 底层发送逻辑
        /// </summary>
        private static void SendResponse(string jsonPackage)
        {
            if (_currentStream != null && _currentStream.CanWrite)
            {
                try
                {
                    LogToUI($"[Out] Sending Response ({jsonPackage.Length} bytes)...");
                    byte[] bytes = Encoding.UTF8.GetBytes(jsonPackage);
                    _currentStream.Write(bytes, 0, bytes.Length);
                    _currentStream.Flush();
                    LogToUI($"[Done] Interaction Complete");
                }
                catch (Exception e)
                {
                    LogToUI($"[Error] Send Failed: {e.Message}");
                }
            }
        }
    }
}