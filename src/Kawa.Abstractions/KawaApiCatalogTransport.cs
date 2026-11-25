namespace Kawa.Abstractions;

/// <summary>Describes an explicitly registered transport binding.</summary>
/// <param name="Kind">The transport kind, such as http, grpc, cli, or streamingHub.</param>
public sealed record KawaApiCatalogTransport(string Kind)
{
    /// <summary>Gets the RPC protocol: magicOnion or protobuf.</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? Protocol { get; init; }
    /// <summary>Gets the HTTP route.</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? Route { get; init; }
    /// <summary>Gets the HTTP method.</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? Method { get; init; }
    /// <summary>Gets the RPC service name.</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? Service { get; init; }
    /// <summary>Gets the RPC operation name.</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? Operation { get; init; }
    /// <summary>Gets the CLI command path.</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? CommandPath { get; init; }
    /// <summary>Gets the streaming hub name.</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? Hub { get; init; }
}
