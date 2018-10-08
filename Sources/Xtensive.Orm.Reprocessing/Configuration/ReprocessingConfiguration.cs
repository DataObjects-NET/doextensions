using System;
using System.Configuration;
using Xtensive.Core;

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
    /// Loads the reprocessing configuration from default section of application configuration or gets the reprocessing configuration with default settings if there is no default section.
    /// </summary>
    /// <returns>The configuration.</returns>
    public static ReprocessingConfiguration LoadOrGetDefault()
    {
      var section = (ConfigurationSection) ConfigurationManager.GetSection(ConfigurationSection.DefaultSectionName);
      return section==null
        ? new ReprocessingConfiguration()
        : new ReprocessingConfiguration {
          DefaultExecuteStrategy = section.DefaultExecuteStrategy,
          DefaultTransactionOpenMode = section.DefaultTransactionOpenMode
        };
    }

    /// <summary>
    /// Loads the reprocessing configuration from default section of given <see cref="System.Configuration.Configuration"/> or gets the reprocessing configuration with default settings if there is no default section.
    /// </summary>
    /// <param name="configuration">Configuration from which reprocessing configuration should be loaded.</param>
    /// <returns>The configuration.</returns>
    public static ReprocessingConfiguration LoadOrGetDefault(System.Configuration.Configuration configuration)
    {
      return LoadOrGetDefault(configuration, ConfigurationSection.DefaultSectionName);
    }

    /// <summary>
    /// Loads the reprocessing configuration from from <paramref name="sectionName"/> section of given <see cref="System.Configuration.Configuration"/> or gets the reprocessing configuration with default settings if there is no such section.
    /// </summary>
    /// <param name="configuration">Configuration from which reprocessing configuration should be loaded.</param>
    /// <param name="sectionName">Name of the section.</param>
    /// <returns>The configuration.</returns>
    public static ReprocessingConfiguration LoadOrGetDefault(System.Configuration.Configuration configuration, string sectionName)
    {
      var section = (ConfigurationSection) configuration.GetSection(sectionName);
      return section==null
        ? new ReprocessingConfiguration()
        : new ReprocessingConfiguration {
          DefaultExecuteStrategy = section.DefaultExecuteStrategy,
          DefaultTransactionOpenMode = section.DefaultTransactionOpenMode
        };
    }

    public static ReprocessingConfiguration Load()
    {
      var section = (ConfigurationSection) ConfigurationManager.GetSection(ConfigurationSection.DefaultSectionName);
      if (section==null)
        throw new InvalidOperationException(string.Format("Section '{0}' is not found in given configuration", ConfigurationSection.DefaultSectionName));
      return new ReprocessingConfiguration {
        DefaultExecuteStrategy = section.DefaultExecuteStrategy,
        DefaultTransactionOpenMode = section.DefaultTransactionOpenMode
      };
    }

    /// <summary>
    /// Loads the reprocessing configuration from default section of given <see cref="System.Configuration.Configuration"/>.
    /// </summary>
    /// <param name="configuration"><see cref="System.Configuration.Configuration"/> from which reprocessing configuration should be loaded.</param>
    /// <returns>The reprocessing configuration.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="configuration"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Default section is not represented in the <paramref name="configuration"/>.</exception>
    public static ReprocessingConfiguration Load(System.Configuration.Configuration configuration)
    {
      return Load(configuration, ConfigurationSection.DefaultSectionName);
    }

    /// <summary>
    /// Loads the reprocessing configuration from <paramref name="sectionName"/> section of given <see cref="System.Configuration.Configuration"/>.
    /// </summary>
    /// <param name="configuration"><see cref="System.Configuration.Configuration"/> from which reprocessing configuration should be loaded.</param>
    /// <param name="sectionName">Name of the section.</param>
    /// <returns>The reprocessing configuration.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="configuration"/> is null.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="sectionName"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Section with given name is not represented in the <paramref name="configuration"/></exception>
    public static ReprocessingConfiguration Load(System.Configuration.Configuration configuration, string sectionName)
    {
      ArgumentValidator.EnsureArgumentNotNull(configuration, "configuration");
      ArgumentValidator.EnsureArgumentNotNull(sectionName, "sectionName");

      var section = (ConfigurationSection) configuration.GetSection(sectionName);
      if (section==null)
        throw new InvalidOperationException(string.Format("Section '{0}' is not found in given configuration", sectionName));
      return new ReprocessingConfiguration {
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