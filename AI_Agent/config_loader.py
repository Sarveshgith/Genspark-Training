import os
import json
import sys

def load_config():
    config_path = os.path.join(os.path.dirname(__file__), "config.json")
    if not os.path.exists(config_path):
        print(f"Error: Config file not found at {config_path}")
        print("Please copy config.json.template to config.json and fill in your details.")
        sys.exit(1)
    
    try:
        with open(config_path, "r", encoding="utf-8") as f:
            raw_config = json.load(f)
    except json.JSONDecodeError as e:
        print(f"Error: Failed to parse JSON from config file: {e}")
        sys.exit(1)
    except IOError as e:
        print(f"Error: Failed to read config file: {e}")
        sys.exit(1)
        
    normalized_config = {}
    for key, val in raw_config.items():
        normalized_key = key.upper().replace("-", "_")
        normalized_config[normalized_key] = val
        
    placeholders = ["YOUR_GROQ_API_KEY", "your_email@gmail.com", "your_gmail_app_password", "recipient_email@gmail.com"]
    for key, val in normalized_config.items():
        if val in placeholders:
            print(f"Warning: Config key '{key}' still has a default placeholder value ({val}). Please update config.json.")
            
    return normalized_config
