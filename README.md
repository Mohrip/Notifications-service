# Notifications Service

Production-ready notification service with .NET 9, Kafka, and Docker.

## Prerequisites
- **Docker & Docker Compose** ( works on all platforms)
- **OR .NET 9 SDK** (for local development)

## How to Run

**Docker:** `docker-compose up -d` → http://localhost:5285 (API)
**Local:** `first dotnet build then dotnet run` → http://localhost:5285 (sync mode)

## System Architecture

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Client Apps   │────│ Notification API │────│  Database       │
│  (Mobile/Web)   │    │  (ASP.NET Core)  │    │  (SQLite)       │
└─────────────────┘    └────────┬─────────┘    └─────────────────┘
                                │
                                ▼
                       ┌──────────────────┐    ┌─────────────────┐
                       │   Apache Kafka   │────│ Background      │
                       │  (Message Queue) │    │ Consumer        │
                       └──────────────────┘    └─────────────────┘
                                │                       │
                                ▼                       ▼
                       ┌──────────────────┐    ┌─────────────────┐
                       │   Kafka UI       │    │ External APIs   │
                       │  (Management)    │    │ (Email/SMS)     │
                       └──────────────────┘    └─────────────────┘
```



## API Usage

**Enpoints:** 
POST http://localhost:5285/api/notifications ,
GET http://localhost:5285/api/notifications/{id}

**Payload Example:**
**in idempotencyKey assumed to get it from another service to prevent duplicates**
```json
{
  "user_id": "user123",  
  "channel": "sms",
  "template": "different_template",
  "data": {
    "message": "This should NOT create a new notification"
  },
  "idempotencyKey": "user123_2025" 
}
```
**Get:** `curl http://localhost:5285/api/notifications/{id}`
**Health:** `curl http://localhost:5285/health`

## Major Design Decisions...

### **1. Async Processing with Kafka**
- **Why**: 10x performance (2-5ms vs 20-50ms responses)
- **Benefit**: Immediate API response, background processing
- **Fallback**: Auto-sync when Kafka unavailable

### **2. Modular Monolith Architecture**
- **Why**: Balance simplicity and scalability
- **Benefit**: Easy development, can evolve to microservices later

### **3. Entity Framework Core + SQLite**
- **Why**: Rapid development, code-first
- **Production**: Configurable for PostgreSQL/SQL Server

### **4. Idempotency with DB Constraints**
- **Why**: Race condition safe duplicate prevention
- **How**: Unique constraint on idempotency key

### **5. Docker-First Development**
- **Why**: Consistent dev/prod environments
- **Features**: Complete orchestration, health checks for production

## Future Improvements

- Rate Limiting (per-user and global)
- Template Management (database-stored)
- New Channels (WhatsApp, Slack, Teams)
- Webhook Support (delivery callbacks)
- Batch Processing (bulk creation)
- Analytics Dashboard

## Key Features

- **10x faster** async processing with Kafka
- **Idempotency** prevents duplicates
- **Multi-channel** (email, SMS, push)
- **Production-ready** logging, health checks
- **Fully containerized** with persistence


## Built with .NET 9, SQLite, Apache Kafka, and Docker
