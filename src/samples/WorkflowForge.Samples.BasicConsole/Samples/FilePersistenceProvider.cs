using System.Globalization;
using System.Text.Json;
using WorkflowForge.Extensions.Persistence.Abstractions;

namespace WorkflowForge.Samples.BasicConsole.Samples;

internal sealed class FilePersistenceProvider : IWorkflowPersistenceProvider
{
    private readonly string _root;

    private static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions
    {
        WriteIndented = true
    };

    public FilePersistenceProvider(string rootDirectory)
    {
        _root = rootDirectory;
        Directory.CreateDirectory(_root);
    }

    public Task SaveAsync(WorkflowExecutionSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        var path = GetPath(snapshot.FoundryExecutionId, snapshot.WorkflowId);
        var toStore = new PersistedModel
        {
            FoundryExecutionId = snapshot.FoundryExecutionId,
            WorkflowId = snapshot.WorkflowId,
            WorkflowName = snapshot.WorkflowName,
            NextOperationIndex = snapshot.NextOperationIndex,
            Properties = ConvertPropsToStringMap(snapshot.Properties)
        };
        var json = JsonSerializer.Serialize(toStore, JsonOpts);
        File.WriteAllText(path, json);
        return Task.CompletedTask;
    }

    public Task<WorkflowExecutionSnapshot?> TryLoadAsync(Guid foundryExecutionId, Guid workflowId, CancellationToken cancellationToken = default)
    {
        var path = GetPath(foundryExecutionId, workflowId);
        if (!File.Exists(path))
            return Task.FromResult<WorkflowExecutionSnapshot?>(null);
        var json = File.ReadAllText(path);
        var model = JsonSerializer.Deserialize<PersistedModel>(json, JsonOpts);
        if (model == null)
            return Task.FromResult<WorkflowExecutionSnapshot?>(null);

        var snapshot = new WorkflowExecutionSnapshot
        {
            FoundryExecutionId = model.FoundryExecutionId,
            WorkflowId = model.WorkflowId,
            WorkflowName = model.WorkflowName ?? string.Empty,
            NextOperationIndex = model.NextOperationIndex,
            Properties = ConvertPropsFromStringMap(model.Properties)
        };
        return Task.FromResult<WorkflowExecutionSnapshot?>(snapshot);
    }

    public Task DeleteAsync(Guid foundryExecutionId, Guid workflowId, CancellationToken cancellationToken = default)
    {
        var path = GetPath(foundryExecutionId, workflowId);
        if (File.Exists(path))
            File.Delete(path);
        return Task.CompletedTask;
    }

    private string GetPath(Guid foundryId, Guid workflowId) => Path.Combine(_root, $"{foundryId:N}_{workflowId:N}.json");

    private static Dictionary<string, string?> ConvertPropsToStringMap(Dictionary<string, object?> props)
    {
        var map = new Dictionary<string, string?>();
        foreach (var kv in props)
        {
            map[kv.Key] = kv.Value?.ToString();
        }
        return map;
    }

    private static Dictionary<string, object?> ConvertPropsFromStringMap(Dictionary<string, string?>? map)
    {
        var dict = new Dictionary<string, object?>();
        if (map == null)
            return dict;
        foreach (var kv in map)
        {
            if (kv.Value is null)
            {
                dict[kv.Key] = null;
                continue;
            }
            // Try basic types, else keep string
            if (int.TryParse(kv.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
            {
                dict[kv.Key] = i;
            }
            else if (double.TryParse(kv.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
            {
                dict[kv.Key] = d;
            }
            else if (bool.TryParse(kv.Value, out var b))
            {
                dict[kv.Key] = b;
            }
            else if (DateTime.TryParse(kv.Value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt))
            {
                dict[kv.Key] = dt;
            }
            else
            {
                dict[kv.Key] = kv.Value;
            }
        }
        return dict;
    }

    private sealed class PersistedModel
    {
        public Guid FoundryExecutionId { get; set; }
        public Guid WorkflowId { get; set; }
        public string? WorkflowName { get; set; }
        public int NextOperationIndex { get; set; }
        public Dictionary<string, string?>? Properties { get; set; }
    }
}
