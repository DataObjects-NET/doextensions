using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using Microsoft.Synchronization;
using Xtensive.IoC;
using Xtensive.Orm.Metadata;
using Xtensive.Orm.Model;
using Xtensive.Orm.Services;
using Xtensive.Orm.Tracking;
using Xtensive.Tuples;
using Type = System.Type;

namespace Xtensive.Orm.Sync
{
  [Service(typeof (SyncMetadataStore), Singleton = true)]
  public class SyncMetadataStore : SessionBound, ISessionService
  {
    private readonly Dictionary<Type, SyncInfoMetadata> metadataCache = new Dictionary<Type, SyncInfoMetadata>();

    private readonly SyncTickGenerator tickGenerator;

    public SyncIdFormatGroup IdFormats { get { return Wellknown.IdFormats; } }

    public SyncId ReplicaId { get; private set; }

    public SyncKnowledge CurrentKnowledge { get; private set; }

    public ForgottenKnowledge ForgottenKnowledge { get; private set; }

    public long TickCount
    {
      get { return tickGenerator.GetLastTick(Session); }
    }

    public long NextTick
    {
      get { return tickGenerator.GetNextTick(Session); }
    }

    public IEnumerable<ItemChange> DetectChanges(SyncKnowledge destinationKnowledge)
    {
      var mappedKnowledge = CurrentKnowledge.MapRemoteKnowledgeToLocal(destinationKnowledge);
      mappedKnowledge.ReplicaKeyMap.FindOrAddReplicaKey(CurrentKnowledge.ReplicaId);

      foreach (var item in Session.Query.All<SyncInfo>()) {
        var result = DetectChange(item, mappedKnowledge);
        if (result != null)
          yield return result;
      }
    }

    private ItemChange DetectChange(ISyncInfo syncInfo, SyncKnowledge mappedKnowledge)
    {
      var createdVersion = syncInfo.CreatedVersion;
      var lastChangeVersion = syncInfo.ChangeVersion;
      var changeKind = ChangeKind.Update;

      if (syncInfo.IsTombstone) {
        changeKind = ChangeKind.Deleted;
        lastChangeVersion = syncInfo.TombstoneVersion;
      }

      if (mappedKnowledge.Contains(ReplicaId, syncInfo.SyncId, lastChangeVersion))
        return null;

      return new ItemChange(IdFormats, ReplicaId, syncInfo.SyncId, changeKind, createdVersion, lastChangeVersion);
    }


    public IEnumerable<ItemChange> GetLocalChanges(IEnumerable<ItemChange> sourceChanges)
    {
      var identifiers = sourceChanges
        .Select(i => i.ItemId.GetGuidId())
        .ToList();
      var items = Session.Query.All<SyncInfo>()
        .Where(i => i.GlobalId.In(identifiers))
        .ToDictionary(i => i.GlobalId);

      foreach (var change in sourceChanges) {

        var changeKind = ChangeKind.UnknownItem;
        var createdVersion = SyncVersion.UnknownVersion;
        var lastChangeVersion = SyncVersion.UnknownVersion;
        
        SyncInfo info;
        if (items.TryGetValue(change.ItemId.GetGuidId(), out info)) {
          createdVersion = info.CreatedVersion;
          lastChangeVersion = info.ChangeVersion;
          if (info.IsTombstone) {
            changeKind = ChangeKind.Deleted;
            lastChangeVersion = info.TombstoneVersion;
          }
        }

        yield return new ItemChange(IdFormats, ReplicaId, change.ItemId, changeKind,
        createdVersion, lastChangeVersion);
      }
    }

    internal SyncInfoMetadata GetSyncInfoMetadata(Type entityType)
    {
      SyncInfoMetadata result;
      if (metadataCache.TryGetValue(entityType, out result))
        return result;

      entityType = Session.Domain.Model.Types[entityType].GetRoot().UnderlyingType;
      Type underlyingType = typeof (SyncInfo<>).MakeGenericType(entityType);
      TypeInfo typeInfo = Session.Domain.Model.Types[underlyingType];

      result = new SyncInfoMetadata {
        UnderlyingType = underlyingType,
        EntityField = typeInfo.Fields["Entity"],
        TypeInfo = typeInfo
      };

      metadataCache[entityType] = result;
      return result;
    }

    internal void ProcessTrackingResult(IEnumerable<ITrackingItem> changes)
    {
      var accessor = Session.Services.Get<DirectEntityAccessor>();

      foreach (var change in changes) {
        var entityKey = change.Key;
        var entityType = entityKey.TypeInfo.UnderlyingType;

        long nextTick = NextTick;
        var syncInfoMetadata = GetSyncInfoMetadata(entityType);
        SyncInfo syncInfo = null;
        if (change.State==TrackingItemState.Created) {
          syncInfo = (SyncInfo) accessor.CreateEntity(syncInfoMetadata.UnderlyingType);
          accessor.SetReferenceKey(syncInfo, syncInfoMetadata.EntityField, entityKey);
          syncInfo.CreatedReplicaKey = 0;
          syncInfo.CreatedTickCount = nextTick;
          syncInfo.GlobalId = Guid.NewGuid();
        }
        else
          syncInfo = FetchSyncInfo(syncInfoMetadata, entityKey);

        if (syncInfo==null)
          continue;

        syncInfo.ChangeTickCount = nextTick;
        syncInfo.ChangeReplicaKey = 0;
        syncInfo.Text = change.RawData.Format();
         
        if (change.State==TrackingItemState.Deleted) {
          syncInfo.TombstoneTickCount = nextTick;
          syncInfo.TombstoneReplicaKey = 0;
          syncInfo.IsTombstone = true;
        }
      }
    }

    internal SyncInfo FetchSyncInfo(SyncInfoMetadata syncInfoMetadata, Key entityKey)
    {
      MethodInfo mi = GetType().GetMethod("TryFetchSyncInfo").MakeGenericMethod(syncInfoMetadata.UnderlyingType);
      return (SyncInfo) mi.Invoke(this, new object[] {entityKey});
    }

    private SyncInfo TryFetchSyncInfo<T>(Key key) where T : Entity
    {
      return Session.Query.All<SyncInfo<T>>().SingleOrDefault(i => i.Entity.Key==key);
    }

    #region Initialization & knowledge update bits

    private void Initialize()
    {
      var names = new[] {Wellknown.FieldNames.ReplicaId, Wellknown.FieldNames.CurrentKnowledge, Wellknown.FieldNames.ForgottenKnowledge};
      var values = Session.Query.All<Extension>()
        .Where(e => e.Name.In(names)).ToArray();

      var value = values.SingleOrDefault(v => v.Name==Wellknown.FieldNames.ReplicaId);
      if (value==null) {
        ReplicaId = new SyncId(Guid.NewGuid());
        using (Session.Activate())
          new Extension(Wellknown.FieldNames.ReplicaId) {
            Text = ReplicaId.GetGuidId().ToString()
          };
      }
      else {
        try { ReplicaId = new SyncId(new Guid(value.Text)); }
        catch (Exception) { }
      }

      value = values.SingleOrDefault(v => v.Name==Wellknown.FieldNames.CurrentKnowledge);
      if (value!=null) {
        CurrentKnowledge = Deserialize<SyncKnowledge>(value.Text);
        CurrentKnowledge.SetLocalTickCount((ulong) TickCount);
      }
      else
        CurrentKnowledge = new SyncKnowledge(IdFormats, ReplicaId, (ulong) TickCount);

      value = values.SingleOrDefault(v => v.Name==Wellknown.FieldNames.ForgottenKnowledge);
      if (value!=null)
        ForgottenKnowledge = Deserialize<ForgottenKnowledge>(value.Text);
      else
        ForgottenKnowledge = new ForgottenKnowledge(IdFormats, CurrentKnowledge);
    }

    public void UpdateKnowledge(SyncKnowledge syncKnowledge, ForgottenKnowledge forgottenKnowledge)
    {
      if (syncKnowledge==null)
        throw new ArgumentNullException("syncKnowledge");

      var names = new[] {Wellknown.FieldNames.CurrentKnowledge, Wellknown.FieldNames.ForgottenKnowledge};
      var values = Session.Query.All<Extension>()
        .Where(e => e.Name.In(names)).ToArray();

      var value = values.SingleOrDefault(e => e.Name==Wellknown.FieldNames.CurrentKnowledge);
      if (value==null)
        using (Session.Activate())
          value = new Extension(Wellknown.FieldNames.CurrentKnowledge);
      value.Text = Serialize(syncKnowledge);

      if (forgottenKnowledge == null)
        return;

      value = values.SingleOrDefault(e => e.Name==Wellknown.FieldNames.ForgottenKnowledge);
      if (value==null)
        using (Session.Activate())
          value = new Extension(Wellknown.FieldNames.ForgottenKnowledge);
      value.Text = Serialize(forgottenKnowledge);
    }

    private static T Deserialize<T>(string value)
    {
      using (var reader = new StringReader(value)) {
        var serializer = new XmlSerializer(typeof (T));
        return (T) serializer.Deserialize(reader);
      }
    }

    private static string Serialize<T>(T value)
    {
      using (var writer = new StringWriter()) {
        var serializer = new XmlSerializer(typeof (T));
        serializer.Serialize(writer, value);
        return writer.ToString();
      }
    }

    #endregion

    [ServiceConstructor]
    public SyncMetadataStore(Session session)
      : base(session)
    {
       if (tickGenerator == null)
        tickGenerator = session.Domain.Services.Get<SyncTickGenerator>();
      
      Initialize();
   }
  }
}