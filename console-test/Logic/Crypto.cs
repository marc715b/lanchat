using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Reflection.Metadata.Ecma335;
using System.Buffers.Binary;
using System.Numerics;

namespace console_test.Logic
{
  static class Crypto
  {
    internal class AES
    {
      private AesGcm _aes;

      private byte[] _key = new byte[32];

      public AES(byte[] key)
      {
        _key = key;

        _aes = new AesGcm(_key, AesGcm.TagByteSizes.MaxSize);
      }

      // Wrappers for string types
      public string Encrypt(string data)
      {
        return Convert.ToBase64String(Encrypt(Encoding.UTF8.GetBytes(data)));
      }

      public string Decrypt(string data)
      {
        return Encoding.UTF8.GetString(Decrypt(Convert.FromBase64String(data)));
      }

      // Crypto functions
      public byte[] Encrypt(byte[] bytes)
      {
        // Get parameter sizes
        int nonceSize = AesGcm.NonceByteSizes.MaxSize;
        int tagSize = AesGcm.TagByteSizes.MaxSize;

        int cipherSize = bytes.Length;

        // Calculate length of final output buffer
        int encryptedDataSize =
          4 + nonceSize +
          4 + tagSize +
          cipherSize;

        // Allocate a new span view of a byte buffer; if < 1024 bytes, on the
        // stack; otherwise, on the heap.
        Span<byte> encryptedData = encryptedDataSize < 1024
                                 ? stackalloc byte[encryptedDataSize]
                                 : new byte[encryptedDataSize];

        // Serialize the parameters
        BinaryPrimitives.WriteInt32LittleEndian(encryptedData.Slice(0, 4), nonceSize);
        BinaryPrimitives.WriteInt32LittleEndian(encryptedData.Slice(4 + nonceSize, 4), tagSize);

        // Get references to the parameters in-memory
        var nonce = encryptedData.Slice(4, nonceSize);
        var tag = encryptedData.Slice(4 + nonceSize + 4, tagSize);
        var cipher = encryptedData.Slice(4 + nonceSize + 4 + tagSize, cipherSize);

        // Generate a secure random nonce. Note that the nonce doesn't need
        // confidentiality, only that it's random for each block.
        RandomNumberGenerator.Fill(nonce);

        // Finally, encrypt the data.
        _aes.Encrypt(nonce, bytes.AsSpan(), cipher, tag);

        return encryptedData.ToArray();
      }

      public byte[] Decrypt(byte[] bytes)
      {
        // Create a view of the bytes
        Span<byte> encryptedData = bytes.AsSpan();

        // Extract parameter sizes
        int nonceSize = BinaryPrimitives.ReadInt32LittleEndian(encryptedData.Slice(0, 4));
        int tagSize = BinaryPrimitives.ReadInt32LittleEndian(encryptedData.Slice(4 + nonceSize, 4));
        int cipherSize = encryptedData.Length - 4 - nonceSize - 4 - tagSize;

        // Extract references to the parameters themselves
        var nonce = encryptedData.Slice(4, nonceSize);
        var tag = encryptedData.Slice(4 + nonceSize + 4, tagSize);
        var cipher = encryptedData.Slice(4 + nonceSize + 4 + tagSize, cipherSize);

        // Allocate a buffer for the plaintext; on the stack, if < 1024 bytes; on the heap otherwise
        Span<byte> plainBytes = cipherSize < 1024
                              ? stackalloc byte[cipherSize]
                              : new byte[cipherSize];

        // Finally, decrypt the data
        _aes.Decrypt(nonce, cipher, tag, plainBytes);

        return plainBytes.ToArray();
      }
    }

    public static class ECDH
    {
      // Quickly and securely generate an ECC keypair
      public static (byte[] pubKey, byte[] prvKey) GenerateKeypair()
      {
        using (ECDiffieHellmanCng ecdh = new ECDiffieHellmanCng())
        {
          // Configure the key derivation function
          ecdh.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
          ecdh.HashAlgorithm = CngAlgorithm.Sha256;

          return (ecdh.ExportECPrivateKey(), ecdh.PublicKey.ToByteArray());
        }
      }

      // Derive the shared secret between two keypairs
      public static byte[] DeriveKey(byte[] alicePubKey, byte[] alicePrvKey, byte[] bobPubKey)
      {
        // Create an ECDH CNG instance
        using (ECDiffieHellmanCng ecdh = new ECDiffieHellmanCng())
        {
          // Import Alice's private key
          ecdh.ImportECPrivateKey(alicePrvKey, out _);

          // Create a public key object from Bob's public key
          var bobPubKeyObj = ECDiffieHellmanCngPublicKey.FromByteArray(bobPubKey, CngKeyBlobFormat.EccPublicBlob);

          // Configure the key derivation function for the keying material
          ecdh.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
          ecdh.HashAlgorithm = CngAlgorithm.Sha256;

          // Derive the shared secret therefrom
          return ecdh.DeriveKeyMaterial(bobPubKeyObj);
        }
      }
    }
  }
}
