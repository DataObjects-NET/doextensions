using System;
using System.Configuration;

namespace Xtensive.Orm.Reprocessing.Configuration
{
  /// <summary>
  /// The configuration of the reprocessing system.
  /// </summary>
  public class ReprocessingConfiguration
  {
    /// <summary>
    /// Gets default value of the <see cref="DefaultTransactionOpenMode"/> property.
    /// </summary>
    public static readonly TransactionOpenMode DefaultDefaultTransactionOpenMode = TransactionOpenMode.New;

    /// <summary>
    /// Gets default value of the <see cref="DefaultExecuteStrategy"/> property.
    /// </summary>
    public static readonly Type DefaultDefaultExecuteStrategy = typeof (HandleReprocessableExceptionStrategy);

    /// <summary>
    /// Gets or sets default value of the <see cref="TransactionOpenMode"/> parameter.
    /// </summary>
    public TransactionOpenMode DefaultTransactionOpenMode { get; set; }

    /// <summary>
    /// Gets or sets default value of the <see cref="IExecuteActionStrategy"/> parameter.
    /// </summary>
    public Type DefaultExecuteStrategy { get; set; }

    /// <summary>
    /// Loads the reprocessing configuration from default section in application configuration file.
    /// </summary>
    /// <returns>The configuration.</returns>
    public static ReprocessingConfiguration Load()
    {
      return Load(ConfigurationSection.DefaultSectionName);
    }

    /// <summary>
    /// Loads the reprocessing configuration from <see cref="sectionName"/> section in application configuration file.
    /// </summary>
    /// <param name="sectionName">Name of the section.</param>
    /// <returns>The configuration.</returns>
    public static ReprocessingConfiguration Load(string sectionName)
    {
      var section = (ConfigurationSection) ConfigurationManager.GetSection(sectionName);
      return section==null
        ? new ReprocessingConfiguration()
        : new ReprocessingConfiguration {
          DefaultExecuteStrategy = section.DefaultExecuteStrategy,
          DefaultTransactionOpenMode = section.DefaultTransactionOpenMode
        };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReprocessingConfiguration"/> class.
    /// </summary>
    public ReprocessingConfiguration()
    {
      DefaultExecuteStrategy = DefaultDefaultExecuteStrategy;
      DefaultTransactionOpenMode = DefaultDefaultTransactionOpenMode;
    }
  }
}