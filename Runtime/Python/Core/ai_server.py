import sys
import os
import argparse
from flask import Flask, request, jsonify

current_dir = os.path.dirname(os.path.abspath(__file__))
if current_dir not in sys.path:
    sys.path.insert(0, current_dir)

from openai import OpenAI
from config import DEFAULT_API_KEY, DEFAULT_API_BASE, DEFAULT_MODEL, SHOW_RAW_RESPONSE, SKILLS_DIR
from utils import process_attachments, extract_python_code
from skills import SkillManager
from unity_bridge import execute_in_unity

app = Flask(__name__)

# 确保 Skills 路径正确
if not os.path.isabs(SKILLS_DIR):
    SKILLS_DIR = os.path.join(current_dir, SKILLS_DIR)

sm = SkillManager(SKILLS_DIR)

@app.route('/chat', methods=['POST'])
def handle_chat():
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
    
    # 技能选择
    selected_skills = sm.select(client, d.get('model', DEFAULT_MODEL), prompt)
    sys_prompt = sm.build_system_prompt(selected_skills)
    
    # 拼接最终 Prompt：用户输入 + 附件内容
    full_prompt = prompt + attachment_context

    usage_info = {}

    try:
        if SHOW_RAW_RESPONSE:
            print(f"\n[Debug] Send: role: system, content: {sys_prompt}")
            print(f"[Debug] Send: role: user, content: {full_prompt}\n")
        res = client.chat.completions.create(
            model=d.get('model', DEFAULT_MODEL),
            messages=[
                {"role": "system", "content": sys_prompt},
                {"role": "user", "content": full_prompt}
            ],
            temperature=0.1
        )
        raw_content = res.choices[0].message.content
        if SHOW_RAW_RESPONSE:
                print(f"\n[Debug] Raw Skill Selection Response:\n{res}\n")
        code_to_run = extract_python_code(raw_content)

        if res.usage:
            usage_info = {
                "prompt_tokens": res.usage.prompt_tokens,
                "completion_tokens": res.usage.completion_tokens,
                "total_tokens": res.usage.total_tokens
            }

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
        "execution": exec_result
    })

@app.route('/shutdown', methods=['POST', 'GET'])
def shutdown():
    # 使用线程防止请求卡死
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
    args = parser.parse_args()
    print(f"Starting AI Server on port {args.port}...")
    app.run(host='127.0.0.1', port=args.port, debug=False)