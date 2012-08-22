using System;
using Microsoft.Synchronization;

namespace Xtensive.Orm.Sync
{
  /// <summary>
  /// Implementation of <see cref="IChangeDataRetriever"/>.
  /// </summary>
  [Serializable]
  public class ChangeDataRetriever : IChangeDataRetriever
  {
    private ChangeSet changeSet;

    #region Implementation of IChangeDataRetriever

    /// <summary>
    /// When overridden in a derived class, this method retrieves item data for a change.
    /// </summary>
    /// <returns>
    /// The item data for the change.
    /// </returns>
    /// <param name="loadChangeContext">Metadata that describes the change for which data should be retrieved.</param>
    public object LoadChangeData(LoadChangeContext loadChangeContext)
    {
      if (changeSet == null)
        return null;
      var globalId = loadChangeContext.ItemChange.ItemId.GetGuidId();
      return changeSet[globalId];
    }

    /// <summary>
    /// Gets the ID format schema of the provider.
    /// </summary>
    /// <returns>
    /// The ID format schema of the provider.
    /// </returns>
    public SyncIdFormatGroup IdFormats { get; private set; }

    #endregion

    /// <summary>
    /// Binds all internal structures to <see cref="Domain"/>.
    /// </summary>
    /// <param name="domain">The domain.</param>
    public void BindTo(Domain domain)
    {
      if (changeSet == null)
        return;

      changeSet.BindTo(domain);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChangeDataRetriever"/> class.
    /// </summary>
    /// <param name="idFormats">The id formats.</param>
    /// <param name="changeSet">The change set.</param>
    public ChangeDataRetriever(SyncIdFormatGroup idFormats, ChangeSet changeSet)
    {
      IdFormats = idFormats;
      this.changeSet = changeSet;
    }
  }
}
