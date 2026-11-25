using Kawa.Grpc.Generators;
using Microsoft.Build.Utilities;
using Xunit;

namespace Kawa.Grpc.Tests;

/// <summary>
/// Verifies deterministic Kawa-originated protobuf schema generation.
/// </summary>
public sealed class KawaProtoSchemaGeneratorTests
{
    /// <summary>
    /// Ensures a unary service and its annotated messages become a usable protobuf schema.
    /// </summary>
    [Fact]
    public void Generate_EmitsUnaryServiceAndAnnotatedMessages()
    {
        var result = KawaProtoSchemaGenerator.Generate(
            "chat.contracts",
            [
                new KawaProtoServiceDefinition(
                    "Chat",
                    "Send",
                    new KawaProtoMessageDefinition("SendRequest", [new KawaProtoFieldDefinition("message", "string", 1)]),
                    new KawaProtoMessageDefinition("SendResponse", [new KawaProtoFieldDefinition("accepted", "bool", 1)])),
            ]);

        Assert.Empty(result.Diagnostics);
        Assert.Contains("package chat.contracts;", result.Schema);
        Assert.Contains("service Chat", result.Schema);
        Assert.Contains("rpc Send(SendRequest) returns (SendResponse);", result.Schema);
        Assert.Contains("string message = 1;", result.Schema);
        Assert.Contains("bool accepted = 1;", result.Schema);
    }

    /// <summary>
    /// Ensures all Kawa streaming use case contracts retain their direction in the generated protobuf service.
    /// </summary>
    [Fact]
    public void Generate_EmitsStreamingDirectionsForEveryOperationMode()
    {
        var request = new KawaProtoMessageDefinition("Request", [new KawaProtoFieldDefinition("message", "string", 1)]);
        var response = new KawaProtoMessageDefinition("Response", [new KawaProtoFieldDefinition("message", "string", 1)]);
        var result = KawaProtoSchemaGenerator.Generate(
            "chat.contracts",
            [
                new KawaProtoServiceDefinition("Chat", "Watch", request, response, KawaGrpcOperationKind.ServerStreaming),
                new KawaProtoServiceDefinition("Chat", "Sum", request, response, KawaGrpcOperationKind.ClientStreaming),
                new KawaProtoServiceDefinition("Chat", "Echo", request, response, KawaGrpcOperationKind.DuplexStreaming),
            ]);

        Assert.Empty(result.Diagnostics);
        Assert.Contains("rpc Watch(Request) returns (stream Response);", result.Schema);
        Assert.Contains("rpc Sum(stream Request) returns (Response);", result.Schema);
        Assert.Contains("rpc Echo(stream Request) returns (stream Response);", result.Schema);
    }

    /// <summary>
    /// Ensures reused field numbers fail schema generation instead of silently changing a protocol.
    /// </summary>
    [Fact]
    public void Generate_ReportsDuplicateFieldNumber()
    {
        var result = KawaProtoSchemaGenerator.Generate(
            "chat.contracts",
            [
                new KawaProtoServiceDefinition(
                    "Chat",
                    "Send",
                    new KawaProtoMessageDefinition(
                        "SendRequest",
                        [
                            new KawaProtoFieldDefinition("message", "string", 1),
                            new KawaProtoFieldDefinition("author", "string", 1),
                        ]),
                    new KawaProtoMessageDefinition("SendResponse", [new KawaProtoFieldDefinition("accepted", "bool", 1)])),
            ]);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "KAWA5101");
    }

    /// <summary>
    /// Ensures the MSBuild-facing task writes only to the intermediate output location.
    /// </summary>
    [Fact]
    public void Task_EmitsSchemaUnderIntermediateOutputPath()
    {
        var temporaryDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var outputPath = Path.Combine(temporaryDirectory, "Kawa", "chat.proto");
        var task = new KawaProtoSchemaGenerationTask
        {
            PackageName = "chat.contracts",
            OutputPath = outputPath,
            Services =
            [
                new KawaProtoServiceDefinition(
                    "Chat",
                    "Send",
                    new KawaProtoMessageDefinition("SendRequest", [new KawaProtoFieldDefinition("message", "string", 1)]),
                    new KawaProtoMessageDefinition("SendResponse", [new KawaProtoFieldDefinition("accepted", "bool", 1)])),
            ],
        };

        try
        {
            Assert.True(task.Execute());
            Assert.True(File.Exists(outputPath));
            Assert.Contains("service Chat", File.ReadAllText(outputPath));
        }
        finally
        {
            if (Directory.Exists(temporaryDirectory))
            {
                Directory.Delete(temporaryDirectory, recursive: true);
            }
        }
    }

    /// <summary>
    /// Ensures the build task discovers an annotated Kawa unary use case and its numbered contracts.
    /// </summary>
    [Fact]
    public void Task_DiscoversKawaUseCaseFromSourceFile()
    {
        var temporaryDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var sourcePath = Path.Combine(temporaryDirectory, "ChatContracts.cs");
        var outputPath = Path.Combine(temporaryDirectory, "obj", "Kawa", "chat.proto");
        Directory.CreateDirectory(temporaryDirectory);
        File.WriteAllText(sourcePath, """
            using Kawa.Abstractions;
            using Kawa.Grpc;

            [KawaProtoService("Chat")]
            public sealed class SendUseCase : IUseCase<SendRequest, SendResponse>
            {
                public Task<KawaResult<SendResponse>> ExecuteAsync(SendRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            }

            public sealed class SendRequest
            {
                [KawaProtoField(1)]
                public string Message { get; init; } = string.Empty;
                [KawaProtoField(2)] public byte[] Payload { get; init; } = [];
            }

            public sealed class SendResponse
            {
                [KawaProtoField(1)]
                public bool Accepted { get; init; }
            }
            """);
        var task = new KawaProtoSchemaGenerationTask
        {
            PackageName = "chat.contracts",
            OutputPath = outputPath,
            SourceFiles = [new TaskItem(sourcePath)],
            ReferenceFiles = [new TaskItem(typeof(Kawa.Abstractions.IUseCase<,>).Assembly.Location), new TaskItem(typeof(Kawa.Grpc.KawaProtoServiceAttribute).Assembly.Location)],
        };

        try
        {
            Assert.True(task.Execute());
            var schema = File.ReadAllText(outputPath);
            Assert.Contains("service Chat", schema);
            Assert.Contains("rpc Send(SendRequest) returns (SendResponse);", schema);
            Assert.Contains("string message = 1;", schema);
            Assert.Contains("bytes payload = 2;", schema);
            Assert.Contains("bool accepted = 1;", schema);
        }
        finally
        {
            if (Directory.Exists(temporaryDirectory))
            {
                Directory.Delete(temporaryDirectory, recursive: true);
            }
        }
    }

    /// <summary>
    /// Ensures an unnumbered public contract member fails generation instead of disappearing from the protocol.
    /// </summary>
    [Fact]
    public void Task_ReportsUnnumberedPublicContractMember()
    {
        var temporaryDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var sourcePath = Path.Combine(temporaryDirectory, "ChatContracts.cs");
        Directory.CreateDirectory(temporaryDirectory);
        File.WriteAllText(sourcePath, """
            using Kawa.Abstractions;
            using Kawa.Grpc;

            [KawaProtoService("Chat")]
            public sealed class SendUseCase : IUseCase<SendRequest, SendResponse>
            {
                public Task<KawaResult<SendResponse>> ExecuteAsync(SendRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            }

            public sealed class SendRequest
            {
                public string Message { get; init; } = string.Empty;
            }

            public sealed class SendResponse
            {
                [KawaProtoField(1)]
                public bool Accepted { get; init; }
            }
            """);
        var task = new KawaProtoSchemaGenerationTask
        {
            PackageName = "chat.contracts",
            OutputPath = Path.Combine(temporaryDirectory, "obj", "Kawa", "chat.proto"),
            SourceFiles = [new TaskItem(sourcePath)],
            ReferenceFiles = [new TaskItem(typeof(Kawa.Abstractions.IUseCase<,>).Assembly.Location), new TaskItem(typeof(Kawa.Grpc.KawaProtoServiceAttribute).Assembly.Location)],
        };

        try
        {
            Assert.False(task.Execute());
            Assert.Contains(task.Diagnostics, diagnostic => diagnostic.Id == "KAWA5102");
        }
        finally
        {
            if (Directory.Exists(temporaryDirectory))
            {
                Directory.Delete(temporaryDirectory, recursive: true);
            }
        }
    }
    /// <summary>Invalid members after the first property must be diagnosed instead of omitted.</summary>
    [Theory]
    [InlineData("public string Missing { get; init; } = string.Empty;", "KAWA5102")]
    [InlineData("[KawaProtoField(1)] public string Duplicate { get; init; } = string.Empty;", "KAWA5101")]
    [InlineData("[KawaProtoField(19000)] public string Reserved { get; init; } = string.Empty;", "KAWA5101")]
    [InlineData("[KawaProtoField(2)] public decimal Unsupported { get; init; }", "KAWA5103")]
    public void Task_DiagnosesInvalidTrailingMembers(string member, string diagnostic)
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "Contracts.cs");
        File.WriteAllText(path, """
            using Kawa.Abstractions;
            using Kawa.Grpc;
            [KawaProtoService("Chat")]
            public class SendUseCase : IUseCase<Request, Request>
            {
                public System.Threading.Tasks.Task<KawaResult<Request>> ExecuteAsync(Request request, System.Threading.CancellationToken cancellationToken = default) => throw null!;
            }
            public class Request
            {
                [KawaProtoField(1)] public string Message { get; init; } = "literal }";
                MEMBER
            }
            public class Unrelated { public string Setting { get; init; } = ""; }
            """.Replace("MEMBER", member));
        var task = new KawaProtoSchemaGenerationTask
        {
            PackageName = "test", OutputPath = Path.Combine(directory, "obj", "generated.proto"),
            SourceFiles = [new TaskItem(path)],
            ReferenceFiles = [new TaskItem(typeof(Kawa.Abstractions.IUseCase<,>).Assembly.Location), new TaskItem(typeof(Kawa.Grpc.KawaProtoServiceAttribute).Assembly.Location)],
        };
        try
        {
            Assert.False(task.Execute());
            Assert.Contains(task.Diagnostics, item => item.Id == diagnostic);
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

}
