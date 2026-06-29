# 🚀 Caching Proxy

A lightweight CLI-based **caching proxy server** built with C# and .NET 10. It sits between a client and an origin server, forwarding GET requests and caching responses locally — so repeated requests are served instantly from the cache without hitting the origin again.

---

## 📋 How It Works

```
Client → Caching Proxy → Origin Server
                ↓
         cachedRequests.json
```

1. A client sends a GET request to the proxy.
2. The proxy checks if the response is already cached.
   - **Cache Hit:** Serves the cached response immediately with `X-Cache: Hit`.
   - **Cache Miss:** Forwards the request to the origin server, caches the response, and serves it with `X-Cache: Miss`.
3. All cached responses are stored in `cachedRequests.json`, including headers, body, and status code.

---

## ⚙️ Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

---

## 🛠️ Installation

Clone the repository and navigate to the project directory:

```bash
git clone <repository-url>
cd Caching_Proxy
```

Build the project:

```bash
dotnet build
```

---

## 🚦 Usage

### Start the proxy server

```bash
dotnet run -- port <PORT> origin <ORIGIN_URL>
```

**Arguments:**

| Argument | Description | Example |
|----------|-------------|---------|
| `port` | Keyword — must be literally `port` | `port` |
| `<PORT>` | The port number the proxy will listen on | `8080` |
| `origin` | Keyword — must be literally `origin` | `origin` |
| `<ORIGIN_URL>` | The base URL of the target server to proxy | `https://example.com` |

**Example:**

```bash
dotnet run -- port 8080 origin https://jsonplaceholder.typicode.com
```

The proxy will start listening at `http://localhost:8080`. Any GET request you send to it will be forwarded to `https://jsonplaceholder.typicode.com`.

```bash
# First request — fetched from origin (Cache Miss)
curl http://localhost:8080/todos/1

# Second request — served from cache (Cache Hit)
curl http://localhost:8080/todos/1
```

### Clear the cache

To wipe all cached responses:

```bash
dotnet run -- clear
```

---

## 📡 Response Headers

| Header | Value | Description |
|--------|-------|-------------|
| `X-Cache` | `Miss` | Response was fetched live from the origin server |
| `X-Cache` | `Hit` | Response was served from the local cache |

---

## ❌ Error Handling

| Scenario | Behavior |
|----------|----------|
| Non-GET request (POST, PUT, DELETE, etc.) | Returns `405 Method Not Allowed` with `Allow: GET` header |
| Origin server unreachable / DNS failure | Returns `503 Service Unavailable`, server keeps running |
| Client disconnects mid-response | Logged gracefully, server keeps running |

---

## 📁 Project Structure

```
Caching_Proxy/
├── Program.cs              # Entry point — server loop and request routing
├── utilities.cs            # Tools class — caching, header handling, response writing
├── cachedRequests.json     # Auto-generated cache storage file
├── Caching_Proxy.csproj    # Project configuration
└── README.md               # This file
```

---

## ⚠️ Limitations

- Only **GET** requests are supported.
- The cache is **persistent** (stored on disk as JSON) but has **no expiry** — cached responses live forever until manually cleared.
- No concurrency locking — simultaneous requests may cause cache write conflicts under heavy load.

---

## 🗺️ Project Reference

This project was built as part of the [roadmap.sh](https://roadmap.sh) backend project challenges.
Check out the full project spec here: [Caching Server — roadmap.sh](https://roadmap.sh/projects/caching-server)

---

## 📄 License

This project is open source and available under the [MIT License](LICENSE).
