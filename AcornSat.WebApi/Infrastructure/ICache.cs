using System;
using System.Collections.Generic;
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

    /// <summary>
    /// This is a little more complex than FileBackedCache - to support long cache keys (long enough that they exceed Windows
    /// path length limitations), there's an intermediate file layer that stores a mapping from hashes of cache keys to the list
    /// of actual underlying files storing cache entries whose keys hash to that value.
    /// </summary>
    public class FileBackedTwoLayerCache : ICache
    {
        readonly string _cacheFolderName;

        public FileBackedTwoLayerCache(string cacheFolderName)
        {
            _cacheFolderName = cacheFolderName;
        }

        void EnsureCacheDirectoryExists()
        {
            var cacheDirectory = new DirectoryInfo(_cacheFolderName);

            if (!cacheDirectory.Exists)
            {
                cacheDirectory.Create();
            }
        }

        string GetFilenameForKeyToFileMappingForHashcode(int hashCode)
        {
            return Path.Combine(_cacheFolderName, $"hash_{hashCode}.json");
        }

        Dictionary<string, string> ReadKeyToFileMappingForHashcode(int hashCode)
        {
            try
            {
                var filename = GetFilenameForKeyToFileMappingForHashcode(hashCode);

                if (File.Exists(filename))
                {
                    var indexJson = File.ReadAllText(filename);

                    var mapping = JsonSerializer.Deserialize<Dictionary<string, string>>(indexJson);

                    return mapping;
                }
            }
            catch (Exception ex)
            {
                // Log & fall back to recreating the index
                Console.WriteLine(ex.ToString());
            }

            return new Dictionary<string, string>();
        }

        void WriteKeyToFileMappingForHashcode(int hashCode, Dictionary<string, string> keyToFileMapping)
        {
            try
            {
                var filename = GetFilenameForKeyToFileMappingForHashcode(hashCode);

                File.WriteAllText(
                    filename, 
                    JsonSerializer.Serialize(
                        keyToFileMapping,
                        new JsonSerializerOptions()
                        {
                            WriteIndented = true
                        }
                    )
                );
            }
            catch (Exception ex)
            {
                // Log
                Console.WriteLine(ex.ToString());
            }
        }

        // Ref: https://andrewlock.net/why-is-string-gethashcode-different-each-time-i-run-my-program-in-net-core/
        static int GetDeterministicHashCode(string str)
        {
            unchecked
            {
                int hash1 = (5381 << 16) + 5381;
                int hash2 = hash1;

                for (int i = 0; i < str.Length; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ str[i];
                    if (i == str.Length - 1)
                        break;
                    hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
                }

                return hash1 + (hash2 * 1566083941);
            }
        }

        public async Task<T> Get<T>(string key)
        {
            try
            {
                EnsureCacheDirectoryExists();

                var keyHash = GetDeterministicHashCode(key);

                // Check if the cache index contains the key
                var mapping = ReadKeyToFileMappingForHashcode(keyHash);

                if (mapping.TryGetValue(key, out var filename))
                {
                    var content = File.ReadAllText(Path.Combine(_cacheFolderName, filename));

                    return JsonSerializer.Deserialize<T>(content);
                }
            }
            catch (Exception ex)
            {
                // TODO: Organised logging for web tier.
                Console.WriteLine(ex.ToString());
            }

            return default(T);
        }

        public async Task Put<T>(string key, T obj)
        {
            try
            {
                EnsureCacheDirectoryExists();

                var keyHash = GetDeterministicHashCode(key);

                // Check if the cache index contains the key
                var mapping = ReadKeyToFileMappingForHashcode(keyHash);

                Guid id = Guid.NewGuid();
                string filename = Guid.NewGuid().ToString() + ".json";

                mapping[key] = filename;

                File.WriteAllText(
                    Path.Combine(_cacheFolderName, filename), 
                    JsonSerializer.Serialize(
                        obj, 
                        new JsonSerializerOptions()
                        {
                            WriteIndented = true
                        }
                    )
                );

                WriteKeyToFileMappingForHashcode(keyHash, mapping);
            }
            catch (Exception ex)
            {
                // TODO: Organised logging for web tier.
                Console.WriteLine(ex.ToString());
            }
        }
    }

}
