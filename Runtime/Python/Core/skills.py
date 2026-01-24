import os
import glob
import json
import re
import yaml

class SkillManager:
    def __init__(self, skills_dir): 
        self.skills_dir = skills_dir
        self.index = {} # 仅存储索引：{name: {path:..., desc:...}}

    def _read_frontmatter_only(self, path):
        """
        仅读取文件的前几行来解析 Frontmatter，避免读取整个文件。
        """
        try:
            head_lines = []
            with open(path, 'r', encoding='utf-8') as f:
                for _ in range(30): # 假设 Frontmatter 不会超过 30 行
                    line = f.readline()
                    head_lines.append(line)
                    if len(head_lines) > 1 and line.strip() == '---':
                        break
            
            content = "".join(head_lines)
            match = re.match(r'^---\s*\n(.*?)\n---', content, re.DOTALL)
            if match:
                meta = yaml.safe_load(match.group(1))
                return meta.get('description', ''), meta.get('name', os.path.basename(path))
        except Exception as e:
            print(f"[Warn] Failed to parse frontmatter for {path}: {e}")
        return "", os.path.basename(path)

    def _read_full_body(self, path):
        """
        读取选定技能的完整代码部分。
        """
        try:
            with open(path, 'r', encoding='utf-8') as f:
                content = f.read()
            match = re.match(r'^---\s*\n(.*?)\n---\s*\n(.*)$', content, re.DOTALL)
            body = match.group(2).strip() if match else content
            body = re.sub(r'\n{3,}', '\n\n', body) 
            return body
        except:
            return ""
    
    def scan(self):
        """
        扫描阶段：只建立索引，不加载正文。
        """
        self.index = {}
        if not os.path.exists(self.skills_dir): return
        
        for p in glob.glob(os.path.join(self.skills_dir, "*.md")):
            desc, name = self._read_frontmatter_only(p)
            self.index[name] = {
                "path": p,
                "desc": desc
            }

    def select(self, client, model, prompt):
        """
        让 AI 基于描述(Desc)来选择技能。
        """
        if len(self.index) == 0: return []
        
        # 1. 构建轻量级菜单
        lst = "\n".join([f"- {name}: {info['desc']}" for name, info in self.index.items()])
        
        try:
            # 2. 这里的 Prompt 专门用于选择
            sys_msg = "You are a skill selector. Identify which skills are needed for the user request. Return a JSON list of skill names."
            user_msg = f"Available Skills:\n{lst}\n\nUser Request: {prompt}\n\nReturn JSON list:"
            
            res = client.chat.completions.create(
                model=model, 
                messages=[{"role": "system", "content": sys_msg}, {"role": "user", "content": user_msg}], 
                temperature=0
            )
            content = res.choices[0].message.content.replace("```json","").replace("```","").strip()
            selected_names = json.loads(content)
            
            # 3. 过滤有效技能
            return [n for n in selected_names if n in self.index]
        except Exception as e:
            print(f"[SkillManager] Selection failed: {e}")
            # 失败策略：返回空列表，依赖 Base Context (unity.md) 进行兜底
            return [] 

    def build_system_prompt(self, selected_skills):
            """
            构建 System Prompt：
            1. 强制加载 'unity' (Base Context) 作为核心规则。
            2. 加载 AI 选中的其他 Skills 作为参考。
            """
            prompt_parts = []

            # 1. 核心规则
            if "unity" in self.index:
                core_path = self.index["unity"]["path"]
                core_body = self._read_full_body(core_path)
                prompt_parts.append(core_body)

            # 2. 对选中的 Skills 进行字母排序
            sorted_skills = sorted([s for s in selected_skills if s != "unity"])

            for name in sorted_skills:
                if name in self.index:
                    path = self.index[name]["path"]
                    body = self._read_full_body(path)
                    prompt_parts.append(f"\n--- Skill Reference: {name} ---\n{body}")

            return "\n\n".join(prompt_parts)