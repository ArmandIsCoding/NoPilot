# Quickstart Guide

Get NoPilot up and running in less than 5 minutes.

---

## Step 1: Install Ollama

### Windows

```powershell
# Download the installer from https://ollama.ai/download
# Or use winget:
winget install Ollama.Ollama
```

### macOS

```bash
brew install ollama
```

### Linux

```bash
curl -fsSL https://ollama.ai/install.sh | sh
```

---

## Step 2: Download Models

```bash
# Start the Ollama server (if not running)
ollama serve

# In another terminal, download the models (this may take a few minutes)
ollama pull deepseek-coder:6.7b      # ~3.8 GB
ollama pull mxbai-embed-large        # ~669 MB

# Verify that they were downloaded correctly
ollama list
```

**Expected Output:**
```
NAME                        ID              SIZE      MODIFIED
deepseek-coder:6.7b         a18a4a4          3.8 GB    2 minutes ago
mxbai-embed-large:latest    468836162        669 MB    1 minute ago
```

---

## Step 3: Clone and Configure NoPilot

```bash
# Clone the repository
git clone https://github.com/ArmandIsCoding/NoPilot.git
cd NoPilot/NoPilot

# Copy the sample configuration file
cp appsettings.example.json appsettings.json

# Edit appsettings.json and change the SourceFolder path
# Windows: "C:\\Path\\To\\Your\\Project"
# Linux/Mac: "/home/user/my-project"
```

### Sample `appsettings.json` (Windows):

```json
{
  "Ollama": {
    "Endpoint": "http://localhost:11434",
    "ChatModel": "deepseek-coder:6.7b",
    "EmbeddingModel": "mxbai-embed-large",
    "EmbeddingDimension": 1024
  },
  "Ingestion": {
    "SourceFolder": "D:\\MyProject\\src",
    "SupportedExtensions": [".cs", ".js", ".ts", ".py", ".md"],
    "ChunkSize": 1500,
    "ChunkOverlap": 200,
    "MaxFileSizeBytes": 1048576
  },
  "VectorStore": {
    "DatabasePath": "nopilot.db"
  }
}
```

---

## Step 4: Compile and Run

```bash
# Restore NuGet packages
dotnet restore

# Build the project
dotnet build

# Run NoPilot
dotnet run
```

**Expected Output:**

```
╔══════════════════════════════════════════════════╗
║       NoPilot  ·  Local Code Assistant        ║
╚══════════════════════════════════════════════════╝
  Chat model   : deepseek-coder:6.7b
  Embeddings   : mxbai-embed-large  (1024d)
  Ollama       : http://localhost:11434
  Folder      : D:\MyProject\src
  Database    : nopilot.db  (0 chunks indexed)

  INGEST     → Indexes all files in the configured folder
  CLEAN      → Removes the index and chat history
  HELP        → Displays this message
  EXIT        → Closes the application
  <text>     → Asks about the indexed code

>>
```

---

## Step 5: Index Your Code

```
>> INGEST
[INGEST] Cleaning up previous data...
[INGEST] 143 files found to index.
[INGEST] 143/143 files | 1247 chunks | 0 omitted
[INGEST] Complete: 143 files, 1247 chunks indexed, 0 omitted.
```

⏱️ **Estimated Time:**
- 10 files → ~30 seconds
- 100 files → ~5 minutes
- 500 files → ~20 minutes

---

## Step 6: Chat with Your Code!

```
>> What does the VectorStoreService class do?
[Searching for relevant context...]
[NoPilot]: VectorStoreService is responsible for managing storage and searching 
embeddings using SQLite with the vec0...

>> How can I add support for .jsx files?
[NoPilot]: To add support for .jsx files, update the SupportedExtensions array in appsettings.json:

```json
"SupportedExtensions": [".cs", ".js", ".jsx", ".ts", ".tsx", ...]
```

Then run CLEAN and INGEST again to reindex with the new types.
```

---

## 🚨 Quick Troubleshooting

### "Connection refused" when connecting to Ollama

```bash
# Check if Ollama is running
curl http://localhost:11434/api/tags

# If not, start the server:
ollama serve
```

### "No files found to index"

- Verify that `SourceFolder` in `appsettings.json` is a valid path
- Check that there are files with the extensions in `SupportedExtensions`
- Windows: use double backslashes for paths `C:\\Path\\To\\Project`

### Ingestion is very slow

- Reduce `ChunkSize` to 1000 (less tokens per embedding)
- Limit `SupportedExtensions` to only important extensions
- Add exclusions in `MaxFileSizeBytes` (e.g. 512KB instead of 1MB)

### The model responds in English despite me asking in Spanish

- Reformulate the question with more context in Spanish
- The model `deepseek-coder` is multilingual but may prefer English in technical contexts
- Alternative: use `deepseek-coder:33b` or `codellama:13b` for better multilinguism

---

## 🎉 You're Done!

You now have NoPilot running. Try:

```
>> Explain the architecture of this project

>> Where is database configuration managed?

>> What design patterns are used?

>> List all services registered in DI
```

---

## 📚 Next Steps

- Read the [Technical Architecture](ARCHITECTURE.md) for internal details
- Check out [EXAMPLES.md](EXAMPLES.md) for more use cases
- Contribute on [GitHub](https://github.com/ArmandIsCoding/NoPilot) — see [CONTRIBUTING.md](../CONTRIBUTING.md)

---

<div align="center">

**[🏠 Go back to start](../README.md)**

</div>