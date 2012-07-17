// Copyright (C) 2011 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2011.05.22

using Xtensive.Orm;

namespace Xtensive.Orm.Security
{
  /// <summary>
  /// Hashing service.
  /// </summary>
  public interface IHashingService : IDomainService
  {
    /// <summary>
    /// Computes the hash.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>String representation of hash.</returns>
    string ComputeHash(string value);

    /// <summary>
    /// Verifies the hash.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="hash">The hash.</param>
    /// <returns><see langword="True" /> if hashes are equal; otherwise <see langword="false" />.</returns>
    bool VerifyHash(string value, string hash);
  }
}