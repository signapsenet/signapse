using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.IdentityModel.Tokens;
using Signapse.Data;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;


namespace Signapse.Services
{
    public struct CryptoKeys
    {
        public byte[] AESKey { get; set; }
    }

    public class Cryptography
    {
        const string CRYPTO_KEY = "@(Super_Secret_Key$)";

        readonly CryptoKeys cryptoKeys;

        public Cryptography(ISecureStorage secureStorage, JsonSerializerFactory jsonFactory)
        {
            var json = secureStorage.ReadFile("encryption_keys.json").Result;
            if (jsonFactory.Deserialize<CryptoKeys>(json) is CryptoKeys keys
                && keys.AESKey?.Length > 0)
            {
                cryptoKeys = keys;
            }
            else
            {
                // Generate a unique key for this site
                using RSACryptoServiceProvider RSA = new RSACryptoServiceProvider();
                using var md5 = MD5.Create();
             
                var data = RSA.Encrypt(Encoding.UTF8.GetBytes(CRYPTO_KEY), RSAEncryptionPadding.Pkcs1);
                cryptoKeys = new CryptoKeys()
                {
                    AESKey = md5.ComputeHash(data)
                };

                secureStorage.WriteFile("encryption_keys.json", jsonFactory.Serialize(cryptoKeys));
            }
        }

        public string Encrypt(string data)
        {
            return Convert.ToBase64String(Encrypt(Encoding.UTF8.GetBytes(data)));
        }

        public byte[] Encrypt<T>(T data)
        {
            return Encrypt(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(data));
        }

        public byte[] Encrypt(byte[] data)
        {
            using (Aes aes = Aes.Create())
            using (var encryptedStream = new MemoryStream())
            {
                byte[] iv = aes.IV;
                encryptedStream.Write(iv, 0, iv.Length);

                using (ICryptoTransform encryptor = aes.CreateEncryptor(cryptoKeys.AESKey, aes.IV))
                using (CryptoStream cryptoStream = new CryptoStream(encryptedStream, encryptor, CryptoStreamMode.Write))
                using (var originalByteStream = new MemoryStream(data))
                {
                    originalByteStream.CopyTo(cryptoStream);
                }

                return encryptedStream.ToArray();
            }
        }

        public string Decrypt(string base64Data)
        {
            return Encoding.UTF8.GetString(Decrypt(Convert.FromBase64String(base64Data)));
        }

        public T? Decrypt<T>(string data)
        {
            try
            {
                return Decrypt<T>(Convert.FromBase64String(data));
            }
            catch { return default(T); }
        }

        public T? Decrypt<T>(byte[] data)
        {
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<T>(Decrypt(data));
            }
            catch { return default(T); }
        }

        public byte[] Decrypt(byte[] data)
        {
            using (var encryptedStream = new MemoryStream(data))
            using (var decryptedStream = new MemoryStream())
            using (Aes aes = Aes.Create())
            {
                byte[] iv = new byte[aes.IV.Length];
                int numBytesToRead = aes.IV.Length;
                int numBytesRead = 0;
                while (numBytesToRead > 0)
                {
                    int n = encryptedStream.Read(iv, numBytesRead, numBytesToRead);
                    if (n == 0) break;

                    numBytesRead += n;
                    numBytesToRead -= n;
                }

                using (var decryptor = aes.CreateDecryptor(cryptoKeys.AESKey, iv))
                using (var cryptoStream = new CryptoStream(encryptedStream, decryptor, CryptoStreamMode.Read))
                {
                    cryptoStream.CopyTo(decryptedStream);
                }

                decryptedStream.Position = 0;
                return decryptedStream.ToArray();
            }
        }

        public string GetRandomAlphanumericString(int length)
        {
            const string CHARACTER_SET =
                "ABCDEFGHJKLMNPQRSTUVWXYZ" +
                "abcdefghjkmnpqrstuvwxyz" +
                "23456789" +
                "!@#$%*()";

            return GetRandomString(length, CHARACTER_SET);
        }

        public string GetRandomString(int length, IEnumerable<char> characterSet)
        {
            using var rng = RandomNumberGenerator.Create();

            if (length < 0)
                throw new ArgumentException("length must not be negative", "length");
            if (length > int.MaxValue / 8) // 250 million chars ought to be enough for anybody
                throw new ArgumentException("length is too big", "length");
            if (characterSet == null)
                throw new ArgumentNullException("characterSet");
            var characterArray = characterSet.Distinct().ToArray();
            if (characterArray.Length == 0)
                throw new ArgumentException("characterSet must not be empty", "characterSet");

            var bytes = new byte[length * 8];
            rng.GetBytes(bytes);

            var result = new char[length];
            for (int i = 0; i < length; i++)
            {
                ulong value = BitConverter.ToUInt64(bytes, i * 8);
                result[i] = characterArray[value % (uint)characterArray.Length];
            }
            return new string(result);
        }
    }
}
