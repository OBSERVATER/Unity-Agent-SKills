import json
import os
import time

class HistoryManager:
    def __init__(self, storage_path="chat_history.json"):
        self.storage_path = storage_path
        self.history = []
        self.load()

    def load(self):
        """加载历史记录"""
        if os.path.exists(self.storage_path):
            try:
                with open(self.storage_path, 'r', encoding='utf-8') as f:
                    self.history = json.load(f)
            except Exception as e:
                print(f"[History] Load failed: {e}")
                self.history = []
        else:
            self.history = []

    def save(self):
        """保存历史记录到磁盘"""
        try:
            with open(self.storage_path, 'w', encoding='utf-8') as f:
                json.dump(self.history, f, indent=2, ensure_ascii=False)
        except Exception as e:
            print(f"[History] Save failed: {e}")

    def add_entry(self, role, content, summary=None):
        """
        添加一条记录
        :param role: "user" 或 "assistant"
        :param content: 对话原始内容
        :param summary: 该轮对话的总结（通常附在 assistant 回复后）
        """
        entry = {
            "timestamp": time.time(),
            "role": role,
            "content": content
        }
        if summary:
            entry["summary"] = summary
        
        self.history.append(entry)
        self.save()

    def get_messages_for_llm(self, limit=10):
        """
        获取用于发送给 LLM 的上下文列表。
        这里可以优化：如果是很久之前的记录，只发送 summary，最近的发送 content。
        目前实现：发送最近的 limit 条完整记录。
        """
        messages = []
        # 获取最近的N条记录
        recent = self.history[-limit:] if limit > 0 else self.history
        for h in recent:
            messages.append({"role": h["role"], "content": h["content"]})
        return messages

    def clear(self):
        """清除历史"""
        self.history = []
        self.save()

    def import_history(self, json_content):
        """导入外部历史记录"""
        if isinstance(json_content, str):
            try:
                data = json.loads(json_content)
            except:
                return False
        else:
            data = json_content
            
        if isinstance(data, list):
            self.history = data
            self.save()
            return True
        return False
