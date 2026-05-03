using System.Diagnostics;
using System.Text.Json;
using DevCleanerPro.Helpers;
using DevCleanerPro.Models;

namespace DevCleanerPro.Services;

public class AiAgentCleanerService : ICleanerModule
{
    public string Name => "AI Agent Cleaner";
    public string Description => "Limpia cachés, logs e historial de herramientas de IA";
    public string IconGlyph => "\uE99A";

    private readonly string _localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    private readonly string _appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    private readonly string _userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    private static readonly string[] ToolNames = { "Cursor", "Claude", "Copilot", "Ollama", "LMStudio", "Windsurf", "ChatGPT", "HuggingFace", "Continue", "Trae", "Antigravity" };

    // ─── ICleanerModule ───
    public async Task<long> AnalyzeAsync(IProgress<string>? progress = null, CancellationToken ct = default)
    {
        var tools = await ScanAllAsync(progress, ct);
        return tools.Sum(t => t.SafeSize);
    }

    public async Task<CleaningResult> CleanAsync(IProgress<string>? progress = null, CancellationToken ct = default)
    {
        var tools = await ScanAllAsync(progress, ct);
        var safeItems = tools.SelectMany(t => t.Items).Where(i => i.IsSafe).ToList();
        safeItems.ForEach(i => i.IsSelected = true);
        return await CleanSelectedAsync(safeItems, progress, ct);
    }

    // ─── Detección ───
    public bool IsToolInstalled(string tool) => tool switch
    {
        "Cursor"       => Directory.Exists(Path.Combine(_localApp, "Cursor")),
        "Claude"       => Directory.Exists(Path.Combine(_localApp, "Claude")),
        "Copilot"      => Directory.Exists(Path.Combine(_appData, "Code", "User", "workspaceStorage")),
        "Ollama"       => Directory.Exists(Path.Combine(_userProfile, ".ollama")),
        "LMStudio"     => Directory.Exists(Path.Combine(_userProfile, ".lmstudio")) || Directory.Exists(Path.Combine(_userProfile, ".cache", "lm-studio")),
        "Windsurf"     => Directory.Exists(Path.Combine(_appData, "Windsurf")),
        "ChatGPT"      => Directory.Exists(Path.Combine(_localApp, "ChatGPT")),
        "HuggingFace"  => Directory.Exists(Path.Combine(_userProfile, ".cache", "huggingface")),
        "Continue"     => Directory.Exists(Path.Combine(_userProfile, ".continue")),
        "Trae"         => Directory.Exists(Path.Combine(_appData, "Trae")),
        "Antigravity"  => Directory.Exists(Path.Combine(_appData, "Antigravity")) ||
                          Directory.Exists(Path.Combine(_userProfile, ".antigravity")) ||
                          Directory.Exists(Path.Combine(_userProfile, ".gemini")),
        _ => false
    };

    public bool IsToolRunning(string tool) => tool switch
    {
        "Cursor"      => Process.GetProcessesByName("Cursor").Length > 0,
        "Claude"      => Process.GetProcessesByName("Claude").Length > 0,
        "Copilot"     => false,
        "Ollama"      => Process.GetProcessesByName("ollama").Length > 0,
        "LMStudio"    => Process.GetProcessesByName("LM Studio").Length > 0,
        "Windsurf"    => Process.GetProcessesByName("Windsurf").Length > 0,
        "ChatGPT"     => Process.GetProcessesByName("ChatGPT").Length > 0,
        "Trae"        => Process.GetProcessesByName("Trae").Length > 0,
        "Antigravity" => Process.GetProcessesByName("Antigravity").Length > 0,
        _ => false
    };

    // ─── Escaneo completo ───
    public async Task<List<AiToolInfo>> ScanAllAsync(IProgress<string>? progress = null, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            var tools = new List<AiToolInfo>();
            foreach (var name in ToolNames)
            {
                ct.ThrowIfCancellationRequested();
                if (!IsToolInstalled(name)) continue;
                progress?.Report($"Escaneando: {name}...");
                var info = new AiToolInfo { Name = name, IsInstalled = true, IsRunning = IsToolRunning(name) };
                info.Items = name switch
                {
                    "Cursor"      => ScanCursor(),
                    "Claude"      => ScanClaude(),
                    "Copilot"     => ScanCopilot(),
                    "Ollama"      => ScanOllama(),
                    "LMStudio"    => ScanLmStudio(),
                    "Windsurf"    => ScanWindsurf(),
                    "ChatGPT"     => ScanChatGpt(),
                    "HuggingFace" => ScanHuggingFace(),
                    "Continue"    => ScanContinue(),
                    "Trae"        => ScanTrae(),
                    "Antigravity" => ScanAntigravity(),
                    _ => new()
                };
                info.TotalSize = info.Items.Sum(i => i.Size);
                info.SafeSize = info.Items.Where(i => i.IsSafe).Sum(i => i.Size);
                info.UnsafeSize = info.Items.Where(i => !i.IsSafe).Sum(i => i.Size);
                tools.Add(info);
            }
            return tools;
        }, ct);
    }

    // ─── Limpieza selectiva ───
    public async Task<CleaningResult> CleanSelectedAsync(List<AiCacheItem> items, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        var result = new CleaningResult { CategoryName = Name };
        var sw = Stopwatch.StartNew();
        foreach (var item in items.Where(i => i.IsSelected))
        {
            ct.ThrowIfCancellationRequested();
            progress?.Report($"Limpiando: {item.ToolName} → {item.Description}...");
            if (Directory.Exists(item.Path))
            {
                var (freed, err) = await SafeFileOps.DeleteDirectoryAsync(item.Path, null, ct);
                result.FreedBytes += freed; result.ItemsCleaned++; result.Errors += err;
            }
            else if (File.Exists(item.Path))
            {
                try { var sz = new FileInfo(item.Path).Length; File.Delete(item.Path); result.FreedBytes += sz; result.ItemsCleaned++; }
                catch { result.Errors++; }
            }
        }
        sw.Stop(); result.Duration = sw.Elapsed;
        return result;
    }

    // ─── Modelos LLM unificados ───
    public async Task<List<LlmModelInfo>> GetAllModelsAsync(CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            var models = new List<LlmModelInfo>();
            if (IsToolInstalled("Ollama"))   models.AddRange(ScanOllamaModels());
            if (IsToolInstalled("LMStudio")) models.AddRange(ScanLmStudioModels());
            if (IsToolInstalled("HuggingFace")) models.AddRange(ScanHuggingFaceModels());
            return models;
        }, ct);
    }

    // ═══════════════════════════════════════════════
    // SCANNERS POR HERRAMIENTA
    // ═══════════════════════════════════════════════

    private List<AiCacheItem> ScanCursor()
    {
        var items = new List<AiCacheItem>();
        var la = Path.Combine(_localApp, "Cursor");
        var ad = Path.Combine(_appData, "Cursor");
        AddSafeDir(items, "Cursor", "GPU Cache", Path.Combine(la, "GPUCache"));
        AddSafeDir(items, "Cursor", "Code Cache", Path.Combine(la, "Code Cache"));
        AddSafeDir(items, "Cursor", "Blob Storage", Path.Combine(la, "blob_storage"));
        AddSafeDir(items, "Cursor", "Crashpad", Path.Combine(la, "Crashpad"));
        // CachedData — mantener solo la más reciente
        var cachedData = Path.Combine(la, "CachedData");
        if (Directory.Exists(cachedData))
        {
            var dirs = Directory.GetDirectories(cachedData).OrderByDescending(d => Directory.GetLastWriteTime(d)).Skip(1).ToList();
            foreach (var d in dirs)
                items.Add(new AiCacheItem { ToolName = "Cursor", Category = "CachedData", Description = $"Versión antigua: {Path.GetFileName(d)}", Path = d, Size = SafeFileOps.GetDirectorySize(d), IsSafe = true, LastModified = Directory.GetLastWriteTime(d) });
        }
        // Logs >7 días
        AddOldLogs(items, "Cursor", Path.Combine(ad, "logs"), 7);
        // Workspace huérfanos
        ScanWorkspaceOrphans(items, "Cursor", Path.Combine(ad, "User", "workspaceStorage"));
        // Extension AI storage
        AddSafeDirPattern(items, "Cursor", "AI Extension Storage", Path.Combine(ad, "User", "globalStorage"), "anysphere.*");
        return items;
    }

    private List<AiCacheItem> ScanClaude()
    {
        var items = new List<AiCacheItem>();
        var la = Path.Combine(_localApp, "Claude");
        AddSafeDir(items, "Claude", "Cache", Path.Combine(la, "Cache"));
        AddSafeDir(items, "Claude", "Code Cache", Path.Combine(la, "Code Cache"));
        AddSafeDir(items, "Claude", "GPU Cache", Path.Combine(la, "GPUCache"));
        AddSafeDir(items, "Claude", "Crashpad", Path.Combine(la, "Crashpad"));
        AddOldLogs(items, "Claude", Path.Combine(la, "Logs"), 7);
        // MCP Logs
        var mcpLogDir = Path.Combine(_appData, "Claude", "logs");
        if (Directory.Exists(mcpLogDir))
            foreach (var f in Directory.GetFiles(mcpLogDir, "mcp*.log"))
                items.Add(new AiCacheItem { ToolName = "Claude", Category = "MCP Logs", Description = Path.GetFileName(f), Path = f, Size = new FileInfo(f).Length, IsSafe = true, LastModified = File.GetLastWriteTime(f) });
        return items;
    }

    private List<AiCacheItem> ScanCopilot()
    {
        var items = new List<AiCacheItem>();
        // Workspace huérfanos con copilot-chat
        var wsDir = Path.Combine(_appData, "Code", "User", "workspaceStorage");
        if (Directory.Exists(wsDir))
        {
            foreach (var ws in Directory.GetDirectories(wsDir))
            {
                var copilotDir = Path.Combine(ws, "GitHub.copilot-chat");
                if (!Directory.Exists(copilotDir)) continue;
                var projPath = ReadWorkspaceProject(ws);
                bool exists = !string.IsNullOrEmpty(projPath) && (Directory.Exists(projPath) || File.Exists(projPath));
                items.Add(new AiCacheItem { ToolName = "Copilot", Category = "Workspace", Description = $"Copilot Chat: {projPath ?? "desconocido"}", Path = copilotDir, Size = SafeFileOps.GetDirectorySize(copilotDir), IsSafe = false, IsOrphan = !exists, ProjectPath = projPath, ProjectExists = exists, LastModified = Directory.GetLastWriteTime(copilotDir) });
            }
        }
        AddSafeDir(items, "Copilot", "CLI Cache", Path.Combine(_localApp, "copilot"));
        return items;
    }

    private List<AiCacheItem> ScanOllama()
    {
        var items = new List<AiCacheItem>();
        AddOldLogs(items, "Ollama", Path.Combine(_localApp, "Ollama"), 7, "*.log");
        // Blobs huérfanos
        var blobDir = Path.Combine(_userProfile, ".ollama", "models", "blobs");
        var manifestDir = Path.Combine(_userProfile, ".ollama", "models", "manifests");
        if (Directory.Exists(blobDir) && Directory.Exists(manifestDir))
        {
            var usedDigests = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var mf in Directory.EnumerateFiles(manifestDir, "*", SearchOption.AllDirectories))
            {
                try { var json = File.ReadAllText(mf); foreach (var line in json.Split('"').Where(s => s.StartsWith("sha256:"))) usedDigests.Add(line.Replace("sha256:", "sha256-")); } catch { }
            }
            foreach (var blob in Directory.GetFiles(blobDir))
            {
                var fn = Path.GetFileName(blob);
                if (!usedDigests.Contains(fn))
                    items.Add(new AiCacheItem { ToolName = "Ollama", Category = "Blob huérfano", Description = fn, Path = blob, Size = new FileInfo(blob).Length, IsSafe = true, IsOrphan = true, LastModified = File.GetLastWriteTime(blob) });
            }
        }
        return items;
    }

    private List<AiCacheItem> ScanLmStudio()
    {
        var items = new List<AiCacheItem>();
        var la = Path.Combine(_localApp, "LM Studio");
        AddSafeDir(items, "LMStudio", "Cache", Path.Combine(la, "Cache"));
        AddSafeDir(items, "LMStudio", "GPU Cache", Path.Combine(la, "GPUCache"));
        // Descargas incompletas
        var modelsDir = Path.Combine(_userProfile, ".lmstudio", "models");
        if (Directory.Exists(modelsDir))
            foreach (var f in Directory.EnumerateFiles(modelsDir, "*.part", SearchOption.AllDirectories).Concat(Directory.EnumerateFiles(modelsDir, "*.download", SearchOption.AllDirectories)))
                items.Add(new AiCacheItem { ToolName = "LMStudio", Category = "Descarga incompleta", Description = Path.GetFileName(f), Path = f, Size = new FileInfo(f).Length, IsSafe = true, LastModified = File.GetLastWriteTime(f) });
        // Historial conversaciones
        var convDir = Path.Combine(_userProfile, ".lmstudio", "conversations");
        if (Directory.Exists(convDir))
            items.Add(new AiCacheItem { ToolName = "LMStudio", Category = "Historial", Description = "Conversaciones", Path = convDir, Size = SafeFileOps.GetDirectorySize(convDir), IsSafe = false, LastModified = Directory.GetLastWriteTime(convDir) });
        return items;
    }

    private List<AiCacheItem> ScanWindsurf() => ScanElectronIde("Windsurf", "codeium.*");
    private List<AiCacheItem> ScanChatGpt()
    {
        var items = new List<AiCacheItem>();
        var la = Path.Combine(_localApp, "ChatGPT");
        AddSafeDir(items, "ChatGPT", "Cache", Path.Combine(la, "Cache"));
        AddSafeDir(items, "ChatGPT", "GPU Cache", Path.Combine(la, "GPUCache"));
        AddSafeDir(items, "ChatGPT", "Code Cache", Path.Combine(la, "Code Cache"));
        AddSafeDir(items, "ChatGPT", "Crashpad", Path.Combine(la, "Crashpad"));
        AddOldLogs(items, "ChatGPT", Path.Combine(la, "logs"), 7);
        return items;
    }

    private List<AiCacheItem> ScanHuggingFace()
    {
        var items = new List<AiCacheItem>();
        var hubDir = Path.Combine(_userProfile, ".cache", "huggingface", "hub");
        if (Directory.Exists(hubDir))
        {
            // Locks y temporales
            foreach (var f in Directory.EnumerateFiles(hubDir, "*.lock").Concat(Directory.EnumerateFiles(hubDir, "tmp*")))
                items.Add(new AiCacheItem { ToolName = "HuggingFace", Category = "Temporal", Description = Path.GetFileName(f), Path = f, Size = new FileInfo(f).Length, IsSafe = true });
        }
        var dsDir = Path.Combine(_userProfile, ".cache", "huggingface", "datasets");
        if (Directory.Exists(dsDir))
            items.Add(new AiCacheItem { ToolName = "HuggingFace", Category = "Datasets cache", Description = "Datasets descargados", Path = dsDir, Size = SafeFileOps.GetDirectorySize(dsDir), IsSafe = false });
        return items;
    }

    private List<AiCacheItem> ScanContinue()
    {
        var items = new List<AiCacheItem>();
        var baseDir = Path.Combine(_userProfile, ".continue");
        AddSafeDir(items, "Continue", "Logs", Path.Combine(baseDir, "logs"));
        var idxDir = Path.Combine(baseDir, "index");
        if (Directory.Exists(idxDir))
            items.Add(new AiCacheItem { ToolName = "Continue", Category = "Índice/Embeddings", Description = "Índice de código y embeddings", Path = idxDir, Size = SafeFileOps.GetDirectorySize(idxDir), IsSafe = false, LastModified = Directory.GetLastWriteTime(idxDir) });
        var sessDir = Path.Combine(baseDir, "sessions");
        if (Directory.Exists(sessDir))
            items.Add(new AiCacheItem { ToolName = "Continue", Category = "Sesiones", Description = "Historial de sesiones", Path = sessDir, Size = SafeFileOps.GetDirectorySize(sessDir), IsSafe = false });
        return items;
    }

    private List<AiCacheItem> ScanTrae() => ScanElectronIde("Trae", null);

    private List<AiCacheItem> ScanAntigravity()
    {
        var items = new List<AiCacheItem>();
        var ad = Path.Combine(_appData, "Antigravity");
        var programsDir = Path.Combine(_localApp, "Programs", "Antigravity");
        var geminiDir = Path.Combine(_userProfile, ".gemini");

        // a) GPU Cache (SIEMPRE SEGURO)
        AddSafeDir(items, "Antigravity", "GPU Cache", Path.Combine(ad, "GPUCache"));

        // b) Caché Electron (SIEMPRE SEGURO)
        AddSafeDir(items, "Antigravity", "Cache Electron", Path.Combine(ad, "Cache"));

        // c) Auth Tokens (REQUIERE CONFIRMACIÓN)
        var authDir = Path.Combine(ad, "auth-tokens");
        if (Directory.Exists(authDir))
            items.Add(new AiCacheItem { ToolName = "Antigravity", Category = "Auth Tokens", Description = "⚠ Eliminar cierra la sesión — requiere volver a iniciar sesión", Path = authDir, Size = SafeFileOps.GetDirectorySize(authDir), IsSafe = false, LastModified = Directory.GetLastWriteTime(authDir) });

        // d) Chrome Profile (REQUIERE CONFIRMACIÓN)
        var chromeProfile = Path.Combine(ad, "ChromeProfile");
        if (Directory.Exists(chromeProfile))
            items.Add(new AiCacheItem { ToolName = "Antigravity", Category = "Chrome Profile", Description = "⚠ Perfil del browser de automatización — resetea la extensión Browser Control", Path = chromeProfile, Size = SafeFileOps.GetDirectorySize(chromeProfile), IsSafe = false, LastModified = Directory.GetLastWriteTime(chromeProfile) });

        // e) Logs del agente (SEGURO con retención 7 días)
        AddOldLogs(items, "Antigravity", Path.Combine(ad, "logs"), 7);

        // f) Sesiones del agente (REQUIERE CONFIRMACIÓN)
        var sessionsDir = Path.Combine(ad, "sessions");
        if (Directory.Exists(sessionsDir))
            items.Add(new AiCacheItem { ToolName = "Antigravity", Category = "Sesiones", Description = "Historial de conversaciones del agente", Path = sessionsDir, Size = SafeFileOps.GetDirectorySize(sessionsDir), IsSafe = false, LastModified = Directory.GetLastWriteTime(sessionsDir) });

        // g) Workspace huérfanos (misma lógica que Cursor/Windsurf)
        ScanWorkspaceOrphans(items, "Antigravity", Path.Combine(ad, "User", "workspaceStorage"));

        // h) BigInt patch backups (INFORMATIVOS, eliminables con advertencia)
        var mainJsBak = Path.Combine(programsDir, "resources", "app", "out", "main.js.bak");
        var mainJsPatchedBak = Path.Combine(programsDir, "resources", "app", "out", "main.js.patched.bak");
        foreach (var bak in new[] { mainJsBak, mainJsPatchedBak })
        {
            if (File.Exists(bak))
                items.Add(new AiCacheItem { ToolName = "Antigravity", Category = "BigInt Backup", Description = $"⚠ Backup del parche BigInt: {Path.GetFileName(bak)} — eliminar solo si reinstalarás", Path = bak, Size = new FileInfo(bak).Length, IsSafe = false, LastModified = File.GetLastWriteTime(bak) });
        }

        // i) Gemini CLI config (~/.gemini/)
        if (Directory.Exists(geminiDir))
        {
            // .tmp → SEGURO
            var tmpDir = Path.Combine(geminiDir, ".tmp");
            AddSafeDir(items, "Antigravity", "Gemini CLI — Temporales", tmpDir);

            // oauth_creds.json → REQUIERE CONFIRMACIÓN
            var oauthFile = Path.Combine(geminiDir, "oauth_creds.json");
            if (File.Exists(oauthFile))
                items.Add(new AiCacheItem { ToolName = "Antigravity", Category = "Gemini CLI", Description = "⚠ Credenciales OAuth — eliminar cierra sesión del CLI", Path = oauthFile, Size = new FileInfo(oauthFile).Length, IsSafe = false, LastModified = File.GetLastWriteTime(oauthFile) });

            // settings.json → REQUIERE CONFIRMACIÓN
            var settingsFile = Path.Combine(geminiDir, "settings.json");
            if (File.Exists(settingsFile))
                items.Add(new AiCacheItem { ToolName = "Antigravity", Category = "Gemini CLI", Description = "Configuración local del Gemini CLI", Path = settingsFile, Size = new FileInfo(settingsFile).Length, IsSafe = false, LastModified = File.GetLastWriteTime(settingsFile) });
        }

        // Detección de ruta de instalación incorrecta
        var correctPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google Antigravity");
        if (Directory.Exists(programsDir) && !Directory.Exists(correctPath))
            items.Add(new AiCacheItem { ToolName = "Antigravity", Category = "Advertencia", Description = $"⚠ Instalado en {programsDir} (ruta incorrecta). Ruta correcta: {correctPath}", Path = programsDir, Size = 0, IsSafe = false });

        return items;
    }

    // ═══ Helpers ═══

    private List<AiCacheItem> ScanElectronIde(string name, string? globalStoragePattern)
    {
        var items = new List<AiCacheItem>();
        var la = Path.Combine(_localApp, name);
        var ad = Path.Combine(_appData, name);
        AddSafeDir(items, name, "CachedData", Path.Combine(la, "CachedData"));
        AddSafeDir(items, name, "GPU Cache", Path.Combine(la, "GPUCache"));
        AddSafeDir(items, name, "Code Cache", Path.Combine(la, "Code Cache"));
        AddSafeDir(items, name, "Crashpad", Path.Combine(la, "Crashpad"));
        AddOldLogs(items, name, Path.Combine(ad, "logs"), 7);
        ScanWorkspaceOrphans(items, name, Path.Combine(ad, "User", "workspaceStorage"));
        if (globalStoragePattern != null)
            AddSafeDirPattern(items, name, "AI Extension Storage", Path.Combine(ad, "User", "globalStorage"), globalStoragePattern);
        return items;
    }

    private void AddSafeDir(List<AiCacheItem> items, string tool, string desc, string path)
    {
        if (!Directory.Exists(path)) return;
        items.Add(new AiCacheItem { ToolName = tool, Category = "Cache", Description = desc, Path = path, Size = SafeFileOps.GetDirectorySize(path), IsSafe = true, LastModified = Directory.GetLastWriteTime(path) });
    }

    private void AddSafeDirPattern(List<AiCacheItem> items, string tool, string desc, string parentDir, string pattern)
    {
        if (!Directory.Exists(parentDir)) return;
        foreach (var d in Directory.GetDirectories(parentDir, pattern))
            items.Add(new AiCacheItem { ToolName = tool, Category = "Extension", Description = $"{desc}: {Path.GetFileName(d)}", Path = d, Size = SafeFileOps.GetDirectorySize(d), IsSafe = true });
    }

    private void AddOldLogs(List<AiCacheItem> items, string tool, string logDir, int retainDays, string pattern = "*")
    {
        if (!Directory.Exists(logDir)) return;
        var cutoff = DateTime.Now.AddDays(-retainDays);
        foreach (var f in Directory.EnumerateFiles(logDir, pattern).Where(f => File.GetLastWriteTime(f) < cutoff))
            items.Add(new AiCacheItem { ToolName = tool, Category = "Logs", Description = Path.GetFileName(f), Path = f, Size = new FileInfo(f).Length, IsSafe = true, LastModified = File.GetLastWriteTime(f) });
    }

    private void ScanWorkspaceOrphans(List<AiCacheItem> items, string tool, string wsDir)
    {
        if (!Directory.Exists(wsDir)) return;
        foreach (var ws in Directory.GetDirectories(wsDir))
        {
            var projPath = ReadWorkspaceProject(ws);
            if (string.IsNullOrEmpty(projPath)) continue;
            bool exists = Directory.Exists(projPath) || File.Exists(projPath);
            if (!exists)
                items.Add(new AiCacheItem { ToolName = tool, Category = "Workspace huérfano", Description = $"Proyecto: {projPath}", Path = ws, Size = SafeFileOps.GetDirectorySize(ws), IsSafe = false, IsOrphan = true, ProjectPath = projPath, ProjectExists = false, LastModified = Directory.GetLastWriteTime(ws) });
        }
    }

    private static string? ReadWorkspaceProject(string wsDir)
    {
        var jsonFile = Path.Combine(wsDir, "workspace.json");
        if (!File.Exists(jsonFile)) return null;
        try
        {
            var json = File.ReadAllText(jsonFile);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("folder", out var folder)) return folder.GetString()?.Replace("file:///", "").Replace("/", "\\");
            if (doc.RootElement.TryGetProperty("workspace", out var workspace)) return workspace.GetString()?.Replace("file:///", "").Replace("/", "\\");
        }
        catch { }
        return null;
    }

    // ═══ Modelos LLM ═══

    private List<LlmModelInfo> ScanOllamaModels()
    {
        var models = new List<LlmModelInfo>();
        var manifestDir = Path.Combine(_userProfile, ".ollama", "models", "manifests", "registry.ollama.ai", "library");
        if (!Directory.Exists(manifestDir)) return models;
        foreach (var modelDir in Directory.GetDirectories(manifestDir))
        {
            var modelName = Path.GetFileName(modelDir);
            foreach (var tagFile in Directory.GetFiles(modelDir))
            {
                var tag = Path.GetFileName(tagFile);
                long totalSize = 0;
                try
                {
                    var json = File.ReadAllText(tagFile);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("layers", out var layers))
                        foreach (var layer in layers.EnumerateArray())
                            if (layer.TryGetProperty("size", out var sz)) totalSize += sz.GetInt64();
                }
                catch { }
                models.Add(new LlmModelInfo { Source = "Ollama", Name = modelName, Tag = tag, Size = totalSize, Path = tagFile, CliCommand = $"ollama rm {modelName}:{tag}", LastAccessed = File.GetLastAccessTime(tagFile) });
            }
        }
        return models;
    }

    private List<LlmModelInfo> ScanLmStudioModels()
    {
        var models = new List<LlmModelInfo>();
        var modelsDir = Path.Combine(_userProfile, ".lmstudio", "models");
        if (!Directory.Exists(modelsDir)) return models;
        foreach (var f in Directory.EnumerateFiles(modelsDir, "*.gguf", SearchOption.AllDirectories))
        {
            var fi = new FileInfo(f);
            var name = fi.Name.Replace(".gguf", "");
            var quant = ExtractQuantization(name);
            models.Add(new LlmModelInfo { Source = "LMStudio", Name = name, Quantization = quant, Size = fi.Length, Path = f, LastAccessed = fi.LastAccessTime });
        }
        return models;
    }

    private List<LlmModelInfo> ScanHuggingFaceModels()
    {
        var models = new List<LlmModelInfo>();
        var hubDir = Path.Combine(_userProfile, ".cache", "huggingface", "hub");
        if (!Directory.Exists(hubDir)) return models;
        foreach (var modelDir in Directory.GetDirectories(hubDir, "models--*"))
        {
            var name = Path.GetFileName(modelDir).Replace("models--", "").Replace("--", "/");
            models.Add(new LlmModelInfo { Source = "HuggingFace", Name = name, Size = SafeFileOps.GetDirectorySize(modelDir), Path = modelDir, LastAccessed = Directory.GetLastWriteTime(modelDir) });
        }
        return models;
    }

    private static string? ExtractQuantization(string name)
    {
        var quantPatterns = new[] { "Q2_K", "Q3_K_S", "Q3_K_M", "Q3_K_L", "Q4_0", "Q4_K_S", "Q4_K_M", "Q5_0", "Q5_K_S", "Q5_K_M", "Q6_K", "Q8_0", "F16", "BF16" };
        return quantPatterns.FirstOrDefault(q => name.Contains(q, StringComparison.OrdinalIgnoreCase));
    }
}
