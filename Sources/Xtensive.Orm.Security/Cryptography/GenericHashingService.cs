// Copyright (C) 2011 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2011.06.10

using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Xtensive.Orm.Security.Cryptography
{
  /// <summary>
  /// Generic <see cref="IHashingService"/> implementation.
  /// </summary>
  public abstract class GenericHashingService : IHashingService
  {
    /// <summary>
    /// The size of salt.
    /// </summary>
    public const int SaltSize = 8;

    /// <summary>
    /// Gets the hash algorithm.
    /// </summary>
    /// <value>The hash algorithm.</value>
    public HashAlgorithm HashAlgorithm { get; private set; }

    /// <summary>
    /// Gets the salt.
    /// </summary>
    /// <returns>The salt.</returns>
    protected byte[] GetSalt()
    {
      var salt = new byte[SaltSize];
      var rng = new RNGCryptoServiceProvider();
      rng.GetNonZeroBytes(salt);
      return salt;
    }

    /// <summary>
    /// Computes the hash.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="salt">The salt.</param>
    /// <returns>String representation of hash.</returns>
    protected string ComputeHash(byte[] value, byte[] salt)
    {
      var hash = HashAlgorithm.ComputeHash(salt.Concat(value).ToArray());
      return Convert.ToBase64String(hash.Concat(salt).ToArray());
    }

    #region IHashingService Members

    /// <inheritdoc/>
    public string ComputeHash(string value)
    {
      return ComputeHash(Encoding.UTF8.GetBytes(value), GetSalt());
    }

    /// <inheritdoc/>
    public bool VerifyHash(string value, string hash)
    {
      byte[] source;
      try {
        source = Convert.FromBase64String(hash);
      }
      catch (FormatException) {
        return false;
      }

      int hashSize = HashAlgorithm.HashSize/8;

      if (source.Length < hashSize)
        return false;

      var salt = source.Skip(hashSize).ToArray();
      return StringComparer.Ordinal.Compare(hash, ComputeHash(Encoding.UTF8.GetBytes(value), salt)) == 0;
    }

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="GenericHashingService"/> class.
    /// </summary>
    /// <param name="hashAlgorithm">The hash algorithm.</param>
    protected GenericHashingService(HashAlgorithm hashAlgorithm)
    {
      HashAlgorithm = hashAlgorithm;
    }
  }
}