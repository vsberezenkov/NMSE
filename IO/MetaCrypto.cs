using System.Security.Cryptography;

namespace NMSE.IO;

/// <summary>
/// TEA (Tiny Encryption Algorithm) cipher used by Steam/GOG NMS meta files (mf_*.hg).
/// TEA was designed by David Wheeler and Roger Needham of the Cambridge Computer Laboratory.
/// See: https://en.wikipedia.org/wiki/Tiny_Encryption_Algorithm
///
/// The meta file contains hashes (SpookyHash + SHA256) of the data file, plus save metadata
/// (game mode, season, play time, save name, etc.) encrypted with a slot-dependent TEA key.
/// </summary>
public static class MetaCrypto
{
    /// <summary>
    /// TEA key derived from the NMS key string "NAESEVADNAYRTNRG" interpreted
    /// as four little-endian uint32 values.
    /// Element [0] is replaced at runtime with the slot-dependent
    /// value computed by <see cref="DeriveKey0"/>.
    /// <para>
    /// </para>
    /// </summary>
    private static readonly uint[] MetaEncryptionKey =
    {
        // Yeah, the key is literally just the ASCII bytes of
        // SEAN, DAVE, RYAN, and GRANT (the NMS devs) packed
        // into uints in little-endian order lol.
        0x5345414E, // bytes "NAES" (overwritten per-slot)
        0x44415645, // bytes "EVAD"
        0x5259414E, // bytes "NAYR"
        0x47524E54, // bytes "TNRG"
    };

    /// <summary>
    /// Compute the slot-dependent key[0] value.
    /// Formula: <c>RotateLeft((storageSlot ^ 0x1422CB8C), 13) * 5 + 0xE6546B64</c>.
    /// </summary>
    private static uint DeriveKey0(int storageSlot)
    {
        return unchecked(RotateLeft(((uint)storageSlot ^ 0x1422CB8C), 13) * 5 + 0xE6546B64);
    }

    /// <summary>
    /// Encrypt meta file uint[] with XXTEA, slot-dependent key.
    /// <para>
    /// The meta buffer always has trailing zero bytes (padding), so the
    /// initial <c>prev = result[lastIndex]</c> evaluates to 0 for all
    /// real-world NMS meta files.
    /// </para>
    /// </summary>
    /// <param name="data">Plaintext meta as uint array.</param>
    /// <param name="storageSlot">Persistent storage slot index (0 for account, 2+ for saves).</param>
    /// <param name="iterations">6 for Waypoint+ (4.00+), 8 for pre-Waypoint (meta format 1).</param>
    /// <returns>Encrypted meta as uint array.</returns>
    public static uint[] Encrypt(uint[] data, int storageSlot, int iterations)
    {
        uint[] key = { DeriveKey0(storageSlot), MetaEncryptionKey[1], MetaEncryptionKey[2], MetaEncryptionKey[3] };
        uint[] result = (uint[])data.Clone();

        int lastIndex = result.Length - 1;
        uint prev = result[lastIndex];
        uint hash = 0;

        for (int i = 0; i < iterations; i++)
        {
            hash = unchecked(hash + 0x9E3779B9);
            int keyIndex = (int)((hash >> 2) & 3);

            for (int j = 0; j < lastIndex; j++)
            {
                uint next = result[j + 1];
                result[j] = unchecked(result[j] +
                    ((((next >> 3) ^ (prev << 4)) + ((next << 2) ^ (prev >> 5)))
                   ^ (((prev ^ key[(j & 3) ^ keyIndex]) + (next ^ hash)))));
                prev = result[j];
            }

            {
                uint next = result[0];
                result[lastIndex] = unchecked(result[lastIndex] +
                    ((((next >> 3) ^ (prev << 4)) + ((next << 2) ^ (prev >> 5)))
                   ^ (((prev ^ key[(lastIndex & 3) ^ keyIndex]) + (next ^ hash)))));
                prev = result[lastIndex];
            }
        }

        return result;
    }

    /// <summary>
    /// Decrypt meta file uint[] with TEA, trying the given slot first, then all others.
    /// Returns the first result whose first uint matches <see cref="MetaFileWriter.META_HEADER"/>.
    /// </summary>
    /// <param name="encrypted">Encrypted meta as uint array.</param>
    /// <param name="storageSlot">Primary slot to try first.</param>
    /// <param name="iterations">6 for Waypoint+ (4.00+), 8 for pre-Waypoint (meta format 1).</param>
    /// <returns>Decrypted meta as uint array, or the input unchanged if decryption fails.</returns>
    public static uint[] Decrypt(uint[] encrypted, int storageSlot, int iterations)
    {
        // Try the expected slot first
        var result = DecryptWithSlot(encrypted, storageSlot, iterations);
        if (result[0] == MetaFileWriter.META_HEADER)
            return result;

        // If that didn't work, try all other slots (file may have been moved manually)
        for (int slot = 0; slot <= 31; slot++)
        {
            if (slot == storageSlot) continue;
            result = DecryptWithSlot(encrypted, slot, iterations);
            if (result[0] == MetaFileWriter.META_HEADER)
                return result;
        }

        return encrypted; // return as-is if nothing worked
    }

    /// <summary>
    /// Decrypt with a specific slot.
    /// </summary>
    private static uint[] DecryptWithSlot(uint[] encrypted, int storageSlot, int iterations)
    {
        uint[] key = { DeriveKey0(storageSlot), MetaEncryptionKey[1], MetaEncryptionKey[2], MetaEncryptionKey[3] };
        uint[] result = (uint[])encrypted.Clone();

        int lastIndex = result.Length - 1;

        // Compute final hash (sum of all deltas)
        uint hash = 0;
        for (int i = 0; i < iterations; i++)
            hash = unchecked(hash + 0x9E3779B9);

        for (int i = 0; i < iterations; i++)
        {
            int keyIndex = (int)((hash >> 2) & 3);
            // In decrypt, "next" starts from result[0]
            uint next = result[0];

            for (int j = lastIndex; j > 0; j--)
            {
                uint prev = result[j - 1];
                result[j] = unchecked(result[j] -
                    ((((next >> 3) ^ (prev << 4)) + ((next << 2) ^ (prev >> 5)))
                   ^ (((prev ^ key[(j & 3) ^ keyIndex]) + (next ^ hash)))));
                next = result[j];
            }

            {
                uint prev = result[lastIndex];
                result[0] = unchecked(result[0] -
                    ((((next >> 3) ^ (prev << 4)) + ((next << 2) ^ (prev >> 5)))
                   ^ (((prev ^ key[keyIndex]) + (next ^ hash)))));
            }

            hash = unchecked(hash + 0x61C88647); // equivalent to hash -= 0x9E3779B9
        }

        return result;
    }

    /// <summary>
    /// Compute SpookyHash-based hash of the data payload for the meta file header.
    /// We implement a simplified SpookyHash version to save on dependencies.
    /// </summary>
    /// <param name="data">The compressed/encrypted data file bytes.</param>
    /// <returns>16 bytes: spookyHash1(8) + spookyHash2(8) followed by sha256(32) = 48 bytes total.</returns>
    public static byte[] ComputeMetaHashes(byte[] data)
    {
        byte[] sha256 = SHA256.HashData(data);

        // SpookyHash V2 with seeds 0x155AF93AC304200, 0x8AC7230489E7FFFF
        // Feed: sha256 then data
        var spooky = new SpookyHashV2(0x155AF93AC304200, unchecked((long)0x8AC7230489E7FFFF));
        spooky.Update(sha256);
        spooky.Update(data);
        spooky.Final(out ulong hash1, out ulong hash2);

        byte[] result = new byte[48];
        BitConverter.GetBytes(hash1).CopyTo(result, 0);
        BitConverter.GetBytes(hash2).CopyTo(result, 8);
        sha256.CopyTo(result, 16);
        return result;
    }

    private static uint RotateLeft(uint value, int count)
    {
        return (value << count) | (value >> (32 - count));
    }
}

/// <summary>
/// Minimal SpookyHash V2 implementation.
/// SpookyHash V2 by Bob Jenkins.
/// See: https://burtleburtle.net/bob/hash/spooky.html
/// </summary>
internal class SpookyHashV2
{
    private const int NumVars = 12;
    private const int BlockSize = NumVars * 8; // 96 bytes
    private const ulong Const = 0xDEADBEEFDEADBEEF;

    private ulong _h0, _h1, _h2, _h3, _h4, _h5, _h6, _h7, _h8, _h9, _h10, _h11;
    private readonly MemoryStream _buffer = new();
    private ulong _length;

    public SpookyHashV2(long seed1, long seed2)
    {
        _h0 = _h3 = _h6 = _h9 = (ulong)seed1;
        _h1 = _h4 = _h7 = _h10 = (ulong)seed2;
        _h2 = Const;
        _h5 = Const;
        _h8 = Const;
        _h11 = Const;
        _length = 0;
    }

    /// <summary>Appends data to the internal buffer for hashing.</summary>
    public void Update(byte[] data)
    {
        _buffer.Write(data, 0, data.Length);
        _length += (ulong)data.Length;
    }

    /// <summary>Computes the final dual hash values from all buffered data.</summary>
    public void Final(out ulong hash1, out ulong hash2)
    {
        byte[] allData = _buffer.ToArray();

        if (_length < 192)
        {
            Short(allData, out hash1, out hash2);
            return;
        }

        int offset = 0;
        int remaining = allData.Length;

        // Process full blocks
        while (remaining >= BlockSize)
        {
            Mix(allData, offset);
            offset += BlockSize;
            remaining -= BlockSize;
        }

        // Final block (remainder)
        byte[] finalBlock = new byte[BlockSize];
        if (remaining > 0)
            Buffer.BlockCopy(allData, offset, finalBlock, 0, remaining);
        finalBlock[BlockSize - 1] = (byte)remaining;

        EndPartial(finalBlock);
        EndPartial(finalBlock);
        EndPartial(finalBlock);

        hash1 = _h0;
        hash2 = _h1;
    }

    private void Short(byte[] data, out ulong hash1, out ulong hash2)
    {
        ulong a = _h0, b = _h1, c = Const, d = Const;
        int len = data.Length;

        if (len >= 32)
        {
            int offset = 0;
            int remaining = len;

            while (remaining >= 32)
            {
                c += ReadLE64(data, offset);
                d += ReadLE64(data, offset + 8);
                ShortMix(ref a, ref b, ref c, ref d);
                a += ReadLE64(data, offset + 16);
                b += ReadLE64(data, offset + 24);
                offset += 32;
                remaining -= 32;
            }

            byte[] tail = new byte[32];
            if (remaining > 0)
                Buffer.BlockCopy(data, offset, tail, 0, remaining);
            d += ((ulong)len) << 56;

            // last partial block
            if (remaining >= 16)
            {
                c += ReadLE64(tail, 0);
                d += ReadLE64(tail, 8);
                ShortMix(ref a, ref b, ref c, ref d);
                c += ReadLE64(tail, 16);
                d += ReadLE64(tail, 24);
            }
            else
            {
                c += ReadLE64(tail, 0);
                d += ReadLE64(tail, 8);
            }
        }
        else if (len >= 16)
        {
            c += ReadLE64(data, 0);
            d += ReadLE64(data, 8);
            ShortMix(ref a, ref b, ref c, ref d);

            byte[] tail = new byte[32];
            if (len > 16)
                Buffer.BlockCopy(data, 16, tail, 0, len - 16);
            d += ((ulong)len) << 56;
            c += ReadLE64(tail, 0);
            d += ReadLE64(tail, 8);
        }
        else
        {
            byte[] tail = new byte[32];
            if (len > 0)
                Buffer.BlockCopy(data, 0, tail, 0, len);
            d += ((ulong)len) << 56;
            c += ReadLE64(tail, 0);
            d += ReadLE64(tail, 8);
        }

        ShortEnd(ref a, ref b, ref c, ref d);
        hash1 = a;
        hash2 = b;
    }

    private static void ShortMix(ref ulong h0, ref ulong h1, ref ulong h2, ref ulong h3)
    {
        h2 = RotL64(h2, 50); h2 += h3; h0 ^= h2;
        h3 = RotL64(h3, 52); h3 += h0; h1 ^= h3;
        h0 = RotL64(h0, 30); h0 += h1; h2 ^= h0;
        h1 = RotL64(h1, 41); h1 += h2; h3 ^= h1;
        h2 = RotL64(h2, 54); h2 += h3; h0 ^= h2;
        h3 = RotL64(h3, 48); h3 += h0; h1 ^= h3;
        h0 = RotL64(h0, 38); h0 += h1; h2 ^= h0;
        h1 = RotL64(h1, 37); h1 += h2; h3 ^= h1;
        h2 = RotL64(h2, 62); h2 += h3; h0 ^= h2;
        h3 = RotL64(h3, 34); h3 += h0; h1 ^= h3;
        h0 = RotL64(h0, 5);  h0 += h1; h2 ^= h0;
        h1 = RotL64(h1, 36); h1 += h2; h3 ^= h1;
    }

    private static void ShortEnd(ref ulong h0, ref ulong h1, ref ulong h2, ref ulong h3)
    {
        h3 ^= h2; h2 = RotL64(h2, 15); h3 += h2;
        h0 ^= h3; h3 = RotL64(h3, 52); h0 += h3;
        h1 ^= h0; h0 = RotL64(h0, 26); h1 += h0;
        h2 ^= h1; h1 = RotL64(h1, 51); h2 += h1;
        h3 ^= h2; h2 = RotL64(h2, 28); h3 += h2;
        h0 ^= h3; h3 = RotL64(h3, 9);  h0 += h3;
        h1 ^= h0; h0 = RotL64(h0, 47); h1 += h0;
        h2 ^= h1; h1 = RotL64(h1, 54); h2 += h1;
        h3 ^= h2; h2 = RotL64(h2, 32); h3 += h2;
        h0 ^= h3; h3 = RotL64(h3, 25); h0 += h3;
        h1 ^= h0; h0 = RotL64(h0, 63); h1 += h0;
    }

    private void Mix(byte[] data, int offset)
    {
        _h0 += ReadLE64(data, offset);
        _h2 ^= _h10; _h11 ^= _h0; _h0 = RotL64(_h0, 11); _h11 += _h1;
        _h1 += ReadLE64(data, offset + 8);
        _h3 ^= _h11; _h0 ^= _h1; _h1 = RotL64(_h1, 32); _h0 += _h2;
        _h2 += ReadLE64(data, offset + 16);
        _h4 ^= _h0; _h1 ^= _h2; _h2 = RotL64(_h2, 43); _h1 += _h3;
        _h3 += ReadLE64(data, offset + 24);
        _h5 ^= _h1; _h2 ^= _h3; _h3 = RotL64(_h3, 31); _h2 += _h4;
        _h4 += ReadLE64(data, offset + 32);
        _h6 ^= _h2; _h3 ^= _h4; _h4 = RotL64(_h4, 17); _h3 += _h5;
        _h5 += ReadLE64(data, offset + 40);
        _h7 ^= _h3; _h4 ^= _h5; _h5 = RotL64(_h5, 28); _h4 += _h6;
        _h6 += ReadLE64(data, offset + 48);
        _h8 ^= _h4; _h5 ^= _h6; _h6 = RotL64(_h6, 39); _h5 += _h7;
        _h7 += ReadLE64(data, offset + 56);
        _h9 ^= _h5; _h6 ^= _h7; _h7 = RotL64(_h7, 57); _h6 += _h8;
        _h8 += ReadLE64(data, offset + 64);
        _h10 ^= _h6; _h7 ^= _h8; _h8 = RotL64(_h8, 55); _h7 += _h9;
        _h9 += ReadLE64(data, offset + 72);
        _h11 ^= _h7; _h8 ^= _h9; _h9 = RotL64(_h9, 54); _h8 += _h10;
        _h10 += ReadLE64(data, offset + 80);
        _h0 ^= _h8; _h9 ^= _h10; _h10 = RotL64(_h10, 22); _h9 += _h11;
        _h11 += ReadLE64(data, offset + 88);
        _h1 ^= _h9; _h10 ^= _h11; _h11 = RotL64(_h11, 46); _h10 += _h0;
    }

    private void EndPartial(byte[] data)
    {
        _h0 += ReadLE64(data, 0);
        _h1 += ReadLE64(data, 8);
        _h2 += ReadLE64(data, 16);
        _h3 += ReadLE64(data, 24);
        _h4 += ReadLE64(data, 32);
        _h5 += ReadLE64(data, 40);
        _h6 += ReadLE64(data, 48);
        _h7 += ReadLE64(data, 56);
        _h8 += ReadLE64(data, 64);
        _h9 += ReadLE64(data, 72);
        _h10 += ReadLE64(data, 80);
        _h11 += ReadLE64(data, 88);

        _h11 += _h1; _h2 ^= _h11; _h1 = RotL64(_h1, 44);
        _h0 += _h2; _h3 ^= _h0; _h2 = RotL64(_h2, 15);
        _h1 += _h3; _h4 ^= _h1; _h3 = RotL64(_h3, 34);
        _h2 += _h4; _h5 ^= _h2; _h4 = RotL64(_h4, 21);
        _h3 += _h5; _h6 ^= _h3; _h5 = RotL64(_h5, 38);
        _h4 += _h6; _h7 ^= _h4; _h6 = RotL64(_h6, 33);
        _h5 += _h7; _h8 ^= _h5; _h7 = RotL64(_h7, 10);
        _h6 += _h8; _h9 ^= _h6; _h8 = RotL64(_h8, 13);
        _h7 += _h9; _h10 ^= _h7; _h9 = RotL64(_h9, 38);
        _h8 += _h10; _h11 ^= _h8; _h10 = RotL64(_h10, 53);
        _h9 += _h11; _h0 ^= _h9; _h11 = RotL64(_h11, 42);
        _h10 += _h0; _h1 ^= _h10; _h0 = RotL64(_h0, 54);
    }

    private static ulong RotL64(ulong x, int k) => (x << k) | (x >> (64 - k));

    private static ulong ReadLE64(byte[] data, int offset)
    {
        if (offset + 8 > data.Length)
            return 0;
        return BitConverter.ToUInt64(data, offset);
    }
}
