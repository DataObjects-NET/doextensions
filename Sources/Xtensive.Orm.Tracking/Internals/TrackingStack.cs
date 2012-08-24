// Copyright (C) 2012 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2012.05.16

using System.Collections.Generic;

namespace Xtensive.Orm.Tracking
{
  public class TrackingStack
  {
    private readonly Stack<TrackingStackFrame> frames = new Stack<TrackingStackFrame>();

    public void Push(TrackingStackFrame frame)
    {
      frames.Push(frame);
    }

    public TrackingStackFrame Peek()
    {
      return IsEmpty ? null : frames.Peek();
    }

    public TrackingStackFrame Pop()
    {
      return IsEmpty ? null : frames.Pop();
    }

    public bool IsEmpty
    {
      get { return frames.Count == 0; }
    }
  }
}