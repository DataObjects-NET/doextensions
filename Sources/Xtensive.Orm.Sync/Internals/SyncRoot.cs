// Copyright (C) 2012 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2012.05.30

using System;
using Xtensive.Orm.Model;

namespace Xtensive.Orm.Sync
{
  public class SyncRoot
  {
    public Type ItemType { get; set; }

    public FieldInfo EntityField { get; set; }

    public Type EntityType
    {
      get { return EntityField.ValueType; }
    }
  }
}