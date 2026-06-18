using Matrix_mizrachi.Models;
using Matrix_mizrachi.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Matrix_mizrachi.Tests.Integration;

public class MathApiFactory : WebApplicationFactory<Program>
{
    public Mock<IMockoonService> MockoonMock { get; } = new();
    public Mock<IKafkaProducerService> KafkaMock { get; } = new();

    public MathApiFactory()
    {
        KafkaMock
            .Setup(k => k.PublishAsync(It.IsAny<MathOperationEvent>()))
            .Returns(Task.CompletedTask);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace IMockoonService (registered via AddHttpClient) with the mock
            var mockoonDescriptors = services
                .Where(d => d.ServiceType == typeof(IMockoonService))
                .ToList();
            foreach (var d in mockoonDescriptors)
                services.Remove(d);
            services.AddSingleton<IMockoonService>(MockoonMock.Object);

            // Replace IKafkaProducerService with the mock
            var kafkaDescriptors = services
                .Where(d => d.ServiceType == typeof(IKafkaProducerService))
                .ToList();
            foreach (var d in kafkaDescriptors)
                services.Remove(d);
            services.AddSingleton<IKafkaProducerService>(KafkaMock.Object);
        });
    }
}
