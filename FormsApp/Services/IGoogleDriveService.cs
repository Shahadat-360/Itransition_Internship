using System.Threading.Tasks;

namespace FormsApp.Services
{
    public interface IGoogleDriveService
    {
        Task<string> UploadJsonFileAsync(string jsonContent, string fileName);
    }
} 