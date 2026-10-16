using CarPrices.Vision.Barcode;
using CarPrices.Vision.Ocr;
using Microsoft.Extensions.DependencyInjection;

namespace CarPrices.Vision;

public static class DependencyInjection
{
    public static IServiceCollection AddVision(this IServiceCollection services)
    {
        services.AddSingleton<IBarcodeReader, BarcodeVinReader>();
        services.AddSingleton<IOcrEngine, NullOcrEngine>();
        services.AddScoped<VinImageExtractor>();
        services.AddScoped<VinImageRecoveryService>();
        return services;
    }
}
