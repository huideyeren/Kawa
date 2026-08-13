namespace Kawa.Grpc.Generators;

/// <summary>
/// Describes a schema-generation diagnostic that applications can report during their build.
/// </summary>
/// <param name="Id">The stable Kawa diagnostic identifier.</param>
/// <param name="Message">The human-readable explanation of the schema problem.</param>
public sealed record KawaProtoDiagnostic(string Id, string Message);
