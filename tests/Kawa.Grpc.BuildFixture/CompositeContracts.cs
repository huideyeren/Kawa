using Kawa.Abstractions;
using Kawa.Grpc;
using Envelope = Kawa.Grpc.BuildFixture.Models.Envelope;

namespace Kawa.Grpc.BuildFixture
{
    [KawaProtoService("Composite")]
    public sealed class RoundTripUseCase : IUseCase<Envelope, Envelope>
    {
        public Task<KawaResult<Envelope>> ExecuteAsync(Envelope request, CancellationToken cancellationToken = default)
            => Task.FromResult(KawaResult<Envelope>.Success(request));
    }
}

namespace Kawa.Grpc.BuildFixture.Models
{
    public enum Delivery { Pending = 0, Sent = 3 }
    public sealed class Envelope
    {
        [KawaProtoField(1)] public string Message { get; init; } = "literal } not a class delimiter";
        [KawaProtoField(2)] public byte[] Payload { get; init; } = [];
        [KawaProtoField(3)] public Delivery State { get; init; }
        [KawaProtoField(4)] public Detail? Child { get; init; }
        [KawaProtoField(5)] public List<Detail> Items { get; init; } = [];
        [KawaProtoField(6)] public int[] Scores { get; init; } = [];
        [KawaProtoField(7)] public List<Delivery> States { get; init; } = [];
        public sealed class Detail
        {
            [KawaProtoField(1)] public string Name { get; init; } = "";
        }
    }
    // Unrelated unannotated types must never become schema messages.
    public sealed class Settings { public string Name { get; init; } = ""; }
}
