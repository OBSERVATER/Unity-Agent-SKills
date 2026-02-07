import sys
import os
import argparse
import json
from flask import Flask, request, jsonify

current_dir = os.path.dirname(os.path.abspath(__file__))
if current_dir not in sys.path:
    sys.path.insert(0, current_dir)

from openai import OpenAI
from config import DEFAULT_API_KEY, DEFAULT_API_BASE, DEFAULT_MODEL, SKILLS_DIR
from utils import process_attachments, extract_python_code
from skills import SkillManager
from unity_bridge import execute_in_unity
from history import HistoryManager

app = Flask(__name__)

if not os.path.isabs(SKILLS_DIR):
    SKILLS_DIR = os.path.join(current_dir, SKILLS_DIR)

sm = SkillManager(SKILLS_DIR)
hm = None 

def generate_summary(client, model, user_prompt, ai_reply):
    try:
        summary_prompt = f"""
        Task: 一句话精准总结以下行为.
        Constraints: 不使用emoji，不使用markdown包裹.
        
        Interaction:
        User: {user_prompt[:500]}...
        AI: {ai_reply[:500]}...
        """
        
        res = client.chat.completions.create(
            model=model,
            messages=[{"role": "user", "content": summary_prompt}],
            temperature=0
        )
        return res.choices[0].message.content.strip()
    except:
        return "Interaction completed."

@app.route('/chat', methods=['POST'])
def handle_chat():
    global hm
    d = request.json
    sm.scan()

    client = OpenAI(
        api_key=d.get('api_key', DEFAULT_API_KEY),
        base_url=d.get('base_url', DEFAULT_API_BASE)
    )

    prompt = d.get('prompt', '')
    
    attachment_paths = d.get('attachments', [])
    project_root = d.get('project_root', None) 
    attachment_context = process_attachments(attachment_paths, project_root)
    
    selected_skills = sm.select(client, d.get('model', DEFAULT_MODEL), prompt)
    sys_prompt = sm.build_system_prompt(selected_skills)
    
    current_full_prompt = prompt + attachment_context

    messages = [{"role": "system", "content": sys_prompt}]
    
    if hm:
        history_msgs = hm.get_messages_for_llm(limit=12) 
        messages.extend(history_msgs)
    
    messages.append({"role": "user", "content": current_full_prompt})

    usage_info = {}
    raw_content = ""
    summary = ""
    
    try:
        res = client.chat.completions.create(
            model=d.get('model', DEFAULT_MODEL),
            messages=messages,
            temperature=0.1
        )
        raw_content = res.choices[0].message.content
        code_to_run = extract_python_code(raw_content)

        if res.usage:
            usage_info = {
                "prompt_tokens": res.usage.prompt_tokens,
                "completion_tokens": res.usage.completion_tokens,
                "total_tokens": res.usage.total_tokens
            }

        if hm:
            hm.add_entry("user", prompt) 
            summary = generate_summary(client, d.get('model', DEFAULT_MODEL), prompt, raw_content)
            hm.add_entry("assistant", raw_content, summary=summary)

    except Exception as e:
        import traceback
        traceback.print_exc()
        return jsonify({"status": "error", "reply": f"AI Error: {e}"})

    exec_result = None
    if code_to_run:
        exec_result = execute_in_unity(code_to_run)
    else:
        exec_result = {"status": "ok", "message": "No code generated."}

    return jsonify({
        "status": "ok",
        "reply": raw_content,
        "selected_skills": selected_skills,
        "usage": usage_info,
        "execution": exec_result,
        "summary": summary
    })

@app.route('/history/clear', methods=['POST'])
def clear_history():
    if hm: hm.clear()
    return jsonify({"status": "ok", "message": "History cleared."})

@app.route('/history/import', methods=['POST'])
def import_history():
    d = request.json
    content = d.get('content', '')
    if hm and hm.import_history(content):
        return jsonify({"status": "ok", "message": "History imported."})
    else:
        return jsonify({"status": "error", "message": "Invalid history format."})

@app.route('/history/get', methods=['GET'])
def get_history():
    if hm:
        return jsonify(hm.history)
    return jsonify([])

@app.route('/shutdown', methods=['POST', 'GET'])
def shutdown():
    def _exit():
        import time
        time.sleep(1)
        os._exit(0)
    
    import threading
    threading.Thread(target=_exit).start()
    return jsonify({"status": "shutdown"})

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument("--port", type=int, default=5000)
    parser.add_argument("--history", type=str, default="chat_history.json")
    args = parser.parse_args()
    
    print(f"Starting AI Server on port {args.port}...")
    print(f"History file: {args.history}")
    
    hm = HistoryManager(args.history)
    
    app.run(host='127.0.0.1', port=args.port, debug=False)