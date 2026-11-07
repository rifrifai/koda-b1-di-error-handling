using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Files;
public interface IProductImageStorage
{
	Task<StoredProductImage> SaveAsync(string productCode, IFormFile file, CancellationToken ct);
}