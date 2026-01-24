using System;

namespace Observater.AiSkills.Runtime.Core
{
    [Serializable]
    public class AiSkillsConfig
    {
        public int Port = 5000;
        public string ApiKey = "";
        public string BaseUrl = "https://api.deepseek.com";
        public string Model = "deepseek-coder";
        public bool ShowConsole = true;
    }
}