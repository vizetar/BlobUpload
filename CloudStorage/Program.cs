using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace CloudStorage
{
    class Program
    {
        public static void Main(string[] args)
        {
			var builder = new ConfigurationBuilder()
			.SetBasePath(Directory.GetCurrentDirectory())
			.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
			.AddEnvironmentVariables();

			IConfigurationRoot configuration = builder.Build();
			Console.WriteLine("Enter Container name: ");
			var container = Console.ReadLine();
			UploadFiles.UploadAsync(configuration, container).Wait();
			Console.ReadLine();
		}
    }
}
