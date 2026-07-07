Customer Scans QR
        │
        ▼
/guest/{tableSecret}
        │
        ▼
Token Exists?
      │
     Yes
      │
      ▼
Token not expired AND tableSecret matches token's tableId claim?
      │
 ┌────┴────┐
Yes       No
 │         │
Reuse    Discard token → POST /guest-login(secret)
 │             │
 ├────No──────►│
 │             ▼
 ▼       Validate Secret
Reuse Token      │
 │               ▼
 ▼        Generate Guest JWT
Guest Dashboard  │
        └────────┘
              │
              ▼
Guest Uses Application
(Menu • Tracking • Call Waiter • Invoice)
              │
              ▼
Bill Paid / Order Cancelled
              │
              ▼
SignalR → GuestSessionEnded
              │
              ▼
Show "Session Ending" Message
              │
      Wait 10 Seconds
              │
              ▼
Remove Guest Token
              │
              ▼
Navigate to /session-ended
              │
              ▼
To Use Again:
Scan QR Again