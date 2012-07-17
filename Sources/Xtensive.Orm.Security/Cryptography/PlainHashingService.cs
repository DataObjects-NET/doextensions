// Copyright (C) 2011 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2011.05.22

using System;
using Xtensive.IoC;

namespace Xtensive.Orm.Security.Cryptography
{
  /// <summary>
  /// Implementation of <see cref="IHashingService"/> without any hashing.
  /// </summary>
  [Service(typeof(IHashingService), Singleton = true, Name = "plain")]
  public class PlainHashingService : IHashingService
  {
    /// <inheritdoc/>
    public string ComputeHash(string value)
    {
      return value;
    }

    /// <inheritdoc/>
    public bool VerifyHash(string value, string hash)
    {
      return StringComparer.Ordinal.Compare(value, hash) == 0;
    }
  }
}