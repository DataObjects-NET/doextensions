using System;
using System.Configuration;

namespace Xtensive.Orm.Reprocessing.Configuration
{
  /// <summary>
  /// A root element of reprocessing configuration section within a configuration file.
  /// </summary>
  public class ConfigurationSection : System.Configuration.ConfigurationSection
  {
    /// <summary>
    /// Gets default section name for reprocessing configuration.
    /// Value is "Xtensive.Reprocessing".
    /// </summary>
    public static readonly string DefaultSectionName = "Xtensive.Orm.Reprocessing";

    /// <summary>
    /// Gets or sets default transaction open mode.
    /// </summary>
    [ConfigurationProperty("defaultTransactionOpenMode", DefaultValue = TransactionOpenMode.New)]
    public TransactionOpenMode DefaultTransactionOpenMode { get; set; }

    /// <summary>
    /// Gets or sets default execute strategy
    /// </summary>
    [ConfigurationProperty("defaultExecuteStrategy", DefaultValue = typeof(HandleReprocessableExceptionStrategy))]
    public Type DefaultExecuteStrategy { get; set; }
  }
}