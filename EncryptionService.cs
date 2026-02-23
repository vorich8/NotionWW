using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TeamManagerBot.Services
{
    public interface IEncryptionService
    {
        byte[] Encrypt(string plainText);
        string Decrypt(byte[] cipherText);
        string EncryptToBase64(string plainText);
        string DecryptFromBase64(string base64CipherText);
        (byte[] iv, byte[] cipherText) EncryptWithIV(string plainText);
        string DecryptWithIV(byte[] iv, byte[] cipherText);
        string EncryptToBase64WithIV(string plainText);
        string DecryptFromBase64WithIV(string base64CipherTextWithIv);
        byte[] GenerateKey();
        byte[] GenerateIV();
    }

    public class EncryptionService : IEncryptionService, IDisposable
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;
        private readonly Aes _aes;
        private readonly ILogger<EncryptionService> _logger;

        public EncryptionService(
            IConfiguration configuration,
            ILogger<EncryptionService> logger)
        {
            _logger = logger;

            var encryptionConfig = configuration.GetSection("Encryption");

            // Получаем ключ из конфигурации
            var keyBase64 = encryptionConfig["Key"];
            var ivBase64 = encryptionConfig["Iv"];

            if (string.IsNullOrEmpty(keyBase64) || string.IsNullOrEmpty(ivBase64))
            {
                _logger.LogWarning("Encryption keys are not configured. Generating temporary keys.");

                // Генерируем временные ключи
                using var tempAes = Aes.Create();
                tempAes.GenerateKey();
                tempAes.GenerateIV();

                _key = tempAes.Key;
                _iv = tempAes.IV;

                _logger.LogWarning("Temporary encryption keys generated. Data encrypted with these keys will be lost after restart!");
            }
            else
            {
                try
                {
                    _key = Convert.FromBase64String(keyBase64);
                    _iv = Convert.FromBase64String(ivBase64);
                }
                catch (FormatException ex)
                {
                    _logger.LogError(ex, "Invalid base64 format for encryption keys");
                    throw new InvalidOperationException("Invalid base64 format for encryption keys", ex);
                }
            }

            // Проверяем длину ключей
            if (_key.Length != 32) // 256 bits
                throw new InvalidOperationException($"Key must be 32 bytes (256 bits), got {_key.Length} bytes");

            if (_iv.Length != 16) // 128 bits
                throw new InvalidOperationException($"IV must be 16 bytes (128 bits), got {_iv.Length} bytes");

            _aes = Aes.Create();
            _aes.Key = _key;
            _aes.IV = _iv;
            _aes.Mode = CipherMode.CBC;
            _aes.Padding = PaddingMode.PKCS7;

            _logger.LogInformation("Encryption service initialized");
        }

        public byte[] Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return Array.Empty<byte>();

            try
            {
                using var encryptor = _aes.CreateEncryptor(_aes.Key, _aes.IV);
                using var ms = new MemoryStream();
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (var sw = new StreamWriter(cs, Encoding.UTF8))
                {
                    sw.Write(plainText);
                }

                return ms.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error encrypting text");
                throw;
            }
        }

        public string Decrypt(byte[] cipherText)
        {
            if (cipherText == null || cipherText.Length == 0)
                return string.Empty;

            try
            {
                using var decryptor = _aes.CreateDecryptor(_aes.Key, _aes.IV);
                using var ms = new MemoryStream(cipherText);
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var sr = new StreamReader(cs, Encoding.UTF8);

                return sr.ReadToEnd();
            }
            catch (CryptographicException ex)
            {
                _logger.LogError(ex, "Error decrypting data - invalid key or corrupted data");
                throw new InvalidOperationException("Decryption failed. Check encryption keys.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decrypting text");
                throw;
            }
        }

        public string EncryptToBase64(string plainText)
        {
            var encryptedBytes = Encrypt(plainText);
            return Convert.ToBase64String(encryptedBytes);
        }

        public string DecryptFromBase64(string base64CipherText)
        {
            if (string.IsNullOrEmpty(base64CipherText))
                return string.Empty;

            try
            {
                var cipherText = Convert.FromBase64String(base64CipherText);
                return Decrypt(cipherText);
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "Invalid base64 format");
                throw new ArgumentException("Invalid base64 format", ex);
            }
        }

        public (byte[] iv, byte[] cipherText) EncryptWithIV(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return (Array.Empty<byte>(), Array.Empty<byte>());

            try
            {
                using var aes = Aes.Create();
                aes.Key = _key;
                aes.GenerateIV();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using var ms = new MemoryStream();
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (var sw = new StreamWriter(cs, Encoding.UTF8))
                {
                    sw.Write(plainText);
                }

                return (aes.IV, ms.ToArray());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error encrypting text with IV");
                throw;
            }
        }

        public string DecryptWithIV(byte[] iv, byte[] cipherText)
        {
            if (cipherText == null || cipherText.Length == 0 || iv == null || iv.Length == 0)
                return string.Empty;

            try
            {
                using var aes = Aes.Create();
                aes.Key = _key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using var ms = new MemoryStream(cipherText);
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var sr = new StreamReader(cs, Encoding.UTF8);

                return sr.ReadToEnd();
            }
            catch (CryptographicException ex)
            {
                _logger.LogError(ex, "Error decrypting data with IV");
                throw new InvalidOperationException("Decryption failed.", ex);
            }
        }

        public string EncryptToBase64WithIV(string plainText)
        {
            var (iv, cipherText) = EncryptWithIV(plainText);
            // Формат: IV:CipherText (оба в base64)
            return $"{Convert.ToBase64String(iv)}:{Convert.ToBase64String(cipherText)}";
        }

        public string DecryptFromBase64WithIV(string base64CipherTextWithIv)
        {
            if (string.IsNullOrEmpty(base64CipherTextWithIv))
                return string.Empty;

            try
            {
                var parts = base64CipherTextWithIv.Split(':');
                if (parts.Length != 2)
                    throw new ArgumentException("Invalid encrypted format");

                var iv = Convert.FromBase64String(parts[0]);
                var cipherText = Convert.FromBase64String(parts[1]);

                return DecryptWithIV(iv, cipherText);
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "Invalid base64 format in encrypted string");
                throw;
            }
        }

        public byte[] GenerateKey()
        {
            using var aes = Aes.Create();
            aes.GenerateKey();
            return aes.Key;
        }

        public byte[] GenerateIV()
        {
            using var aes = Aes.Create();
            aes.GenerateIV();
            return aes.IV;
        }

        public void Dispose()
        {
            _aes?.Dispose();

            // Очищаем ключи из памяти
            if (_key != null)
            {
                Array.Clear(_key, 0, _key.Length);
                _logger.LogDebug("Encryption key cleared from memory");
            }

            if (_iv != null)
            {
                Array.Clear(_iv, 0, _iv.Length);
                _logger.LogDebug("IV cleared from memory");
            }
        }
    }
}