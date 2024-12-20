using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;

//https://docs.sixlabors.com/articles/imagesharp/resize.html
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace BlobManipulation
{
    class Program
    {
        private const string connectionString = "DefaultEndpointsProtocol=https;AccountName=devoirguro;AccountKey=bv7/NlPf+62JXVtlYD1ncpcPVv8//Dx/ug3wX0dDqcwCs/zH9hfBoFcCOkSv6w6dVFRa4cVSnWlK+AStwl1Nlg==;EndpointSuffix=core.windows.net";
        private const string containerName = "initial";
        private const string containerName2 = "final";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Start : " + DateTime.Now.ToString());

            //Create container 1 
            BlobServiceClient serviceClient = new BlobServiceClient(connectionString);
            BlobContainerClient blobClient1 = serviceClient.GetBlobContainerClient(containerName);

            if (!blobClient1.Exists())
            {
                blobClient1 = await serviceClient.CreateBlobContainerAsync(containerName);

                if (await blobClient1.ExistsAsync())
                {
                    Console.WriteLine("Created container {0}", blobClient1.Name);
                }
            }
            else
            {
                Console.WriteLine("Container {0} allready exist", blobClient1.Name);
            }

            //Create container 2
            BlobContainerClient blobClient2 = serviceClient.GetBlobContainerClient(containerName2);

            if (!blobClient2.Exists())
            {
                blobClient2 = await serviceClient.CreateBlobContainerAsync(containerName2);

                if (await blobClient2.ExistsAsync())
                {
                    Console.WriteLine("Created container {0}", blobClient2.Name);
                }
            }
            else
            {
                Console.WriteLine("Container {0} allready exist", blobClient2.Name);
            }


            //File should be absent
            string path = @"C:\Users\gui44\OneDrive\Bureau\Meme\freebsd-devil.jpg";
            string filename = Path.GetFileName(path);

            //Upload a file to a Blob
            Console.WriteLine("File doesn't exist, uploading file ...");

            using (FileStream fileStream = File.OpenRead(path))
            {
                await blobClient1.UploadBlobAsync(filename, fileStream);
            }

            Console.WriteLine(("File uploaded : {0}", filename));


            //Download a file from Blob
            string newPath = @"C:\Users\gui44\OneDrive\Bureau\Meme\fromBlob-devil.jpg";
            string newFilename = Path.GetFileName(newPath);

            var blob = serviceClient.GetBlobContainerClient(containerName).GetBlockBlobClient(filename);

            using (FileStream fileStream = File.OpenWrite(newPath))
            {
                await blob.DownloadToAsync(fileStream);
            }


            //Modify file
            string modifyPath = @"C:\Users\gui44\OneDrive\Bureau\Meme\modify.jpg";

            using (Image image = Image.Load(newPath))
            {
                int width = image.Width / 2;
                int height = image.Height / 2;
                image.Mutate(x => x.Resize(width, height));

                image.Save(modifyPath);
            }

            //Upload file to Blob
            //In here I'm using the second bloc client that is connected on the "final" blob
            //I'm also using the modifyPath and the original filename.
            using (FileStream fileStream = File.OpenRead(modifyPath))
            { 
                await blobClient2.UploadBlobAsync(filename,fileStream);
            }

            //Delete file from Original Blob
            await blob.DeleteAsync();
        }
    }

}
