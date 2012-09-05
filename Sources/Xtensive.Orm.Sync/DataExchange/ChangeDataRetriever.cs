using System;
using Microsoft.Synchronization;

namespace Xtensive.Orm.Sync.DataExchange
{
  /// <summary>
  /// Implementation of <see cref="IChangeDataRetriever"/>.
  /// </summary>
  [Serializable]
  public sealed class ChangeDataRetriever : IChangeDataRetriever
  {
    /// <summary>
    /// Gets change set this instance wraps.
    /// </summary>
    public ChangeSet ChangeSet { get; private set; }

    #region Implementation of IChangeDataRetriever

    /// <summary>
    /// Gets the ID format schema of the provider.
    /// </summary>
    /// <returns>
    /// The ID format schema of the provider.
    /// </returns>
    public SyncIdFormatGroup IdFormats { get; private set; }

    /// <summary>
    /// When overridden in a derived class, this method retrieves item data for a change.
    /// </summary>
    /// <returns>
    /// The item data for the change.
    /// </returns>
    /// <param name="loadChangeContext">Metadata that describes the change for which data should be retrieved.</param>
    public object LoadChangeData(LoadChangeContext loadChangeContext)
    {
      var globalId = loadChangeContext.ItemChange.ItemId.GetGuidId();
      return ChangeSet[globalId];
    }

    #endregion

    /// <summary>
    /// Binds all internal structures to <see cref="Domain"/>.
    /// </summary>
    /// <param name="domain">The domain.</param>
    public void BindTo(Domain domain)
    {
      ChangeSet.BindTo(domain);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChangeDataRetriever"/> class.
    /// </summary>
    /// <param name="idFormats">The id formats.</param>
    /// <param name="changeSet">The change set.</param>
    public ChangeDataRetriever(SyncIdFormatGroup idFormats, ChangeSet changeSet)
    {
      if (idFormats==null)
        throw new ArgumentNullException("idFormats");
      if (changeSet==null)
        throw new ArgumentNullException("changeSet");

      IdFormats = idFormats;
      ChangeSet = changeSet;
    }
  }
}
