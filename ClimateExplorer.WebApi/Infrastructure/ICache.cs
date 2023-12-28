using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ClimateExplorer.WebApi.Infrastructure;

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
/// This cache implementation is file-backed (as is FileBackedCache), but adds some complexity so that it can support longer keys.
/// Because FileBackedCache stored the value for a given key in a file whose name was a Base64 encoding of the key, long keys would
/// result in longer file names, which eventually bumps up against Windows path length limitations.
/// 
/// To get around that problem, this implementation introduces an intermediate file layer that stores a mapping from hashes of cache
/// keys to the list of actual underlying files storing cache entries whose keys hash to that value.
/// 
/// Because string.GetHashCode() gives different values across process instances, we use a deterministic hashing function.
/// 
///     /[cacheFolderName]/hash_[hashcode].json files
///         The hashcode in the filename is a deterministic hash of the cache key. Each of these files contains one to many entries:
///         one entry for every cache key (there may be multiple, but in practice this is rare) which hashes to the same 32-bit
///         hashcode. Each of those entries maps from that cache key to a randomly assigned guid. The guid can be used to locate the
///         file containing the cached value for that cache key.
///     /[cacheFolderName]/[guid].json files
///         These files contain the actual cached content.
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

    async Task<Dictionary<string, string>> ReadKeyToFileMappingForHashcode(int hashCode)
    {
        try
        {
            var filename = GetFilenameForKeyToFileMappingForHashcode(hashCode);

            if (File.Exists(filename))
            {
                var indexJson = await File.ReadAllTextAsync(filename);

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

    async Task WriteKeyToFileMappingForHashcode(int hashCode, Dictionary<string, string> keyToFileMapping)
    {
        try
        {
            var filename = GetFilenameForKeyToFileMappingForHashcode(hashCode);

            await File.WriteAllTextAsync(
                filename, 
                JsonSerializer.Serialize(
                    keyToFileMapping,
                    new JsonSerializerOptions()
                    {
                        WriteIndented = true,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
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
            var mapping = await ReadKeyToFileMappingForHashcode(keyHash);

            if (mapping.TryGetValue(key, out var filename))
            {
                var content = await File.ReadAllTextAsync(Path.Combine(_cacheFolderName, filename));

                return JsonSerializer.Deserialize<T>(content);
            }
        }
        catch (Exception ex)
        {
            // TODO: Organised logging for web tier.
            Console.WriteLine(ex.ToString());
        }

        return default;
    }

    public async Task Put<T>(string key, T obj)
    {
        try
        {
            EnsureCacheDirectoryExists();

            var keyHash = GetDeterministicHashCode(key);

            // Check if the cache index contains the key
            var mapping = await ReadKeyToFileMappingForHashcode(keyHash);

            Guid id = Guid.NewGuid();
            string filename = Guid.NewGuid().ToString() + ".json";

            mapping[key] = filename;

            await File.WriteAllTextAsync(
                Path.Combine(_cacheFolderName, filename), 
                JsonSerializer.Serialize(
                    obj, 
                    new JsonSerializerOptions()
                    {
                        WriteIndented = true,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    }
                )
            );

            await WriteKeyToFileMappingForHashcode(keyHash, mapping);
        }
        catch (Exception ex)
        {
            // TODO: Organised logging for web tier.
            Console.WriteLine(ex.ToString());
        }
    }
}

