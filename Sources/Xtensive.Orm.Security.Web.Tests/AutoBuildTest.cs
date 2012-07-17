using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web.Security;
using NUnit.Framework;
using Xtensive.Core;
using Xtensive.Orm.Configuration;
using Xtensive.Orm.Web;

namespace Xtensive.Orm.Security.Web.Tests
{
  [TestFixture]
  public abstract class AutoBuildTest
  {
    protected Domain Domain { get; private set; }

    protected OrmMembershipProvider Provider { get; private set; }

    protected List<TestUser> Users { get; private set; }

    [TestFixtureSetUp]
    public virtual void TestFixtureSetUp()
    {
      DomainConfiguration config = BuildConfiguration();
      Domain = BuildDomain(config);
      SessionManager.DomainBuilder = () => Domain;
      Provider = BuildProvider();
      PopulateData();
    }

    [TestFixtureTearDown]
    public virtual void TestFixtureTearDown()
    {
      Domain.DisposeSafely();
    }

    protected virtual DomainConfiguration BuildConfiguration()
    {
      return DomainConfiguration.Load("Default");
    }

    protected virtual Domain BuildDomain(DomainConfiguration configuration)
    {
      return Domain.Build(configuration);
    }

    protected virtual NameValueCollection BuildProviderConfiguration()
    {
      return new NameValueCollection();
    }

    protected virtual OrmMembershipProvider BuildProvider()
    {
      var result = new OrmMembershipProvider();
      NameValueCollection settings = BuildProviderConfiguration();
      result.Initialize(Wellknown.MembershipProviderName, settings);
      return result;
    }

    protected virtual void PopulateData()
    {
      Users = GetTestUsers(5, "Default");
      foreach (TestUser u in Users) {
        MembershipCreateStatus status;
        Provider.CreateUser(u.UserName, u.Password, u.Email, u.PasswordQuestion,
          u.PasswordAnswer, u.IsApproved, null, out status);
      }
    }

    private static List<TestUser> GetTestUsers(int numUsers, string prefix)
    {
      var t = new List<TestUser>();
      for (int i = 0; i < numUsers; i++) {
        string username = prefix + "TestUser" + i;
        var u = new TestUser {
          UserName = username,
          Password = prefix + "!TestPassword" + i,
          ProviderUserKey = null,
          Email = username + "@testdomain.com",
          PasswordQuestion = prefix + "TestPasswordQuestion" + i,
          PasswordAnswer = prefix + "TestPasswordAnswer" + i,
          IsApproved = true
        };
        t.Add(u);
      }
      return t;
    }
  }
}