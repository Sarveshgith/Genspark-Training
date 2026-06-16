import smtplib
import ssl
from email.mime.text import MIMEText
from email.mime.multipart import MIMEMultipart

class EmailService:
    def __init__(self, smtp_server="smtp.gmail.com", smtp_port=587, sender_email=None, sender_password=None):
        self.smtp_server = smtp_server
        self.smtp_port = int(smtp_port) if smtp_port else 587
        self.sender_email = sender_email
        self.sender_password = sender_password

    def send_html_email(self, recipient_email, subject, html_content):
        """
        Sends an HTML email with text fallback, supporting TLS (port 587) or SSL (port 465).
        """
        if not self.sender_email or not self.sender_password or not recipient_email:
            print("Error: SMTP credentials or recipient email missing from config. Skipping email dispatch.")
            return False
            
        print(f"Connecting to SMTP server {self.smtp_server}:{self.smtp_port}...")
        try:
            # Create MIMEMultipart message
            message = MIMEMultipart("alternative")
            message["Subject"] = subject
            message["From"] = self.sender_email
            message["To"] = recipient_email
            
            # Plain text fallback
            text_fallback = "Please use an HTML-compatible email client to view this analysis."
            part1 = MIMEText(text_fallback, "plain")
            part2 = MIMEText(html_content, "html")
            
            message.attach(part1)
            message.attach(part2)
            
            # Setup secure SSL context
            context = ssl.create_default_context()
            
            # Determine ssl connection mode vs tls upgrade mode
            if self.smtp_port == 465:
                # SSL Connection
                with smtplib.SMTP_SSL(self.smtp_server, self.smtp_port, context=context) as server:
                    server.login(self.sender_email, self.sender_password)
                    print(f"Sending email (via SSL) to {recipient_email}...")
                    server.sendmail(self.sender_email, recipient_email, message.as_string())
            else:
                # TLS Connection (port 587 or other)
                with smtplib.SMTP(self.smtp_server, self.smtp_port) as server:
                    server.ehlo()
                    server.starttls(context=context)
                    server.ehlo()
                    server.login(self.sender_email, self.sender_password)
                    print(f"Sending email (via TLS) to {recipient_email}...")
                    server.sendmail(self.sender_email, recipient_email, message.as_string())
                    
            print("Email sent successfully!")
            return True
        except smtplib.SMTPAuthenticationError:
            print("Error: SMTP authentication failed. Please check your email and app password.")
            return False
        except smtplib.SMTPConnectError:
            print("Error: Could not connect to the SMTP server. Please check host and port.")
            return False
        except Exception as e:
            print(f"Error: Failed to send email due to an unexpected error: {e}")
            return False
