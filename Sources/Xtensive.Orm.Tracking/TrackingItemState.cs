// Copyright (C) 2012 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2012.05.16

using System;

namespace Xtensive.Orm.Tracking
{
  [Serializable]
  public enum TrackingItemState
  {
    Created = 0,

    Modified = 1,

    Removed = 2,
  }
}