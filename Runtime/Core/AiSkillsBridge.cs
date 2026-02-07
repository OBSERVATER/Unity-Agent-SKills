using UnityEngine;
using UnityEditor;
using UnityEditor.Scripting.Python;
using UnityEditor.Compilation;
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
    [InitializeOnLoad]
    public static class AiSkillsBridge
    {
        public static event Action<string> OnStatusLog;

        private static readonly ConcurrentQueue<string> _logQueue = new ConcurrentQueue<string>();

        private static void LogToUI(string msg) => _logQueue.Enqueue(msg);

        private const int UNITY_PORT = 8081;

        private const string CMD_SHUTDOWN_LISTENER = "@@INTERNAL_STOP@@";

        private const string PACKAGE_NAME = "com.observater.aiskills";

        private const string RELATIVE_SCRIPT_PATH = "Runtime/Python/Core/ai_server.py";

        private const string RELATIVE_PYTHON_EXE_PATH = "Runtime/Python/python.exe";

        private static string ConfigPath => Path.Combine(Application.dataPath, "../ProjectSettings/AiSkillsConfig.json");

        public static string HistoryPath => Path.GetFullPath(Path.Combine(Application.dataPath, "../ProjectSettings/AiSkills_History.json"));

        public static AiSkillsConfig Config { get; private set; }

        private static TcpListener _listener;
        private static Thread _serverThread;
        private static volatile bool _isRunning = false;
        private static NetworkStream _currentStream;
        private static Process _pythonProcess;

        private static readonly ConcurrentQueue<string> _commandQueue = new ConcurrentQueue<string>();

        static AiSkillsBridge()
        {
            LoadConfig();
            EditorApplication.update += OnUpdate;
            EditorApplication.quitting += () => RestartPython(true, false);
            AssemblyReloadEvents.beforeAssemblyReload += () => RestartPython(true, false);
            EditorApplication.delayCall += () => RestartPython(true);
        }

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
                LoadConfig();
                LogToUI($"[System] Starting services (Port {Config.Port} <-> {UNITY_PORT})...");
                StartServer();
                CheckAndStartPython();
            }
        }

        private static string GetPackageRootPath()
        {
            string packagePath = Path.GetFullPath($"Packages/{PACKAGE_NAME}");
            if (Directory.Exists(packagePath)) return packagePath;

            string assetsPath = Path.GetFullPath($"Assets/{PACKAGE_NAME}");
            if (Directory.Exists(assetsPath)) return assetsPath;

            var guids = AssetDatabase.FindAssets("ai_server");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return Path.GetFullPath(path + "/../../../../");
            }

            return null;
        }

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

            if (!File.Exists(pythonExePath))
            {
                LogToUI($"[Warn] Embedded Python not found. Trying system 'python'...");
                pythonExePath = "python";
            }

            StartPythonProcess(pythonExePath, scriptPath, Config.Port);
        }

        private static void StartPythonProcess(string pythonExe, string scriptPath, int port)
        {
            try
            {
                string workingDir = Path.GetDirectoryName(scriptPath);
                string args = $"\"{scriptPath}\" --port {port} --history \"{HistoryPath}\"";

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

        private static void KillPythonProcess()
        {
            try
            {
                using (var c = new WebClient())
                {
                    c.Headers.Add("Content-Type", "application/json");
                    c.UploadString($"http://127.0.0.1:{Config.Port}/shutdown", "POST", "{}");
                }
            }
            catch { }

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

        private static void StopServer()
        {
            _isRunning = false;

            try { _listener?.Stop(); } catch { }
            try { _currentStream?.Close(); _currentStream?.Dispose(); } catch { }
            _listener = null;
            _currentStream = null;

            while (_commandQueue.TryDequeue(out _)) { }
            while (_logQueue.TryDequeue(out _)) { }

            if (_serverThread != null && _serverThread.IsAlive)
            {
                _serverThread.Join(200);
                _serverThread = null;
            }
        }

        private static void ListenLoop()
        {
            while (_isRunning)
            {
                try
                {
                    var client = _listener.AcceptTcpClient();
                    _currentStream = client.GetStream();

                    byte[] buffer = new byte[1024 * 1024];
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
                        _commandQueue.Enqueue(data);
                    }
                    else
                    {
                        client.Close();
                    }
                }
                catch { }
            }
        }

        private static void OnUpdate()
        {
            while (_logQueue.TryDequeue(out string logMsg))
            {
                OnStatusLog?.Invoke(logMsg);
            }

            if (_commandQueue.TryDequeue(out string pythonCode))
            {
                LogToUI("[Run] Executing Python Code...");
                RunPythonCode(pythonCode);
            }
        }

        private static void RunPythonCode(string code)
        {
            var sw = Stopwatch.StartNew();

            try
            {
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

        public static void SendMessage(string msg) =>
            SendResponse(JsonConvert.SerializeObject(new { status = "ok", message = msg }));

        public static void SendResult(string jsonContent) =>
            SendResponse($"{{\"status\":\"ok\", \"data\": {jsonContent}}}");

        public static void SendError(string error) =>
            SendResponse(JsonConvert.SerializeObject(new { status = "error", message = error }));

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