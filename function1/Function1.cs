using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace QueueTriggerFunction
{
    public class Function1
    {
        private readonly ILogger<Function1> _logger;
        private const string ServiceBusConnectionString = "Endpoint=sb://devoirguro.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=TKKcqPGJw9JYIQNwikA4Ihv2qejQwxFvx+ASbKBLad4="; // Remplacez par votre chaîne de connexion Service Bus
        private const string QueueName = "messagequeue"; // Nom de la file d'attente

        public Function1(ILogger<Function1> logger)
        {
            _logger = logger;
        }

        [Function(nameof(Function1))]
        public async Task Run(
            [ServiceBusTrigger("%QueueName%", Connection = "ServiceBusConnectionString")] string message)
        {
            _logger.LogInformation($"Service Bus trigger function processed message: {message}");

            // Connection string et containers
            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            string sourceContainerName = Environment.GetEnvironmentVariable("ContainerName");
            string destinationContainerName = Environment.GetEnvironmentVariable("ContainerName2");

            try
            {
                BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
                BlobContainerClient sourceContainerClient = blobServiceClient.GetBlobContainerClient(sourceContainerName);
                BlobContainerClient destinationContainerClient = blobServiceClient.GetBlobContainerClient(destinationContainerName);

                BlobClient sourceBlobClient = sourceContainerClient.GetBlobClient(message);
                if (!await sourceBlobClient.ExistsAsync())
                {
                    _logger.LogError($"Blob '{message}' not found in source container '{sourceContainerName}'.");
                    return;
                }

                // Téléchargement de l'image localement
                MemoryStream memoryStream = new MemoryStream();
                await sourceBlobClient.DownloadToAsync(memoryStream);

                // Redimensionnement de l'image
                memoryStream.Position = 0;
                using (SixLabors.ImageSharp.Image image = SixLabors.ImageSharp.Image.Load(memoryStream))
                {
                    int newWidth = image.Width / 2;
                    int newHeight = image.Height / 2;

                    image.Mutate(x => x.Resize(newWidth, newHeight));

                    memoryStream = new MemoryStream(); // Réinitialisation du stream
                    image.Save(memoryStream, new JpegEncoder());
                }

                // Upload du fichier modifié vers le container final
                memoryStream.Position = 0;
                BlobClient destinationBlobClient = destinationContainerClient.GetBlobClient(message); // destination
                await destinationBlobClient.UploadAsync(memoryStream, overwrite: true);

                _logger.LogInformation($"File '{message}' successfully processed and uploaded to '{destinationContainerName}'.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing blob '{message}': {ex.Message}");
            }
        }

        public async Task SendMessageToQueueAsync(string fileName)
        {
            _logger.LogInformation($"Envoi du message '{fileName}' à la file d'attente Service Bus...");
            await using (ServiceBusClient client = new ServiceBusClient(ServiceBusConnectionString))
            {
                ServiceBusSender sender = client.CreateSender(QueueName);
                try
                {
                    ServiceBusMessage message = new ServiceBusMessage(fileName);
                    await sender.SendMessageAsync(message);
                    _logger.LogInformation($"Message '{fileName}' envoyé avec succès.");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Erreur lors de l'envoi du message : {ex.Message}");
                }
                finally
                {
                    await sender.DisposeAsync();
                }
            }
        }

        public async Task TriggerAndSendMessage()
        {
            string fileName = "corail.png";
            await SendMessageToQueueAsync(fileName);
        }
    }
}
