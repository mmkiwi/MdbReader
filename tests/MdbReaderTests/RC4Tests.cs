// This file is part of MdbReader. Licensed under the LGPL version 2.0.
// You should have received a coy of the GNU LGPL version along with this
// program. If not, see https://www.gnu.org/licenses/old-licenses/lgpl-2.0.html
//
// Copyright Micah Makaiwi.
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using MMKiwi.MdbReader.Helpers;
using FluentAssertions;
namespace MMKiwi.MdbReader.Tests;

public sealed class RC4Tests
{

    [Theory]
    [MemberData(nameof(AllTestData))]
    public void TestRc4Encrypt(byte[] key, byte[] plaintext, byte[] ciphertext)
    {
        RC4.Apply(plaintext, key).Should().BeEquivalentTo(ciphertext);
    }

    [Theory]
    [MemberData(nameof(AllTestData))]
    public void TestRc4Decrypt(byte[] key, byte[] plaintext, byte[] ciphertext)
    {
        RC4.Apply(ciphertext, key).Should().BeEquivalentTo(plaintext);
    }

    [Theory]
    [MemberData(nameof(AllTestData))]
    public void TestRc4EncryptInPlace(byte[] key, byte[] plaintext, byte[] ciphertext)
    {
        RC4.ApplyInPlace(plaintext, key);
        plaintext.Should().BeEquivalentTo(ciphertext);
    }

    [Theory]
    [MemberData(nameof(AllTestData))]
    public void TestRc4DecryptInPlace(byte[] key, byte[] plaintext, byte[] ciphertext)
    {
        RC4.ApplyInPlace(ciphertext, key);
        plaintext.Should().BeEquivalentTo(plaintext);
    }

    public static IEnumerable<object[]> AllTestData => new object[][] {
        ToParameters("Key"u8.ToArray(),
                     "Plaintext"u8.ToArray(),
                     new byte[] {0xBB, 0xF3, 0x16, 0xE8, 0xD9, 0x40, 0xAF, 0x0A, 0xD3}),
        ToParameters("Wiki"u8.ToArray(),
                     "pedia"u8.ToArray(),
                     new byte[] { 0x10, 0x21, 0xBF, 0x04, 0x20}),
        ToParameters("Secret"u8.ToArray(),
                     "Attack at dawn"u8.ToArray(),
                     new byte[] {0x45, 0xA0, 0x1F, 0x64, 0x5F, 0xC3, 0x5B, 0x38, 0x35, 0x52, 0x54, 0x4B, 0x9B, 0xF5})
    };

    private static object[] ToParameters(byte[] key, byte[] plaintext, byte[] ciphertext)
        => new object[] { key, plaintext, ciphertext };
}