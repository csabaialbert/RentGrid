using System;
using System.IO;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace RentGrid.Api.Services
{
    public class GridFSService : IImageService
    {
        private readonly GridFSBucket _bucket;

        public GridFSService(IMongoDatabase database)
        {
            _bucket = new GridFSBucket(database);
        }

        public async Task<string> UploadImageAsync(Stream stream, string fileName)
        {
            ArgumentNullException.ThrowIfNull(stream);

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("Fájlnév megadása kötelező.", nameof(fileName));
            }

            try
            {
                var options = new GridFSUploadOptions
                {
                    Metadata = new BsonDocument
                    {
                        { "uploadedAt", DateTime.UtcNow },
                        { "contentType", "image" }
                    }
                };

                var fileId = await _bucket.UploadFromStreamAsync(fileName, stream, options);
                return fileId.ToString();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Hiba történt a kép feltöltésekor a GridFS-be.", ex);
            }
        }

        public async Task<Stream> GetImageStreamAsync(string fileId)
        {
            if (string.IsNullOrWhiteSpace(fileId))
            {
                throw new ArgumentException("FileId megadása kötelező.", nameof(fileId));
            }

            if (!ObjectId.TryParse(fileId, out var objectId))
            {
                throw new ArgumentException("A megadott FileId nem érvényes ObjectId formátum.", nameof(fileId));
            }

            try
            {
                var stream = new MemoryStream();
                await _bucket.DownloadToStreamAsync(objectId, stream);
                stream.Position = 0;
                return stream;
            }
            catch (GridFSFileNotFoundException ex)
            {
                throw new InvalidOperationException("A megadott FileId-hez nem található kép a GridFS-ben.", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Hiba történt a kép letöltésekor a GridFS-ből.", ex);
            }
        }

        public async Task DeleteImageAsync(string fileId)
        {
            if (string.IsNullOrWhiteSpace(fileId))
            {
                throw new ArgumentException("FileId megadása kötelező.", nameof(fileId));
            }

            if (!ObjectId.TryParse(fileId, out var objectId))
            {
                throw new ArgumentException("A megadott FileId nem érvényes ObjectId formátum.", nameof(fileId));
            }

            try
            {
                await _bucket.DeleteAsync(objectId);
            }
            catch (GridFSFileNotFoundException)
            {
                // Ha a fájl nem található, nincs mit törölni.
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Hiba történt a kép törlésekor a GridFS-ből.", ex);
            }
        }
    }
}
