using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AcornSat.WebApi.Infrastructure
{
    public interface ICache
    {
        Task<T> Get<T>(string key);
        Task Put<T>(string key, T obj);
    }

    public class FileBackedCache : ICache
    {
        readonly string _cacheFolderName;

        public FileBackedCache(string cacheFolderName)
        {
            _cacheFolderName = cacheFolderName;
        }

        string GenerateCacheFileName(string cacheEntryKey)
        {
            return Path.Combine(_cacheFolderName, Convert.ToBase64String(Encoding.UTF8.GetBytes(cacheEntryKey)) + ".json");
        }

        void EnsureCacheDirectoryExists()
        {
            var cacheDirectory = new DirectoryInfo(_cacheFolderName);

            if (!cacheDirectory.Exists)
            {
                cacheDirectory.Create();
            }
        }

        public async Task<T> Get<T>(string key)
        {
            var filePath = GenerateCacheFileName(key);

            if (File.Exists(filePath))
            {
                var file = await File.ReadAllTextAsync(filePath);
                var obj = JsonSerializer.Deserialize<T>(file);

                return obj;
            }

            return default(T);
        }

        public async Task Put<T>(string key, T obj)
        {
            var filePath = GenerateCacheFileName(key);

            var json = JsonSerializer.Serialize(obj);

            await File.WriteAllTextAsync(filePath, json);
        }
    }
}
