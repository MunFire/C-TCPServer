# AuthServer Implementation Plan (History)

This document outlines the architecture and design of the `AuthServer` which handles user Registration, Login authentication, and Token issuance.

## 1. Project Structure
```
C:\MSH_Server\
└── AuthServer\
    ├── Protos\
    │   └── auth.proto        (Packet definitions for Authentication)
    ├── Network\
    │   ├── TcpServer.cs      (Connection Listener on Port 9000)
    │   └── ClientHandler.cs   (Session Handler)
    ├── Database\
    │   └── MySqlManager.cs   (MySQL User verification & Creation)
    └── Cache\
        └── RedisManager.cs   (Issues Auth Tokens via Memurai)
```

## 2. Component Details

### Protobuf (`auth.proto`)
- `Packet` (oneof wrapper)
- `LoginRequest` (Username, Password)
- `LoginResponse` (Success flag, Message, **Token**)
- `RegisterRequest` (Username, Password)
- `RegisterResponse` (Success flag, Message)

### Authentication Flow (MySQL & Redis)
1. **Registration:**
   - Client sends `RegisterRequest(ID, PW)`.
   - `ClientHandler` invokes `MySqlManager.CreateUserAsync`.
   - Returns duplicate error messages if ID exists, or Success on INSERT.
   
2. **Login & Token Issuance:**
   - Client sends `LoginRequest(ID, PW)`.
   - `MySqlManager` verifies credentials (checks for existence, then password).
   - If valid, `RedisManager` generates a unique string `Token`.
   - Saves mapping `session:{username} -> token` with a TTL (e.g., 2 hours).
   - Returns the `Token` to the client in `LoginResponse`.

## 3. Database & Cache
- **MySQL (`authdb`)**: Stores permanent user data (id, username, password_hash (TODO), crt_time).
- **Redis (Memurai)**: Acts as an ephemeral Session store allowing the Token to be handed over to securely enter the `GameServer` without querying MySQL again.
