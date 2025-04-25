using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Reflection.Metadata.Ecma335;
using System.Buffers.Binary;

namespace console_test.Logic
{
    internal class Crypt
    {
        /*  internal Crypt(byte[] Key) //here make each new instance have key specific to user, and also assign it to a user in some way
          {
              CryptKey = Key; 
          } */
        byte[] CryptKey = new byte[]
            {
             0x5b, 0xde, 0x43, 0x0b, 0xde, 0x39, 0x5a, 0x6b,
             0x73, 0x6f, 0x35, 0x08, 0x4b, 0x65, 0xbb, 0x57,
             0x8c, 0xf0, 0x2b, 0x36, 0xcb, 0x0a, 0x21, 0xd2,
             0x4a, 0x87, 0xd4, 0xd1, 0x05, 0x7e, 0x3b, 0xc9
            }; // make an actual from ecdh later

        public string Encrypter (string ClearTextMsg)
        {
           
            // Get bytes of plaintext string
            byte[] plainBytes = Encoding.UTF8.GetBytes(ClearTextMsg);

            // Get parameter sizes
            int nonceSize = AesGcm.NonceByteSizes.MaxSize;
            int TagSize = AesGcm.TagByteSizes.MaxSize;
            int cipherSize = plainBytes.Length;

            // Write everything into one big array for easier encoding
            int encryptedDataLength = 4 + nonceSize + 4 + TagSize + cipherSize;
            Span<byte> encryptedData = encryptedDataLength < 1024
                                     ? stackalloc byte[encryptedDataLength]
                                     : new byte[encryptedDataLength].AsSpan(); 

            // Copy parameters
            BinaryPrimitives.WriteInt32LittleEndian(encryptedData.Slice(0, 4), nonceSize);
            BinaryPrimitives.WriteInt32LittleEndian(encryptedData.Slice(4 + nonceSize, 4), TagSize);
            var nonce = encryptedData.Slice(4, nonceSize);
            var tag = encryptedData.Slice(4 + nonceSize + 4, TagSize); 
            var cipherBytes = encryptedData.Slice(4 + nonceSize + 4 + TagSize, cipherSize); 

            // Generate secure nonce
            RandomNumberGenerator.Fill(nonce);

            // Encrypt
            using var aes = new AesGcm(CryptKey, TagSize);
            aes.Encrypt(nonce, plainBytes.AsSpan(), cipherBytes, tag);

            // Encode for transmission
            return Convert.ToBase64String(encryptedData);
        }

        public string Decrypt(string cipher)
        {
            // Decode
            Span<byte> encryptedData = Convert.FromBase64String(cipher).AsSpan();

            // Extract parameter sizes
            int nonceSize = BinaryPrimitives.ReadInt32LittleEndian(encryptedData.Slice(0, 4));
            int tagSize = BinaryPrimitives.ReadInt32LittleEndian(encryptedData.Slice(4 + nonceSize, 4));
            int cipherSize = encryptedData.Length - 4 - nonceSize - 4 - tagSize;

            // Extract parameters
            var nonce = encryptedData.Slice(4, nonceSize);
            var tag = encryptedData.Slice(4 + nonceSize + 4, tagSize);
            var cipherBytes = encryptedData.Slice(4 + nonceSize + 4 + tagSize, cipherSize);

            // Decrypt
            Span<byte> plainBytes = cipherSize < 1024
                                  ? stackalloc byte[cipherSize]
                                  : new byte[cipherSize];
            using var aes = new AesGcm(CryptKey, tagSize);
            aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

            // Convert plain bytes back into string
            return Encoding.UTF8.GetString(plainBytes);
        }


    }
}
