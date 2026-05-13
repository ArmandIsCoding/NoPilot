# 🚀 NoPilot

<div align="center">

**Local AI-powered code assistant that indexes your codebase and answers questions using Retrieval-Augmented Generation (RAG)**

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Semantic Kernel](https://img.shields.io/badge/Semantic_Kernel-1.73.0-00A4EF?logo=microsoft)](https://github.com/microsoft/semantic-kernel)
[![Ollama](https://img.shields.io/badge/Ollama-Local_AI-000000?logo=llama)](https://ollama.ai/)
[![sqlite-vec](https://img.shields.io/badge/sqlite--vec-0.1.7-003B57?logo=sqlite)](https://github.com/asg017/sqlite-vec)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

[🚀 Quick Start](docs/QUICKSTART.md) • [📖 Architecture](docs/ARCHITECTURE.md) • [💬 Examples](docs/EXAMPLES.md) • [🤝 Contributing](CONTRIBUTING.md)

</div>

---

## 📋 Table of Contents

- [What is NoPilot?](#what-is-nopilot)
- [Demo](#demo)
- [Features](#features)
- [Requirements](#requirements)
- [Quick Start](#quick-start)
- [Configuration](#configuration)
- [Usage](#usage)
- [Architecture](#architecture)
- [How It Works](#how-it-works)
- [Use Cases](#use-cases)
- [Troubleshooting](#troubleshooting)
- [Roadmap](#roadmap)
- [Contributing](#contributing)
- [License](#license)

### 📂 Full Documentation

- [⚡ Quick Start Guide](docs/QUICKSTART.md)
- [🏗️ Detailed Technical Architecture](docs/ARCHITECTURE.md)
- [💬 Conversation Examples](docs/EXAMPLES.md)
- [📚 Documentation Index](docs/README.md)

---

<a name="what-is-nopilot"></a>

## 🤔 What is NoPilot?

**NoPilot** is a .NET console application that turns your local codebase into a semantic knowledge base. It uses **Semantic Kernel**, **Ollama** (local LLM execution), and **sqlite-vec** to provide a code-expert chatbot without sending any data to the cloud.

### The problem it solves

- ❌ You don't want to share your code with cloud services
- ❌ GitHub Copilot doesn't know your entire private codebase
- ❌ Searching code with `grep` doesn't understand semantics or context
- ✅ **NoPilot indexes your project and answers questions with full context**

---

<a name="demo"></a>

## 🎬 Demo

```plaintext
╔══════════════════════════════════════════════════╗
║         NoPilot  ·  Local Code Assistant         ║
╚══════════════════════════════════════════════════╝
  Chat model   : deepseek-coder:6.7b
  Embeddings   : mxbai-embed-large  (1024d)
  Ollama       : http://localhost:11434
  Folder       : C:\MyProject
  Database     : nopilot.db  (2891 chunks indexed)

>> How is the dependency injection pattern structured?
[Searching for relevant context...]
[NoPilot]: The project uses Microsoft.Extensions.DependencyInjection
with a standard pattern. In Program.cs the following are registered:

1. AppSettings as a singleton from appsettings.json
2. VectorStoreService for SQLite + vec0 management
3. IngestionService for file processing
4. ChatService for RAG orchestration
5. Semantic Kernel configured with Ollama for chat and embeddings...

>> What does VectorStoreService do?
[NoPilot]: VectorStoreService encapsulates all interaction with SQLite
and sqlite-vec. Its main responsibilities are:
- Initialize the database schema (chunks + vec_chunks)
- Dynamically load the native vec0 extension based on the platform...
```

**[📺 See more usage examples →](docs/EXAMPLES.md)**

---

<a name="features"></a>

## ✨ Features

| Feature | Description |
|---|---|
| 🏠 **100% Local** | Everything runs on your machine. No external APIs, no telemetry. |
| 🧠 **Semantic RAG** | Vector search with embeddings to find relevant code. |
| 💬 **Chat with History** | Maintains conversation context (last 5 rounds). |
| ⚡ **Streaming** | Real-time responses token by token. |
| 🔌 **Configurable** | Models, folders, file extensions — all in `appsettings.json`. |
| 📦 **SQLite + sqlite-vec** | Embedded database with native vector support (KNN search). |
| 🎯 **Smart Chunking** | File splitting with overlap to preserve context. |
| 🛠️ **Semantic Kernel** | Orchestration with plugins and function calling. |
| 🌍 **Cross-Platform** | Windows, Linux, macOS (x64 and ARM64). |

---

<a name="requirements"></a>

## 📦 Requirements

### Software

- **.NET 10 SDK** ([Download here](https://dotnet.microsoft.com/download/dotnet/10.0))
- **Ollama** installed and running locally ([Installation guide](https://ollama.ai/download))

### Ollama Models

Run these commands to download the required models:

```bash
# Chat model (6.7B parameters, specialized in code)
ollama pull deepseek-coder:6.7b

# Embeddings model (1024 dimensions, optimized for RAG)
ollama pull mxbai-embed-large
```

**[🚀 Full step-by-step installation guide →](docs/QUICKSTART.md)**

---

<a name="quick-start"></a>

## 🚀 Quick Start

```bash
# 1. Clone the repository
git clone https://github.com/ArmandIsCoding/NoPilot.git
cd NoPilot/NoPilot

# 2. Set the source folder
cp appsettings.example.json appsettings.json
# Edit appsettings.json and change "SourceFolder" to your project path

# 3. Build and run
dotnet restore
dotnet build
dotnet run

# 4. Index your code
>> INGEST

# 5. Start asking questions!
>> How does the authentication system work?
```

**[📖 Detailed installation with troubleshooting →](docs/QUICKSTART.md)**

---

<a name="configuration"></a>

## ⚙️ Configuration

All configuration is centralized in `NoPilot/appsettings.json`:

```json
{
  "Ollama": {
    "Endpoint": "http://localhost:11434",
    "ChatModel": "deepseek-coder:6.7b",
    "EmbeddingModel": "mxbai-embed-large",
    "EmbeddingDimension": 1024
  },
  "Ingestion": {
    "SourceFolder": "C:\\MyProjects",
    "SupportedExtensions": [".cs", ".ts", ".js", ".py", ".go", ".md"],
    "ChunkSize": 1500,
    "ChunkOverlap": 200,
    "MaxFileSizeBytes": 1048576
  },
  "VectorStore": {
    "DatabasePath": "nopilot.db"
  }
}
```

### 🔄 Changing models

If you change `EmbeddingModel` or `EmbeddingDimension`, **delete `nopilot.db`** and run `INGEST` again (the vector table schema depends on the dimension).

**Alternative models:**

| Model | Dimension | Speed | Quality | Recommended use |
|-------|-----------|-------|---------|-----------------|
| `mxbai-embed-large` | 1024 | Medium | High | **Recommended** (balanced) |
| `nomic-embed-text` | 768 | High | Medium | Large projects (>50K files) |
| `all-minilm` | 384 | Very high | Low | Quick tests |

---

<a name="usage"></a>

## 💻 Usage

### Available commands

| Command | Description | Example |
|---------|-------------|---------|
| `INGEST` | Indexes all files from `SourceFolder` | First time or after large changes |
| `CLEAR` | Deletes the full index and history | Before changing models |
| `HELP` | Shows the list of commands | - |
| `EXIT` | Closes the application | Ctrl+C also works |
| *any text* | Chat with the assistant about your code | "What does the UserService class do?" |

### Typical workflow

```bash
# Terminal 1: Ollama Server
ollama serve

# Terminal 2: NoPilot
cd NoPilot
dotnet run

# First time: index
>> INGEST
[INGEST] 347 files | 2891 chunks indexed

# Chat
>> Explain the general architecture of the project
>> Where is the database connection defined?
>> List all services registered in DI
>> What design patterns are used in the authentication module?

# If you modify many files: re-index
>> CLEAR
>> INGEST
```

**[💬 See full conversation examples →](docs/EXAMPLES.md)**

---

<a name="architecture"></a>

## 🏗️ Architecture

```
NoPilot/
├── 📄 appsettings.json          # Central configuration
├── 📁 Configuration/
│   └── AppSettings.cs           # Strongly typed POCOs
├── 📁 Models/
│   ├── DocumentChunk.cs         # Code chunk + embedding
│   └── SearchResult.cs          # Semantic search result
├── 📁 Services/
│   ├── VectorStoreService.cs    # SQLite + sqlite-vec (KNN search)
│   ├── IngestionService.cs      # File reading, chunking, embedding generation
│   └── ChatService.cs           # RAG pipeline with history
├── 📁 Plugins/
│   └── CodebasePlugin.cs        # SK Plugin for function calling
└── 📄 Program.cs                # DI, initialization, console loop
```

### Tech Stack

| Component | Technology | Version |
|-----------|------------|---------|
| **Framework** | .NET | 10.0 |
| **AI Orchestration** | [Semantic Kernel](https://github.com/microsoft/semantic-kernel) | 1.73.0 |
| **Local LLM** | [Ollama](https://ollama.ai/) + [deepseek-coder](https://ollama.ai/library/deepseek-coder) | 6.7b |
| **Embeddings** | [mxbai-embed-large](https://ollama.ai/library/mxbai-embed-large) | 1024d |
| **Vector Store** | [SQLite](https://www.sqlite.org/) + [sqlite-vec](https://github.com/asg017/sqlite-vec) | 0.1.7-alpha.2 |
| **Dependency Injection** | Microsoft.Extensions.DependencyInjection | 10.0.2 |

**[🏗️ Full technical architecture with diagrams →](docs/ARCHITECTURE.md)**

---

<a name="how-it-works"></a>

## 🔍 How It Works

### Ingestion Pipeline

```
📁 Files on disk
    ↓ Read + filter by extension (.cs, .ts, .py...)
📝 Chunking with overlap
    ↓ Smart splitting (1500 chars, overlap 200)
🧠 Embedding generation
    ↓ mxbai-embed-large → 1024 dimensions
💾 SQLite + sqlite-vec
    ↓ Storage with optimized KNN index
✅ Ready for search
```

### RAG Chat Pipeline

```
💬 User question
    ↓ "How does authentication work?"
🔢 Question embedding
    ↓ mxbai-embed-large → float[1024]
🔍 KNN vector search
    ↓ sqlite-vec: Top 5 most similar chunks
📋 Context construction
    ↓ System prompt + History + Relevant chunks
🤖 LLM generation
    ↓ deepseek-coder:6.7b
⚡ Response streaming
    ↓ Token by token
📺 Response with context
```

### Vector Store Technology

`sqlite-vec` extends SQLite with efficient vector search capabilities:

```sql
-- Tabla de metadatos
CREATE TABLE chunks (
    id INTEGER PRIMARY KEY,
    file_path TEXT,
    content TEXT,
    chunk_index INTEGER,
    created_at TEXT
);

-- Tabla virtual con índice vectorial
CREATE VIRTUAL TABLE vec_chunks USING vec0(
    embedding float[1024]
);

-- Búsqueda KNN (K-Nearest Neighbors)
SELECT c.id, c.file_path, c.content, v.distance
FROM vec_chunks v
JOIN chunks c ON c.id = v.rowid
WHERE v.embedding MATCH @query_vector
  AND k = 5
ORDER BY v.distance ASC;
```

**Performance:** ~5-10ms for search across 10K vectors, ~15-30ms across 100K vectors.

**[🔬 Detailed technical pipeline →](docs/ARCHITECTURE.md)**

---

<a name="use-cases"></a>

## 🎯 Use Cases

### 👨‍💻 For Developers

- **Onboarding**: "How does the authentication system work?"
- **Debugging**: "Where is the 'NullReferenceException' error handled in the user service?"
- **Refactoring**: "Which files use the `IRepository` interface?"
- **Architecture**: "Explain the dependency injection pattern used in this project"
- **API Discovery**: "List all HTTP endpoints in the project"

### 🔍 For Code Review

- "Are there input validations on the API endpoints?"
- "Is async/await used correctly in database calls?"
- "List all places where `HttpContext` is accessed directly"
- "Is there duplicated code in the controllers?"

### 📚 For Documentation

- "Generate a description of the project's main services"
- "What design patterns are used?"
- "Document the authentication flow step by step"

### 🧪 For Testing

- "Which classes don't have unit tests?"
- "How is the database mocked in tests?"

**[💡 See full conversation examples →](docs/EXAMPLES.md)**

---

<a name="troubleshooting"></a>

## 🔧 Troubleshooting

### ❌ Error: `The specified module could not be found` (sqlite-vec)

**Cause:** The native `vec0.dll` extension failed to load.

**Solution:**
```bash
dotnet clean
dotnet build
```

Verifica que `vec0.dll` existe en:
- Windows: `bin/Debug/net10.0/runtimes/win-x64/native/vec0.dll`
- Linux: `bin/Debug/net10.0/runtimes/linux-x64/native/vec0.so`
- macOS: `bin/Debug/net10.0/runtimes/osx-arm64/native/vec0.dylib`

### ❌ Error: Connection refused (Ollama)

**Cause:** The Ollama service is not running.

**Solution:**
```bash
# Terminal 1: Start Ollama
ollama serve

# Terminal 2: Verify connectivity
curl http://localhost:11434/api/tags

# Verify that models are downloaded
ollama list
```

### ❌ Embeddings with incorrect dimensions

**Cause:** You changed the embeddings model but didn't update `EmbeddingDimension`.

**Solution:**
1. Check the model's dimension:
   ```bash
   ollama show mxbai-embed-large | grep "embedding"
   ```
2. Update `Ollama.EmbeddingDimension` in `appsettings.json`
3. Delete the database and re-index:
   ```bash
   rm nopilot.db
   dotnet run
   >> INGEST
   ```

### ⚠️ Ingestion is very slow

**Possible causes:**
- Very large folder (>1000 files)
- Ollama running on CPU only
- Heavy embeddings model

**Optimizations:**
1. Reduce `ChunkSize` to 1000 in `appsettings.json`
2. Limit `SupportedExtensions` to only the critical extensions
3. Reduce `MaxFileSizeBytes` to 512KB
4. Use a faster embeddings model: `nomic-embed-text` (768d)
5. If you have a GPU: configure Ollama to use CUDA

**[🔧 Full troubleshooting guide →](docs/QUICKSTART.md#-troubleshooting)**

---

<a name="roadmap"></a>

## 🧪 Roadmap

### v0.2 (Coming soon)

- [ ] **Incremental ingestion**: Detect modified files and re-index only those
- [ ] **Progress bar**: Visual progress display during ingestion
- [ ] **Config watcher**: Reload `appsettings.json` without restarting

### v0.3

- [ ] **Web interface**: Blazor Server or REST API
- [ ] **Multiple folders**: Index several projects in the same database
- [ ] **Export/Import**: Index backup and restore

### v1.0

- [ ] **Git integration**: Index only specific commits or branches
- [ ] **Unit tests**: Coverage >80%
- [ ] **Docker**: Image with Ollama pre-configured
- [ ] **Azure OpenAI**: Support as an alternative to Ollama

### Future ideas

- [ ] VSCode plugin
- [ ] Relevance metrics (feedback loop)
- [ ] Support for external documentation (URLs, PDFs)
- [ ] Multi-language RAG (change assistant language)

**[🗳️ Vote for features](https://github.com/ArmandIsCoding/NoPilot/discussions) or propose new ideas!**

---

<a name="contributing"></a>

## 🤝 Contributing

Contributions are welcome! Here are several ways to help:

### 🐛 Report Bugs

Open an [issue](https://github.com/ArmandIsCoding/NoPilot/issues) with:
- Problem description
- Steps to reproduce
- Error logs
- Software versions (.NET, Ollama, OS)

### ✨ Propose Features

Open a [discussion](https://github.com/ArmandIsCoding/NoPilot/discussions) or [issue](https://github.com/ArmandIsCoding/NoPilot/issues) with the `enhancement` tag.

### 💻 Contribute Code

1. 🍴 Fork the project
2. 🌿 Create your branch (`git checkout -b feature/AmazingFeature`)
3. 💾 Commit your changes (`git commit -m "feat: add amazing feature"`)
4. 📤 Push to the branch (`git push origin feature/AmazingFeature`)
5. 🎯 Open a Pull Request

### Priority areas

- 🧪 **Tests**: Add unit and integration test coverage
- ⚡ **Performance**: Optimize ingestion (parallelization, batch embeddings)
- 🎨 **UI**: Create a web interface with Blazor or React
- 📝 **Docs**: Improve documentation with more examples
- 🌍 **i18n**: Translate console messages

**[📖 Full contribution guide →](CONTRIBUTING.md)**

---

<a name="license"></a>

## 📜 License

This project is under the **MIT license**. You can use, modify, and distribute it freely. See [LICENSE](LICENSE) for more details.

---

## 🙏 Acknowledgements

This project is built on the shoulders of giants:

- [**Semantic Kernel**](https://github.com/microsoft/semantic-kernel) - Microsoft's AI orchestration framework
- [**Ollama**](https://ollama.ai/) - Hassle-free local LLM execution
- [**sqlite-vec**](https://github.com/asg017/sqlite-vec) - SQLite vector extension by Alex Garcia
- [**DeepSeek Coder**](https://github.com/deepseek-ai/DeepSeek-Coder) - Code-specialized model
- [**mxbai-embed-large**](https://huggingface.co/mixedbread-ai/mxbai-embed-large-v1) - Embeddings model by mixedbread.ai

Special thanks to the .NET and AI open source community.

---

## 📬 Contact & Community

- **GitHub**: [@ArmandIsCoding](https://github.com/ArmandIsCoding)
- **Discussions**: [Discussion forum](https://github.com/ArmandIsCoding/NoPilot/discussions)
- **Issues**: [Report bugs](https://github.com/ArmandIsCoding/NoPilot/issues)

---

## ⭐ Show Your Support

If **NoPilot** is useful to you:

1. ⭐ Star the repository
2. 🐦 Share it on social media
3. 📝 Write an article about your experience
4. 🤝 Contribute with code or documentation

---

## 📊 Project Stats

![GitHub stars](https://img.shields.io/github/stars/ArmandIsCoding/NoPilot?style=social)
![GitHub forks](https://img.shields.io/github/forks/ArmandIsCoding/NoPilot?style=social)
![GitHub issues](https://img.shields.io/github/issues/ArmandIsCoding/NoPilot)
![GitHub pull requests](https://img.shields.io/github/issues-pr/ArmandIsCoding/NoPilot)

---

<div align="center">

**[⬆ Back to top](#-nopilot)**

---

Made with ❤️ and .NET 10 | Powered by Semantic Kernel & Ollama

</div>
