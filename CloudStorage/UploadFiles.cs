using CloudStorage.FileProcessing;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Data;
using System.Linq;

namespace CloudStorage
{
    public static class UploadFiles
    {
		public static IConfigurationRoot Configuration { get; set; }
		public static async Task UploadAsync(IConfiguration _configuration, string containername)
		{
			string account = _configuration.GetSection("ConnectionString").GetSection("StorageAccount").Value;
			string key = _configuration.GetSection("ConnectionString").GetSection("AccountKey").Value;

			
			CloudStorageAccount storageAccount = CloudStorageAccount.Parse(account);

			CloudBlobClient myBlobClient = storageAccount.CreateCloudBlobClient();
			myBlobClient.DefaultRequestOptions.SingleBlobUploadThresholdInBytes = 1024 * 1024;
			CloudBlobContainer container = myBlobClient.GetContainerReference(containername);

            #region use if you need impersonated user to access shared location
            //string batchServerLogin = _configuration.GetSection("SharedCredentialSettings").GetSection("BatchServerLogin").Value;
            //string batchServerDomain = _configuration.GetSection("SharedCredentialSettings").GetSection("BatchServerDomain").Value;
            //string batchServerLoginPwd = _configuration.GetSection("SharedCredentialSettings").GetSection("BatchServerLoginPwd").Value;

            //UserImpersonation impersonator = new UserImpersonation();

            //if (impersonator.ImpersonateUser(batchServerLogin, batchServerDomain, batchServerLoginPwd))
            //{
            //	Console.WriteLine("login successful for shared folder");
            //}

            #endregion

            Task<IEnumerable<string>> task2 = Task<IEnumerable<string>>.Factory.StartNew(() =>
			{
				return Directory.EnumerateFiles(@"\\server3\dropbox\Azure\files\Xml");
			});
			await Task.Yield();
			IEnumerable<string> files = task2.Result;
			int count = 0;
			foreach (var file in files)
			{
				count = count++;
				CloudBlockBlob myBlob = container.GetBlockBlobReference(Path.GetFileName(file));
				if (await myBlob.ExistsAsync())
				{
					Console.WriteLine(myBlob.Name + " already exist.");
					goto final;
				}
				var blockSize = 256 * 1024;
				myBlob.StreamWriteSizeInBytes = blockSize;
				var fileName = file;
				long bytesToUpload = (new FileInfo(fileName)).Length;
				long fileSize = bytesToUpload;

				if (bytesToUpload < blockSize)
				{
					var ado = myBlob.UploadFromFileAsync(fileName);
					Console.WriteLine(ado.Status);
					await ado.ContinueWith(t =>
					{
						Console.WriteLine("Status = " + t.Status);
						Console.WriteLine("It is over"); //this is working OK
				});
				}
				else
				{
					List<string> blockIds = new List<string>();
					int index = 1;
					long startPosition = 0;
					long bytesUploaded = 0;
					do
					{
						var bytesToRead = Math.Min(blockSize, bytesToUpload);
						var blobContents = new byte[bytesToRead];
						using (FileStream fs = new FileStream(fileName, FileMode.Open))
						{
							fs.Position = startPosition;
							fs.Read(blobContents, 0, (int)bytesToRead);
						}
						ManualResetEvent mre = new ManualResetEvent(false);
						var blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes(index.ToString("d6")));
						Console.WriteLine("Now uploading block # " + index.ToString("d6"));
						blockIds.Add(blockId);
						var ado = myBlob.PutBlockAsync(blockId, new MemoryStream(blobContents), null);
						await ado.ContinueWith(t =>
						{
							bytesUploaded += bytesToRead;
							bytesToUpload -= bytesToRead;
							startPosition += bytesToRead;
							index++;
							double percentComplete = (double)bytesUploaded / (double)fileSize;
							Console.WriteLine("Percent complete = " + percentComplete.ToString("P"));
							mre.Set();
						});
						mre.WaitOne();
					}
					while (bytesToUpload > 0);
					Console.WriteLine("Now committing block list");
					var pbl = myBlob.PutBlockListAsync(blockIds);
					
					await pbl.ContinueWith(t =>
					{
						Console.WriteLine("Blob uploaded completely.");
					});

					 
				}

				final:
				Console.WriteLine();
			}
			Console.WriteLine("Total files uploaded:{0}", count);
			Console.ReadKey();
		}
	}
}
