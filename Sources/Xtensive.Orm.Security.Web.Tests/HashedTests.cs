using System.Collections.Specialized;
using System.Configuration.Provider;
using NUnit.Framework;

namespace Xtensive.Orm.Security.Web.Tests
{
  [TestFixture]
  public class HashedTests : AutoBuildTest
  {
    protected override NameValueCollection BuildProviderConfiguration()
    {
      NameValueCollection settings = base.BuildProviderConfiguration();
      settings.Add("passwordFormat", "Hashed");
      return settings;
    }

    [Test]
    [ExpectedException(typeof (ProviderException))]
    public void GetPassword_GivenGoodUserAndGoodAnswer_ThrowsException()
    {
      string name = "GoodUser";
      string answer = "GoodAnswer";
      //Act
      Provider.GetPassword(name, answer);
    }

    [Test]
    [ExpectedException(typeof (ProviderException))]
    public void Hashed_GetPassword_AnyAnswer_ThrowsException()
    {
      TestUser u = Users[0];
      string answer = "KittyCatsLikeTuna";
      Provider.GetPassword(u.UserName, answer);
    }

    [Test]
    public void Hashed_ValidateUser_GoodPassword_ReturnsTrue()
    {
      TestUser u = Users[0];
      bool result;
      result = Provider.ValidateUser(u.UserName, u.Password);
      Assert.IsTrue(result);
    }

    [Test]
    public void ResetPassword_GoodUser_QandARequired_ReturnsNewPassword()
    {
      string name = "HashUser";
      string answer = "GoodAnswer";
      //Act
      string actual = Provider.ResetPassword(name, answer);
      //Assert
      Assert.AreNotEqual("", actual);
    }

    [Test]
    public void ValidateUser_GivenGoodUserGoodPassword_ReturnsTrue()
    {
      string userName = "HashUser";
      string userPass = "GoodPass";
      //Act
      bool actual = Provider.ValidateUser(userName, userPass);
      //Assert
      Assert.IsTrue(actual);
    }
  }
}