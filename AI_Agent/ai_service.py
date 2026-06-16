import os
import re
import sys
import requests

class AIService:
    def __init__(self, api_key, model="llama-3.3-70b-versatile"):
        self.api_key = api_key
        self.model = model
        self.url = "https://api.groq.com/openai/v1/chat/completions"
        
        if not self.api_key or self.api_key == "YOUR_GROQ_API_KEY":
            self.api_key = os.environ.get("GROQ_API_KEY")
            
        if not self.api_key:
            print("Error: GROQ_API_KEY not found in config or environment variables.")
            sys.exit(1)

    def generate_requirements_analysis(self, prompt_template, requirements_text):
        """
        Sends the requirements text to the Groq API using the prompt template.
        """
        headers = {
            "Authorization": f"Bearer {self.api_key}",
            "Content-Type": "application/json"
        }
        
        system_prompt = prompt_template.replace("{requirements_text}", requirements_text)
        
        data = {
            "model": self.model,
            "messages": [
                {
                    "role": "user",
                    "content": system_prompt
                }
            ],
            "temperature": 0.2
        }
        
        print(f"Calling Groq API with model '{self.model}'...")
        try:
            # Added a 30-second network timeout for request robustness
            response = requests.post(self.url, headers=headers, json=data, timeout=30)
            
            if response.status_code != 200:
                print(f"API Error (Status {response.status_code}): {response.text}")
                sys.exit(1)
                
            result = response.json()
            return result["choices"][0]["message"]["content"]
            
        except requests.exceptions.Timeout:
            print("Error: The request to Groq API timed out (limit 30s). Please check your internet connection.")
            sys.exit(1)
        except requests.exceptions.RequestException as e:
            print(f"Error: Network exception occurred while connecting to Groq API: {e}")
            sys.exit(1)
        except (KeyError, IndexError, ValueError) as e:
            print(f"Error: Failed to parse Groq API response. Format might have changed: {e}")
            sys.exit(1)

    @staticmethod
    def parse_response(response_text):
        """
        Parses the Groq API response to extract Subject and HTML Body content.
        """
        subject = "Requirements Analysis and Response Plan"
        subject_match = re.search(r'(?i)^subject:\s*(.*)$', response_text, re.MULTILINE)
        if subject_match:
            subject = subject_match.group(1).strip()
            
        html_block_match = re.search(r'```html\s*(.*?)\s*```', response_text, re.DOTALL | re.IGNORECASE)
        if html_block_match:
            html_content = html_block_match.group(1).strip()
        else:
            content_without_subject = re.sub(r'(?i)^subject:\s*.*$', '', response_text, flags=re.MULTILINE).strip()
            html_tag_match = re.search(r'(<html.*?>|<div.*?>|<body.*?>|<header.*?>|<p.*?>|<table.*?>)', content_without_subject, re.IGNORECASE)
            if html_tag_match:
                start_index = html_tag_match.start()
                html_content = content_without_subject[start_index:].strip()
                if html_content.endswith("```"):
                    html_content = html_content[:-3].strip()
            else:
                html_content = content_without_subject
                
        return subject, html_content.strip()
