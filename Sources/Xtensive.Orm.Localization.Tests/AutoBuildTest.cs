using System.Globalization;
using NUnit.Framework;
using Xtensive.Orm;
using Xtensive.Orm.Configuration;
using Xtensive.Reflection;

namespace Xtensive.Orm.Localization.Tests
{
  [TestFixture]
  public abstract class AutoBuildTest
  {
    public static CultureInfo EnglishCulture = new CultureInfo("en-US");
    public static string EnglishTitle = "Welcome!";
    public static string EnglishContent = "My dear guests, welcome to my birthday party!";

    public static CultureInfo SpanishCulture = new CultureInfo("es-ES");
    public static string SpanishTitle = "Bienvenido!";
    public static string SpanishContent = "Mis amigos mejores! Bienvenido a mi cumpleanos!";

    protected Domain Domain { get; private set; }

    [TestFixtureSetUp]
    public void TestFixtureSetUp()
    {
      Domain = Domain.Build(DomainConfiguration.Load("Default"));
      TypeLocalizationMap.Initialize(Domain);
      PopulateDatabase();
    }

    protected virtual void PopulateDatabase()
    {
    }
  }
}