using Kawa.Aspire;
using Microsoft.Extensions.Hosting;

namespace KawaStarter.ServiceDefaults;

public static class Extensions
{
    public static TBuilder AddSampleDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddKawaAspireDefaults(builder.Configuration);
        return builder;
    }
}
