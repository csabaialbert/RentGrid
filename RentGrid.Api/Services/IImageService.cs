using System.IO;
using System.Threading.Tasks;

namespace RentGrid.Api.Services
{
    public interface IImageService
    {
        Task<string> UploadImageAsync(Stream stream, string fileName);
        Task<Stream> GetImageStreamAsync(string fileId);
        Task DeleteImageAsync(string fileId);
    }
}
