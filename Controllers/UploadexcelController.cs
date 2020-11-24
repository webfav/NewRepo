using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UploadExcelfileApp.Models;

namespace UploadExcelfileApp.Controllers
{
    public class UploadexcelController : Controller
    {
        private readonly IConfiguration _configuration;
        public UploadexcelController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string Index()
        {
            return "This is my index action...";
        }


        [HttpGet]
        public IActionResult Create()
        {
            return View();
            
        }
        
        [HttpPost]
        public async Task<IActionResult> Create(IFormFile files)
        {
            string systemFileName = files.FileName;
            if (systemFileName == null)
            {
                return BadRequest();
            }

            string blobstorageconnection = _configuration.GetValue<string>("AccessKey");
            
            // Retrieve storage account from connection string.
            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(blobstorageconnection);
            
            // Create the blob client.
            CloudBlobClient blobClient = cloudStorageAccount.CreateCloudBlobClient();
            
            // Retrieve a reference to a container.
            CloudBlobContainer container = blobClient.GetContainerReference("excelimports");

            
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(systemFileName);
            await using (var data = files.OpenReadStream())
            {
                await blockBlob.UploadFromStreamAsync(data);
            }
            return View("Create");
        }
        
        [HttpGet]
        public async Task<IActionResult> ListBlobs()
        {
            string blobstorageconnection = _configuration.GetValue<string>("AccessKey");
            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(blobstorageconnection);
            // Create the blob client.
            CloudBlobClient blobClient = cloudStorageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("excelimports");
            CloudBlobDirectory dirb = container.GetDirectoryReference("excelimports");


            BlobResultSegment result = await container.ListBlobsSegmentedAsync(string.Empty,
                true, BlobListingDetails.Metadata, 100, null, null, null);

            List<FileData> fileList = new List<FileData>();

            foreach (var blobItem in result.Results)
            {
                
                var blob = (CloudBlob)blobItem;
                fileList.Add(new FileData()
                {
                    FileName = blob.Name,
                    FileSize = Math.Round((blob.Properties.Length / 1024f) / 1024f, 2).ToString(),
                    ModifiedOn = DateTime.Parse(blob.Properties.LastModified.ToString()).ToLocalTime().ToString()
                });
            }

            return View(fileList);
        }

        [HttpGet]
        public async Task<IActionResult> Download(string blobName)
        {
            CloudBlockBlob blockBlob;
            await using (MemoryStream memoryStream = new MemoryStream())
            {
                string blobstorageconnection = _configuration.GetValue<string>("AccessKey");
                CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(blobstorageconnection);
                CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
                CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference("excelimports");
                blockBlob = cloudBlobContainer.GetBlockBlobReference(blobName);
                await blockBlob.DownloadToStreamAsync(memoryStream);
            }

            Stream blobStream = blockBlob.OpenReadAsync().Result;
            return File(blobStream, blockBlob.Properties.ContentType, blockBlob.Name);
        }
        [HttpGet]
        public async Task<IActionResult> Delete(string blobName)
        {
            string blobstorageconnection = _configuration.GetValue<string>("AccessKey");
            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(blobstorageconnection);
            CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
            string strContainerName = "excelimports";
            CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference(strContainerName);
            var blob = cloudBlobContainer.GetBlobReference(blobName);
            await blob.DeleteIfExistsAsync();
            return RedirectToAction("ListBlobs", "Uploadexcel");
        }
    }
}
