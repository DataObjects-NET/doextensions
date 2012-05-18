// Copyright (C) 2012 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2012.05.16

using System;
using System.Collections.Generic;
using System.Linq;
using Xtensive.Tuples;
using Tuple = Xtensive.Tuples.Tuple;

namespace Xtensive.Orm.Tracking
{
  [Serializable]
  public class TrackingItem : ITrackingItem
  {
    public Key Key { get; private set; }

    public DifferentialTuple RawData { get; private set; }

    public TrackingItemState State { get; private set; }

    public IEnumerable<ChangedValue> GetChangedValues()
    {
      var origValues = RawData.Origin;
      var changedValues = RawData.Difference;

      if (State==TrackingItemState.Created) {
        origValues = null;
        changedValues = RawData.Origin;
      }

      foreach (var field in Key.TypeInfo.Fields.Where(f => f.Column != null)) {
        object origValue = null, changedValue = null;
        int fieldIndex = field.MappingInfo.Offset;
        if (origValues != null) 
          origValue = origValues.GetValue(fieldIndex);
        if (changedValues != null) {
          if (changedValues.GetFieldState(fieldIndex) != TupleFieldState.Available)
            continue;
          changedValue = changedValues.GetValue(fieldIndex);
        }
        yield return new ChangedValue(field, origValue, changedValue);
           
      }
    }

    public void MergeWith(TrackingItem source)
    {
      if (source == null)
        throw new NullReferenceException("source");

      if (State == TrackingItemState.Removed && source.State == TrackingItemState.Created) {
        State = TrackingItemState.Modified;
        RawData = source.RawData; // TODO: Check whether a clone is required
        return;
      }

      if (State == TrackingItemState.Created && source.State == TrackingItemState.Modified) {
        State = TrackingItemState.Created;
        MergeWith(source.RawData.Difference);
        return;
      }

      MergeWith(source.RawData.Difference);
      State = source.State;
    }

    private void MergeWith(Tuple difference)
    {
      if (RawData.Difference==null)
        RawData.Difference = difference;
      else
        RawData.Difference.MergeWith(difference, MergeBehavior.PreferDifference);
    }

    public TrackingItem(Key key, DifferentialTuple tuple, TrackingItemState state)
    {
      if (key == null)
        throw new NullReferenceException("key");
      if (state != TrackingItemState.Removed && tuple == null)
        throw new NullReferenceException("tuple");

      Key = key;
      RawData = (DifferentialTuple) tuple.Clone();
      State = state;
    }
  }
}