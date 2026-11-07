using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Files;
public record class StoredProductImage(
    string StoredFileName,
    long SizeInBytes,
    string ContentType
);