using System;
using NUnit.Framework;

namespace TestCommon.Tests
{
  [TestFixture]
  public class TestConfigurationTest
  {
     [Test]
     public void Test()
     {
       var storage = TestConfiguration.Instance;
       Console.WriteLine("storage: {0}", storage);
       var configuration = DomainConfigurationFactory.Create();
       Console.WriteLine("connection: {0}", configuration.ConnectionInfo);
       var configuration2 = DomainConfigurationFactory.Create("remote");
       Console.WriteLine("connection2: {0}", configuration2.ConnectionInfo);
     }
  }
}