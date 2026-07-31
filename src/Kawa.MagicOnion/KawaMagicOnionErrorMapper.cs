using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Grpc.Core;
using Kawa.Abstractions;

namespace Kawa.MagicOnion;

/// <summary>
/// Converts predictable Kawa application errors into gRPC status information.
/// </summary>
public static class KawaMagicOnionErrorMapper
{
    private static readonly JsonSerializerOptions ErrorJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>
    /// Maps a Kawa error to a gRPC status and its portable structured trailer payload.
    /// </summary>
    /// <param name="error">The predictable application error to map.</param>
    /// <returns>The gRPC status details for the error.</returns>
    public static KawaMagicOnionMappedError Map(KawaError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new KawaMagicOnionMappedError(
            error.Kind switch
            {
                KawaErrorKind.Validation => StatusCode.InvalidArgument,
                KawaErrorKind.Unauthorized => StatusCode.Unauthenticated,
                KawaErrorKind.Forbidden => StatusCode.PermissionDenied,
                KawaErrorKind.NotFound => StatusCode.NotFound,
                KawaErrorKind.Conflict => StatusCode.Aborted,
                KawaErrorKind.Cancelled => StatusCode.Cancelled,
                KawaErrorKind.DeadlineExceeded => StatusCode.DeadlineExceeded,
                KawaErrorKind.AlreadyExists => StatusCode.AlreadyExists,
                KawaErrorKind.PreconditionFailed => StatusCode.FailedPrecondition,
                KawaErrorKind.ResourceExhausted => StatusCode.ResourceExhausted,
                KawaErrorKind.Unavailable => StatusCode.Unavailable,
                _ => StatusCode.Internal,
            },
            error.Message,
            JsonSerializer.SerializeToUtf8Bytes(error, ErrorJsonOptions));
    }
}

/// <summary>
/// Holds the gRPC status and serialized Kawa error used by a MagicOnion adapter.
/// </summary>
/// <param name="StatusCode">The gRPC status code.</param>
/// <param name="Detail">The human-readable gRPC status detail.</param>
/// <param name="KawaErrorJson">The UTF-8 JSON payload written to the <c>kawa-error-bin</c> trailer.</param>
public sealed record KawaMagicOnionMappedError(StatusCode StatusCode, string Detail, byte[] KawaErrorJson);
