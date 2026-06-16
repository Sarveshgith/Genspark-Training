import os
import sys
from config_loader import load_config
from ai_service import AIService
from email_service import EmailService

def read_file(filepath):
    if not os.path.exists(filepath):
        print(f"Error: File not found at {filepath}")
        sys.exit(1)
    with open(filepath, "r", encoding="utf-8") as f:
        return f.read()

def main():
    config = load_config()
    
    requirements_file = os.path.join(os.path.dirname(__file__), "requirements.txt")
    prompt_file = os.path.join(os.path.dirname(__file__), "prompt.txt")
    
    requirements_text = read_file(requirements_file)
    prompt_content = read_file(prompt_file)
    
    ai_service = AIService(
        api_key=config.get("GROQ_API_KEY"),
        model=config.get("GROQ_MODEL", "llama-3.3-70b-versatile")
    )
    
    print("Analyzing requirements...")
    raw_response = ai_service.generate_requirements_analysis(prompt_content, requirements_text)
    
    output_raw_path = os.path.join(os.path.dirname(__file__), "output_raw_response.txt")
    with open(output_raw_path, "w", encoding="utf-8") as f:
        f.write(raw_response)
    print(f"Raw response saved to {output_raw_path}")
    
    subject, html_content = ai_service.parse_response(raw_response)
    
    output_html_path = os.path.join(os.path.dirname(__file__), "output_analysis.html")
    with open(output_html_path, "w", encoding="utf-8") as f:
        f.write(html_content)
    print(f"HTML analysis saved to {output_html_path}")
    
    email_service = EmailService(
        smtp_server=config.get("SMTP_SERVER", "smtp.gmail.com"),
        smtp_port=config.get("SMTP_PORT", 587),
        sender_email=config.get("SENDER_EMAIL"),
        sender_password=config.get("SENDER_PASSWORD")
    )
    
    recipient_email = config.get("RECIPIENT_EMAIL")
    email_service.send_html_email(recipient_email, subject, html_content)

if __name__ == "__main__":
    main()