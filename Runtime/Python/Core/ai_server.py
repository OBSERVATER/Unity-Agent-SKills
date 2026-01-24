import sys
import os
import argparse
from flask import Flask, request, jsonify

current_dir = os.path.dirname(os.path.abspath(__file__))
if current_dir not in sys.path:
    sys.path.insert(0, current_dir)

from openai import OpenAI
from config import DEFAULT_API_KEY, DEFAULT_API_BASE, DEFAULT_MODEL, SKILLS_DIR
from utils import process_attachments, extract_python_code
from skills import SkillManager
from unity_bridge import execute_in_unity

app = Flask(__name__)
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
    selected_skills = sm.select(client, d.get('model', DEFAULT_MODEL), prompt)
    sys_prompt = sm.build_system_prompt(selected_skills)
    full_prompt = prompt + process_attachments(d.get('attachments', []))

    usage_info = {} # [新增] 用于存储 token 信息

    try:
        res = client.chat.completions.create(
            model=d.get('model', DEFAULT_MODEL),
            messages=[
                {"role": "system", "content": sys_prompt},
                {"role": "user", "content": full_prompt}
            ],
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

    except Exception as e:
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
        "usage": usage_info, # [新增] 返回 token 信息
        "execution": exec_result
    })

@app.route('/shutdown', methods=['POST', 'GET'])
def shutdown():
    os._exit(0)
    return jsonify({"status": "shutdown"})

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument("--port", type=int, default=5000)
    args = parser.parse_args()
    print(f"Starting AI Server on port {args.port}...")
    app.run(host='127.0.0.1', port=args.port, debug=False)