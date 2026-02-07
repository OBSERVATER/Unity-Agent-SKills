using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.Networking;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Linq;
using Newtonsoft.Json.Linq;
using Observater.AiSkills.Runtime.Core;

namespace Observater.AiSkills.Editor
{
    /// <summary>
    /// AI Copilot 编辑器窗口。
    /// <para>负责 UI 交互，收集用户输入与附件路径，并发送给 Python 后端。</para>
    /// </summary>
    public class CopilotWindow : EditorWindow
    {
        // ================= 样式定义 =================
        private static readonly Color BgColor = new Color(0.12f, 0.12f, 0.12f);
        private static readonly Color InputBgColor = new Color(0.24f, 0.24f, 0.24f);
        private static readonly Color UserBubbleColor = new Color(0f, 0.47f, 0.84f);
        private static readonly Color AiBubbleColor = new Color(0.18f, 0.18f, 0.18f);
        private static readonly Color StatusBubbleColor = new Color(0.15f, 0.15f, 0.15f);
        private static readonly Color TextColor = new Color(0.8f, 0.8f, 0.8f);
        private static readonly Color StatusTextColor = new Color(0.6f, 0.6f, 0.6f);
        private static readonly Color AttachmentChipColor = new Color(0.24f, 0.31f, 0.39f);
        private static readonly Color LogBgColor = new Color(0.08f, 0.08f, 0.08f);
        private static readonly Color LogTextColor = new Color(0.6f, 0.6f, 0.6f);
        private static readonly Color CodeBlockBgColor = new Color(0.1f, 0.1f, 0.1f);
        private static readonly Color StopBtnColor = new Color(0.8f, 0.2f, 0.2f);
        private static readonly Color SkillTagColor = new Color(0.6f, 0.4f, 0.8f);

        private const string PREF_SHOW_DEBUG = "AiSkills_ShowDebug";

        // UI 元素
        private ScrollView _chatView;
        private TextField _inputField;
        private Button _sendBtn;

        // 附件相关
        private List<string> _attachments = new List<string>();
        private VisualElement _attachmentContainer;

        // 日志与设置
        private VisualElement _logContainer;
        private ScrollView _statusLogView;
        private Foldout _logFoldout;
        private Foldout _settingsFoldout;
        private Toggle _debugToggle;

        private VisualElement _currentStatusContainer;
        private Label _currentStatusLabel;

        private bool _isProcessing = false;
        private bool _showDebugLog = true;

        private UnityWebRequest _currentRequest;

        // 不支持的文件后缀（Unity端简单过滤，Python端会有更严格的检查）
        private readonly string[] _binaryExtensions = { ".dll", ".exe", ".so", ".png", ".jpg", ".mat", ".prefab", ".meta" };

        [MenuItem("Tools/AI Copilot")]
        public static void ShowWindow()
        {
            var wnd = GetWindow<CopilotWindow>();
            wnd.titleContent = new GUIContent("Copilot", EditorGUIUtility.IconContent("console.infoicon").image);
            wnd.minSize = new Vector2(400, 700);
        }

        private void OnEnable()
        {
            AiSkillsBridge.OnStatusLog += HandleStatusLog;
            _showDebugLog = EditorPrefs.GetBool(PREF_SHOW_DEBUG, true);
        }

        private void OnDisable()
        {
            AiSkillsBridge.OnStatusLog -= HandleStatusLog;
            if (_currentRequest != null) _currentRequest.Abort();
        }

        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.backgroundColor = BgColor;
            root.style.color = TextColor;

            CreateToolbar(root);
            CreateSettingsArea(root);

            _chatView = new ScrollView { style = { flexGrow = 1, flexShrink = 1 } };
            SetPadding(_chatView.style, 10);
            root.Add(_chatView);

            CreateLogArea(root);
            CreateInputArea(root);

            AddMessage("System", "Ready.", false);
            UpdateLogAreaVisibility();
        }

        private void CreateSettingsArea(VisualElement root)
        {
            var container = new VisualElement
            {
                style =
                {
                    borderBottomWidth = 1, borderBottomColor = new Color(0.2f, 0.2f, 0.2f),
                    backgroundColor = new Color(0.15f, 0.15f, 0.15f), flexShrink = 0
                }
            };
            SetPadding(container.style, 5);
            _settingsFoldout = new Foldout { text = "Settings", value = false, style = { color = TextColor } };

            if (AiSkillsBridge.Config == null) AiSkillsBridge.LoadConfig();
            var config = AiSkillsBridge.Config;

            _debugToggle = new Toggle("Show Debug Log") { value = _showDebugLog };
            _debugToggle.RegisterValueChangedCallback(evt =>
            {
                _showDebugLog = evt.newValue;
                EditorPrefs.SetBool(PREF_SHOW_DEBUG, _showDebugLog);
                UpdateLogAreaVisibility();
            });
            StyleToggleLabel(_debugToggle);
            _settingsFoldout.Add(_debugToggle);

            var consoleToggle = new Toggle("Show Python Console (Restart Req.)")
            {
                value = config.ShowConsole,
                tooltip = "Toggle to show external Python command window."
            };
            consoleToggle.RegisterValueChangedCallback(evt =>
            {
                config.ShowConsole = evt.newValue;
                AiSkillsBridge.SaveConfig();
            });
            StyleToggleLabel(consoleToggle);
            _settingsFoldout.Add(consoleToggle);

            var apiKeyField = new TextField("API Key") { value = config.ApiKey, isPasswordField = true };
            apiKeyField.RegisterValueChangedCallback(e => { config.ApiKey = e.newValue; AiSkillsBridge.SaveConfig(); });

            var baseUrlField = new TextField("Base URL") { value = config.BaseUrl };
            baseUrlField.RegisterValueChangedCallback(e => { config.BaseUrl = e.newValue; AiSkillsBridge.SaveConfig(); });

            var modelField = new TextField("Model") { value = config.Model };
            modelField.RegisterValueChangedCallback(e => { config.Model = e.newValue; AiSkillsBridge.SaveConfig(); });

            var portField = new IntegerField("Python Port") { value = config.Port };
            portField.RegisterValueChangedCallback(e => { config.Port = e.newValue; AiSkillsBridge.SaveConfig(); });

            _settingsFoldout.Add(apiKeyField);
            _settingsFoldout.Add(baseUrlField);
            _settingsFoldout.Add(modelField);
            _settingsFoldout.Add(portField);
            container.Add(_settingsFoldout);
            root.Add(container);
        }

        private void StyleToggleLabel(Toggle t)
        {
            var label = t.Q<Label>();
            if (label != null) label.style.color = TextColor;
        }

        private void UpdateLogAreaVisibility()
        {
            if (_logContainer != null)
                _logContainer.style.display = _showDebugLog ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void CreateLogArea(VisualElement root)
        {
            _logContainer = new VisualElement
            {
                style =
                {
                    borderTopWidth = 1, borderTopColor = new Color(0.2f, 0.2f, 0.2f), backgroundColor = LogBgColor,
                    maxHeight = 200, minHeight = 20, flexShrink = 0
                }
            };
            _logFoldout = new Foldout
            {
                text = "Process Log (Debug)",
                value = true,
                style = { fontSize = 11, color = LogTextColor, paddingLeft = 5 }
            };
            _statusLogView = new ScrollView { style = { height = 120 } };
            _logFoldout.Add(_statusLogView);
            _logContainer.Add(_logFoldout);
            root.Add(_logContainer);
        }

        private void CreateToolbar(VisualElement root)
        {
            var toolbar = new Toolbar();
            toolbar.style.backgroundColor = new Color(0.16f, 0.16f, 0.16f);
            toolbar.style.flexShrink = 0;

            toolbar.Add(new ToolbarButton(() =>
            {
                AiSkillsBridge.RestartPython();
                _statusLogView?.Clear();
                AddMessage("System", "Service Restarted.", false);
            })
            { text = "Restart Service", style = { unityFontStyleAndWeight = FontStyle.Bold } });

            toolbar.Add(new ToolbarButton(() => { _chatView.Clear(); }) { text = "Clear Chat" });
            toolbar.Add(new ToolbarButton(() => { _statusLogView?.Clear(); }) { text = "Clear Log" });
            root.Add(toolbar);
        }

        private void CreateInputArea(VisualElement root)
        {
            var container = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column, backgroundColor = BgColor, borderTopWidth = 1,
                    borderTopColor = new Color(0.27f, 0.27f, 0.27f), flexShrink = 0, paddingBottom = 5
                }
            };
            SetPadding(container.style, 10);

            // 附件操作栏
            CreateAttachmentTools(container);

            // 附件显示区
            _attachmentContainer = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, marginBottom = 5, minHeight = 0 }
            };
            container.Add(_attachmentContainer);

            // 输入框区域
            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row, backgroundColor = InputBgColor,
                    minHeight = 30, alignItems = Align.FlexEnd, flexWrap = Wrap.NoWrap
                }
            };
            row.style.borderTopLeftRadius = 4;
            row.style.borderTopRightRadius = 4;
            row.style.borderBottomLeftRadius = 4;
            row.style.borderBottomRightRadius = 4;

            _inputField = new TextField
            {
                multiline = true,
                style =
                {
                    flexGrow = 1, flexShrink = 1,
                    whiteSpace = WhiteSpace.Normal,
                    marginTop = 0, marginBottom = 0, marginLeft = 0, marginRight = 0, paddingBottom = 4, paddingTop = 4
                }
            };

            // Shift+Enter 发送，Enter 换行
            _inputField.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return && evt.shiftKey)
                {
                    evt.StopPropagation();
                    evt.PreventDefault();
                    OnActionClicked();
                }
            });

            _inputField.RegisterCallback<GeometryChangedEvent>(e =>
            {
                var i = _inputField.Q("unity-text-input");
                if (i != null)
                {
                    i.style.backgroundColor = Color.clear;
                    i.style.borderRightWidth = 0;
                    i.style.borderLeftWidth = 0;
                    i.style.borderTopWidth = 0;
                    i.style.borderBottomWidth = 0;
                    i.style.color = TextColor;
                    i.style.whiteSpace = WhiteSpace.Normal;
                }
            });

            _sendBtn = new Button(OnActionClicked)
            {
                text = "Send",
                style =
                {
                    width = 50, flexShrink = 0, backgroundColor = Color.clear, color = TextColor,
                    borderRightWidth = 0, borderLeftWidth = 0, borderTopWidth = 0, borderBottomWidth = 0,
                    alignSelf = Align.FlexEnd, height = 30
                }
            };

            row.Add(_inputField);
            row.Add(_sendBtn);
            container.Add(row);

            // 提示信息
            container.Add(new Label("Shift+Enter to Send")
            {
                style = { fontSize = 9, color = Color.gray, alignSelf = Align.FlexEnd, marginRight = 5 }
            });

            root.Add(container);
        }

        private void CreateAttachmentTools(VisualElement parent)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 5 } };

            // 1. 添加单个文件
            row.Add(new Button(AddFile)
            {
                text = "+ Add File",
                style = { fontSize = 10, backgroundColor = AttachmentChipColor, color = Color.white }
            });

            // 2. 添加选中的文件 (支持多选)
            row.Add(new Button(AddSelectedAssets)
            {
                text = "+ Add Selected",
                tooltip = "Add currently selected assets from Project window",
                style = { fontSize = 10, backgroundColor = AttachmentChipColor, color = Color.white }
            });

            // 3. 清空列表
            row.Add(new Button(() => { _attachments.Clear(); RefreshAttachmentList(); })
            {
                text = "Clear Files",
                style = { fontSize = 10, backgroundColor = new Color(0.3f, 0.3f, 0.3f), color = Color.white }
            });

            parent.Add(row);
        }

        private void AddFile()
        {
            string path = EditorUtility.OpenFilePanel("Select File", "Assets", "");
            if (string.IsNullOrEmpty(path)) return;

            // 转换为相对路径
            if (path.StartsWith(Application.dataPath))
                path = "Assets" + path.Substring(Application.dataPath.Length);

            if (!_attachments.Contains(path))
            {
                _attachments.Add(path);
                RefreshAttachmentList();
            }
        }

        private void AddSelectedAssets()
        {
            var selectedObjects = Selection.GetFiltered<UnityEngine.Object>(SelectionMode.Assets);
            int count = 0;
            foreach (var obj in selectedObjects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path)) continue;

                // 简单的文件夹处理：如果是文件夹，可能暂不递归添加，避免误操作
                if (Directory.Exists(path))
                {
                    // 可选：如果希望支持文件夹，可以在这里做 GetFiles
                    // 目前仅添加选中的具体文件
                    continue;
                }

                if (!_attachments.Contains(path))
                {
                    // 简单过滤
                    string ext = Path.GetExtension(path).ToLower();
                    if (_binaryExtensions.Contains(ext)) continue;

                    _attachments.Add(path);
                    count++;
                }
            }
            if (count > 0) RefreshAttachmentList();
            else HandleStatusLog("[Info] No valid text assets selected.");
        }

        private void RefreshAttachmentList()
        {
            _attachmentContainer.Clear();
            if (_attachments.Count == 0) return;

            foreach (var p in _attachments)
            {
                var label = new Label(Path.GetFileName(p))
                {
                    style =
                    {
                        backgroundColor = AttachmentChipColor,
                        color = Color.white,
                        paddingLeft = 6, paddingRight = 6, paddingTop = 2, paddingBottom = 2,
                        marginRight = 4, marginBottom = 4,
                        fontSize = 10,
                        borderTopLeftRadius = 4, borderTopRightRadius = 4,
                        borderBottomLeftRadius = 4, borderBottomRightRadius = 4
                    }
                };
                _attachmentContainer.Add(label);
            }
        }

        private void OnActionClicked()
        {
            if (_isProcessing) CancelRequest();
            else OnSendClicked();
        }

        private void UpdateUIState(bool processing)
        {
            _isProcessing = processing;
            _inputField.SetEnabled(!processing);
            if (processing)
            {
                _sendBtn.text = "Stop";
                _sendBtn.style.color = StopBtnColor;
                _sendBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            }
            else
            {
                _sendBtn.text = "Send";
                _sendBtn.style.color = TextColor;
                _sendBtn.style.unityFontStyleAndWeight = FontStyle.Normal;
                _inputField.Focus();
            }
        }

        private void CancelRequest()
        {
            if (_currentRequest != null)
            {
                _currentRequest.Abort();
                _currentRequest = null;
            }
            HandleStatusLog("[Warn] Request cancelled by user.");
            RemoveStatusBubble();
            UpdateUIState(false);
        }

        private async void OnSendClicked()
        {
            try
            {
                string p = _inputField.value.Trim();
                if (string.IsNullOrEmpty(p) && _attachments.Count == 0) return;

                // 构建用户显示的 Prompt (包含附件提示)
                string displayMsg = p;
                if (_attachments.Count > 0)
                {
                    displayMsg += $"\n\n[Attached {_attachments.Count} files]";
                }

                AddMessage("User", displayMsg, true);
                UpdateUIState(true);
                _inputField.value = "";

                // 清空附件列表（可选：发送后是否清空？通常习惯清空以防下一次误发）
                var sentAttachments = new List<string>(_attachments);
                _attachments.Clear();
                RefreshAttachmentList();

                CreateStatusBubble("Thinking...");
                HandleStatusLog($"[Req] Sending request ({sentAttachments.Count} files)...");

                await SendToPythonAndProcess(p, sentAttachments);

                RemoveStatusBubble();
            }
            catch (Exception e)
            {
                if (_currentStatusLabel != null) _currentStatusLabel.text = $"Error: {e.Message}";
                AddMessage("System", $"Client Error: {e.Message}", false);
            }
            finally
            {
                UpdateUIState(false);
                _currentRequest = null;
            }
        }

        private async Task SendToPythonAndProcess(string prompt, List<string> attachments)
        {
            var config = AiSkillsBridge.Config;

            // [关键修改] 发送路径列表 + 项目根目录
            // Path.GetDirectoryName(Application.dataPath) 通常指向 Unity 项目根文件夹 (包含 Assets, Packages 等)
            string projectRoot = Path.GetDirectoryName(Application.dataPath);

            var json = new JObject
            {
                ["prompt"] = prompt,
                ["api_key"] = config.ApiKey,
                ["base_url"] = config.BaseUrl,
                ["model"] = config.Model,

                // 发送给 Python 的数据
                ["attachments"] = JArray.FromObject(attachments),
                ["project_root"] = projectRoot
            };

            _currentRequest = UnityWebRequest.Post($"http://127.0.0.1:{config.Port}/chat",
                json.ToString(), "application/json");

            _currentRequest.downloadHandler = new DownloadHandlerBuffer();
            _currentRequest.timeout = 300; // 5分钟超时，大文件读取可能耗时
            _currentRequest.disposeUploadHandlerOnDispose = true;
            _currentRequest.disposeDownloadHandlerOnDispose = true;

            var op = _currentRequest.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (_currentRequest.result == UnityWebRequest.Result.ConnectionError ||
                _currentRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                if (_currentRequest.error != "Request aborted")
                {
                    HandleStatusLog($"[Error] Net Fail: {_currentRequest.error}");
                    AddMessage("System", $"Connection Error: {_currentRequest.error}", false);
                }
                return;
            }

            if (_currentRequest.result == UnityWebRequest.Result.Success)
            {
                HandleStatusLog("[Net] Response Received");
                try
                {
                    var root = JObject.Parse(_currentRequest.downloadHandler.text);
                    string aiReply = root["reply"]?.ToString() ?? "No reply";

                    // 处理技能显示
                    var skillsToken = root["selected_skills"];
                    string skillHeader = "";
                    if (skillsToken != null && skillsToken.HasValues)
                    {
                        var skills = skillsToken.ToObject<List<string>>();
                        string skillsStr = string.Join(", ", skills);
                        HandleStatusLog($"[Skill] Selected: {skillsStr}");
                        if (skills.Count > 0)
                            skillHeader = $"[🛠 Used Skills: {skillsStr}]\n\n";
                    }

                    // [新增] 处理 Token 用量显示 (可选)
                    if (root["usage"] != null)
                    {
                        var usage = root["usage"];
                        string total = usage["total_tokens"]?.ToString();
                        if (!string.IsNullOrEmpty(total))
                            HandleStatusLog($"[Info] Tokens: {total}");
                    }

                    AddMessage("AI", skillHeader + aiReply, false);

                    // 处理执行结果
                    if (root["execution"] != null)
                    {
                        var exec = root["execution"];
                        string status = exec["status"]?.ToString();
                        string msg = exec["message"]?.ToString() ?? "";
                        if (status == "error") HandleStatusLog($"[Error] Unity Execution: {msg}");
                        else HandleStatusLog($"[OK] Unity Execution: {msg}");
                    }
                }
                catch (Exception e)
                {
                    AddMessage("System", $"Parse Error: {e.Message}", false);
                }
            }

            _currentRequest.Dispose();
            _currentRequest = null;
        }

        // 辅助方法保持不变
        private void SetPadding(IStyle s, float v) { s.paddingTop = v; s.paddingBottom = v; s.paddingLeft = v; s.paddingRight = v; }

        private void HandleStatusLog(string msg)
        {
            rootVisualElement.schedule.Execute(() =>
            {
                if (_showDebugLog && _statusLogView != null)
                {
                    var label = new Label($"[{DateTime.Now:HH:mm:ss}] {msg}") { style = { fontSize = 10, color = LogTextColor, whiteSpace = WhiteSpace.Normal, marginBottom = 2 } };
                    if (msg.Contains("[Error]") || msg.Contains("Fail")) label.style.color = new Color(1f, 0.4f, 0.4f);
                    else if (msg.Contains("[OK]") || msg.Contains("[Done]")) label.style.color = new Color(0.4f, 1f, 0.4f);
                    else if (msg.Contains("[In]") || msg.Contains("[Out]")) label.style.color = new Color(0.4f, 0.8f, 1f);
                    else if (msg.Contains("[Skill]")) label.style.color = SkillTagColor;
                    _statusLogView.Add(label);
                    _statusLogView.schedule.Execute(() => _statusLogView.scrollOffset = new Vector2(0, _statusLogView.contentContainer.layout.height));
                }
                if (_isProcessing && _currentStatusLabel != null) { _currentStatusLabel.text = msg; if (msg.Contains("[Error]")) _currentStatusLabel.style.color = new Color(1f, 0.4f, 0.4f); }
            });
        }

        private void CreateStatusBubble(string t)
        {
            _currentStatusContainer = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 10, justifyContent = Justify.FlexStart } };
            var bubble = new VisualElement { style = { backgroundColor = StatusBubbleColor, color = StatusTextColor, maxWidth = Length.Percent(85), borderTopLeftRadius = 8, borderTopRightRadius = 8, borderBottomLeftRadius = 8, borderBottomRightRadius = 8, borderLeftWidth = 2, borderLeftColor = new Color(0.4f, 0.4f, 0.4f) } };
            SetPadding(bubble.style, 8);
            _currentStatusLabel = new Label(t) { style = { whiteSpace = WhiteSpace.Normal, fontSize = 11, unityFontStyleAndWeight = FontStyle.Italic } };
            bubble.Add(_currentStatusLabel);
            _currentStatusContainer.Add(bubble);
            _chatView.Add(_currentStatusContainer);
            _chatView.schedule.Execute(() => _chatView.scrollOffset = new Vector2(0, _chatView.contentContainer.layout.height));
        }

        private void RemoveStatusBubble() { if (_currentStatusContainer != null && _chatView.Contains(_currentStatusContainer)) { _chatView.Remove(_currentStatusContainer); } _currentStatusContainer = null; _currentStatusLabel = null; }

        private void AddMessage(string sender, string t, bool u)
        {
            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row, marginBottom = 10,
                    justifyContent = u ? Justify.FlexEnd : Justify.FlexStart
                }
            };

            var bubble = new VisualElement
            {
                style =
                {
                    backgroundColor = u ? UserBubbleColor : AiBubbleColor,
                    color = TextColor,
                    maxWidth = Length.Percent(85),
                    flexDirection = FlexDirection.Column,
                    borderTopLeftRadius = 8,
                    borderTopRightRadius = 8,
                    borderBottomLeftRadius = 8,
                    borderBottomRightRadius = 8
                }
            };

            if (u) bubble.style.borderTopRightRadius = 2;
            else bubble.style.borderTopLeftRadius = 2;

            SetPadding(bubble.style, 8);

            // 处理 Skill Tag 高亮
            if (t.StartsWith("[🛠 Used Skills:"))
            {
                int endIdx = t.IndexOf("]\n\n");
                if (endIdx > 0)
                {
                    string skillPart = t.Substring(0, endIdx + 1);
                    t = t.Substring(endIdx + 3);

                    var skillLabel = new Label(skillPart)
                    {
                        style = { color = SkillTagColor, fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 4 }
                    };
                    bubble.Add(skillLabel);
                }
            }

            var parts = Regex.Split(t, @"(```python[\s\S]*?```)");

            foreach (var part in parts)
            {
                if (string.IsNullOrWhiteSpace(part)) continue;

                if (part.StartsWith("```python"))
                {
                    string codeContent = part.Replace("```python", "").Replace("```", "").Trim();
                    var foldout = new Foldout { text = "Python Code", value = false, style = { marginTop = 5, marginBottom = 5 } };
                    var toggle = foldout.Q<Toggle>();
                    var label = toggle?.Q<Label>();
                    if (label != null) label.style.color = new Color(0.6f, 0.8f, 1f);

                    var codeLabel = new Label(codeContent)
                    {
                        style =
                        {
                            backgroundColor = CodeBlockBgColor, color = new Color(0.8f, 0.9f, 0.8f),
                            fontSize = 11, whiteSpace = WhiteSpace.Normal,
                            paddingTop = 5, paddingBottom = 5, paddingLeft = 5, paddingRight = 5,
                            borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4
                        },
                        selection = { isSelectable = true }
                    };
                    foldout.Add(codeLabel);
                    bubble.Add(foldout);
                }
                else
                {
                    bubble.Add(new Label(part.Trim()) { style = { whiteSpace = WhiteSpace.Normal, fontSize = 13 }, selection = { isSelectable = true } });
                }
            }

            row.Add(bubble);
            _chatView.Add(row);
            _chatView.schedule.Execute(() => _chatView.scrollOffset = new Vector2(0, _chatView.contentContainer.layout.height));
        }
    }
}
