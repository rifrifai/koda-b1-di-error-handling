using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Files;

public sealed class LocalProductImageStore(ILogger<LocalProductImageStore> logger, IWebHostEnvironment env) : IProductImageStorage
{
	private readonly string _storageRoot = Path.Combine(env.ContentRootPath, "KodaStore", "ProductImages");
	private readonly ILogger<LocalProductImageStore> _logger = logger;

	public async Task<StoredProductImage> SaveAsync(string productCode, IFormFile file, CancellationToken ct)
	{
		var safeCode = productCode.Trim().ToUpperInvariant();
		var validCode = safeCode.Length is >= 1 and <= 30 && safeCode.All(character => (character >= 'A' && character <= 'Z') || (character >= '0' && character <= '9') || character is '-' or '_');

		if (!validCode) throw new ArgumentException("ProductCode invalid", nameof(productCode));

		var productFolder = Path.Combine(_storageRoot, safeCode);

		Directory.CreateDirectory(productFolder);

		var storedFileName = $"{Guid.NewGuid():N}.jpg";

		var fullPath = Path.Combine(productFolder, storedFileName);

		Console.WriteLine($"Err {fullPath}");

		try
		{
			await using (var stream = new FileStream(
				fullPath, FileMode.CreateNew,
				FileAccess.Write,
				FileShare.None,
				bufferSize: 81928,
				useAsync: true))
			{
				await file.CopyToAsync(stream, ct);
			}
			var savedSize = new FileInfo(fullPath).Length;

			_logger.LogInformation("Product image saved. ProductCode={ProductCode}, FileName={FileName}", safeCode, storedFileName);

			return new StoredProductImage(storedFileName, savedSize, "image/jpg");
		}
		catch
		{
			if (File.Exists(fullPath))
			{
				try
				{
					File.Delete(fullPath);
				}
				catch (IOException ex)
				{
					_logger.LogWarning(ex, "Failed to clean up incomplete upload");
				}
			}
			throw;
		}
	}
}