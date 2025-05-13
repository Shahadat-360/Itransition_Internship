using System.Threading.Tasks;

namespace FormsApp.Services
{
    public interface IOneDriveService
    {
        Task<string> UploadJsonFileAsync(string jsonContent, string fileName);
    }
} 