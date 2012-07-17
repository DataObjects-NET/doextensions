using System;
using System.Collections.Specialized;
using System.Configuration.Provider;
using System.Web.Security;
using NUnit.Framework;

namespace Xtensive.Orm.Security.Web.Tests
{
  [TestFixture]
  public class MainTests : AutoBuildTest
  {
    [Test]
    public void ChangePasswordQuestionAndAnswer_BadPassword_ReturnsFalse()
    {
      TestUser u = Users[0];
      string badpass = "BadPassword";
      bool result = false;
      result = Provider.ChangePasswordQuestionAndAnswer(u.UserName, badpass, u.PasswordQuestion, u.PasswordAnswer);
      //Cleanup
      Assert.IsFalse(result);
    }

    [Test]
    public void ChangePasswordQuestionAndAnswer_BadUser_ReturnsFalse()
    {
      string user = "BadUser";
      string pass = "BadPass";
      string question = "Good";
      string answer = "Answer";
      //Act
      bool actual = Provider.ChangePasswordQuestionAndAnswer(user, pass, question, answer);
      //Assert
      Assert.IsFalse(actual);
    }

    [Test]
    public void ChangePasswordQuestionAndAnswer_ValidUser_ReturnsTrue()
    {
      TestUser u = Users[0];
      string newquestion = "question";
      string newanswer = "answer";
      bool result = false;
      result = Provider.ChangePasswordQuestionAndAnswer(u.UserName, u.Password, newquestion, newanswer);
      //Cleanup
      if (result)
        Provider.ChangePasswordQuestionAndAnswer(u.UserName, u.Password, u.PasswordQuestion, u.PasswordAnswer);
      Assert.IsTrue(result);
    }

    [Test]
    [ExpectedException(typeof (ArgumentNullException))]
    public void ChangePassword_BadNewPass_ThrowsException()
    {
      TestUser u = Users[0];
      string newpass = "";
      Provider.ChangePassword(u.UserName, u.Password, newpass);
    }

    [Test]
    [ExpectedException(typeof (MembershipPasswordException))]
    public void ChangePassword_BadOldPass_ThrowsException()
    {
      TestUser u = Users[0];
      string newpass = "!Password??999";
      string badpass = "!!!!BadPass999";
      bool result = true;
      result = Provider.ChangePassword(u.UserName, badpass, newpass);
      Assert.IsFalse(result);
    }

    [Test]
    public void ChangePassword_SamePasswords_ReturnsTrue()
    {
      TestUser u = Users[0];
      bool result = false;
      result = Provider.ChangePassword(u.UserName, u.Password, u.Password);
      Assert.IsTrue(result);
    }

    [Test]
    public void ChangePassword_UnapprovedUser_ReturnsFalse()
    {
      TestUser u = Users[0];
      //Change user to unapproved
      MembershipUser user = Provider.GetUser(u.UserName, true);
      user.IsApproved = false;
      Provider.UpdateUser(user);

      string newpass = "!Password??999";
      bool result = true;
      result = Provider.ChangePassword(u.UserName, u.Password, newpass);
      //Cleanup
      if (result)
        Provider.ChangePassword(u.UserName, newpass, u.Password);
      user.IsApproved = true;
      Provider.UpdateUser(user);

      Assert.IsFalse(result);
    }

    [Test]
    public void ChangePassword_ValidUser_PasswordChanged()
    {
      TestUser u = Users[0];
      string newpass = "!Password??999";
      Provider.ChangePassword(u.UserName, u.Password, newpass);
      string curpass = Provider.GetPassword(u.UserName, u.PasswordAnswer);
      //Cleanup
      Provider.ChangePassword(u.UserName, newpass, u.Password);
      Assert.AreEqual(newpass, curpass);
    }

    [Test]
    public void ChangePassword_ValidUser_ReturnsTrue()
    {
      TestUser u = Users[0];
      string newpass = "!Password??999";
      bool result = false;
      result = Provider.ChangePassword(u.UserName, u.Password, newpass);
      //Cleanup
      if (result)
        Provider.ChangePassword(u.UserName, newpass, u.Password);
      Assert.IsTrue(result);
    }

    [Test]
    public void ChangePassword_BadUserBadPass_ReturnsFalse()
    {
      bool actual = Provider.ChangePassword("BadUser", "BadOldPassword", "NewPassword!");
      Assert.IsFalse(actual);
    }

    [Test]
    [ExpectedException(typeof (MembershipPasswordException))]
    public void ChangePassword_GoodUserBadPass_ThrowsException()
    {
      string username = Users[0].UserName;
      string oldpass = "BadOldPass";
      string newpass = "NewPassword!";
      //Act
      bool actual = Provider.ChangePassword(username, oldpass, newpass);
      //Assert
    }

    [Test]
    public void ChangePassword_GoodUserGoodPass_ReturnsTrue()
    {
      string username = Users[1].UserName;
      string oldpass = Users[1].Password;
      string newpass = "ABC123!?";

      Assert.IsTrue(Provider.ChangePassword(username, oldpass, newpass));
      // Cleanup
      Assert.IsTrue(Provider.ChangePassword(username, newpass, oldpass));
    }

    [Test]
    public void CreateUser_DuplicateEMail_ReturnsDuplicateEmail()
    {
      var u = Users[0];
      MembershipCreateStatus actual;
      MembershipCreateStatus expected = MembershipCreateStatus.DuplicateEmail;
      Provider.CreateUser(u.UserName + "x", u.Password, u.Email, u.PasswordQuestion, u.PasswordAnswer, u.IsApproved, u.ProviderUserKey, out actual);
      Assert.AreEqual(expected, actual);
    }

    [Test]
    public void CreateUser_DuplicateUserName_ReturnsDuplicateUserName()
    {
      var u = Users[0];
      MembershipCreateStatus actual;
      MembershipCreateStatus expected = MembershipCreateStatus.DuplicateUserName;
      Provider.CreateUser(u.UserName, u.Password, u.Email, u.PasswordQuestion, u.PasswordAnswer, u.IsApproved, u.ProviderUserKey, out actual);
      Assert.AreEqual(expected, actual);
    }

    [Test]
    public void CreateUser_InvalidPassword_ReturnsInvalidPassword()
    {
      var u = Users[0];
      MembershipCreateStatus actual;
      MembershipCreateStatus expected = MembershipCreateStatus.InvalidPassword;
      Provider.CreateUser(u.UserName, string.Empty, u.Email, u.PasswordQuestion, u.PasswordAnswer, u.IsApproved, u.ProviderUserKey, out actual);
      Assert.AreEqual(expected, actual);
    }

    [Test]
    public void CreateUser_InvalidPassword_ReturnsSuccess()
    {
      var u = Users[0];
      MembershipCreateStatus actual;
      MembershipCreateStatus expected = MembershipCreateStatus.Success;
      Provider.CreateUser(u.UserName + "_", u.Password + "_", "123"+u.Email, u.PasswordQuestion + "_", u.PasswordAnswer + "_", u.IsApproved, u.ProviderUserKey, out actual);
      //Cleanup
      if (expected==actual)
        Provider.DeleteUser(u.UserName + "_", true);
      Assert.AreEqual(expected, actual);
    }

    [Test]
    public void DeleteUser_BadUser_ReturnsFalse()
    {
      Assert.IsFalse(Provider.DeleteUser("BadUser", true));
    }

    [Test]
    public void DeleteUser_GoodUser_ReturnsTrue()
    {
      var u = Users[0];
      bool actual = Provider.DeleteUser(u.UserName, true);
      Assert.IsTrue(actual);
      // Clean up
      MembershipCreateStatus status;
      Provider.CreateUser(u.UserName, u.Password, u.Email, u.PasswordQuestion, u.PasswordAnswer, u.IsApproved, null, out status);
      Assert.AreEqual(MembershipCreateStatus.Success, status);
    }

    [Test]
    public void FindUserByEmail_BadEmail_ReturnsZeroRecords()
    {
      string email = "BadEmail";
      int recs = -1;
      int expectedRecs = 0;
      //Act
      MembershipUserCollection actual = Provider.FindUsersByEmail(email, 0, 99, out recs);
      //Assert
      Assert.AreEqual(expectedRecs, recs);
      Assert.AreEqual(expectedRecs, actual.Count);
    }

    [Test]
    public void FindUserByEmail_DuplicateEmail_ReturnsTwoRecords()
    {
      string email = "DupEmail";
      int recs = -1;
      int expectedRecs = 2;
      //Act
      MembershipUserCollection actual = Provider.FindUsersByEmail(email, 0, 99, out recs);
      //Assert
      Assert.AreEqual(expectedRecs, recs);
      Assert.AreEqual(expectedRecs, actual.Count);
    }

    [Test]
    public void FindUserByEmail_GoodEmail_ReturnsOneRecord()
    {
      int recs = -1;
      int expectedRecs = 1;
      //Act
      MembershipUserCollection actual = Provider.FindUsersByEmail(Users[0].Email, 0, 99, out recs);
      //Assert
      Assert.AreEqual(expectedRecs, recs);
      Assert.AreEqual(expectedRecs, actual.Count);
    }

    [Test]
    public void FindUserByName_BadName_ReturnsZeroRecords()
    {
      int recs = -1;
      int expectedRecs = 0;
      //Act
      MembershipUserCollection actual = Provider.FindUsersByName("BadName", 0, 99, out recs);
      //Assert
      Assert.AreEqual(expectedRecs, recs);
      Assert.AreEqual(expectedRecs, actual.Count);
    }

    [Test]
    public void FindUserByName_DuplicateName_ReturnsTwoRecords()
    {
      int recs = -1;
      int expectedRecs = 2;
      //Act
      MembershipUserCollection actual = Provider.FindUsersByName("DupName", 0, 99, out recs);
      //Assert
      Assert.AreEqual(expectedRecs, recs);
      Assert.AreEqual(expectedRecs, actual.Count);
    }

    [Test]
    public void FindUserByName_GoodName_ReturnsOneRecord()
    {
      TestUser u = Users[0];
      int recs = -1;
      int expectedRecs = 1;
      //Act
      MembershipUserCollection actual = Provider.FindUsersByName(u.UserName, 0, 99, out recs);
      //Assert
      Assert.AreEqual(expectedRecs, recs);
      Assert.AreEqual(expectedRecs, actual.Count);
    }

    [Test]
    public void FindUsersByEmail_InvalidUser_ReturnsNoRecords()
    {
      string email = "InvalidEmailAddress";
      int total = -1;
      MembershipUserCollection users = Provider.FindUsersByEmail(email, 0, 5, out total);
      Assert.AreEqual(total, 0);
    }

    [Test]
    public void FindUsersByEmail_ValidUser_ReturnsOneRecord()
    {
      TestUser u = Users[0];
      int total = 0;
      MembershipUserCollection users = Provider.FindUsersByEmail(u.Email, 0, 5, out total);
      Assert.AreEqual(total, 1);
    }

    [Test]
    public void FindUsersByName_InvalidUser_ReturnsNoRecords()
    {
      string badname = "InvalidUserName";
      int total = -1;
      MembershipUserCollection users = Provider.FindUsersByName(badname, 0, 5, out total);
      Assert.AreEqual(total, 0);
    }

    [Test]
    public void FindUsersByName_ValidUser_ReturnsOneRecord()
    {
      TestUser u = Users[0];
      int total = 0;
      MembershipUserCollection users = Provider.FindUsersByName(u.UserName, 0, 5, out total);
      Assert.AreEqual(total, 1);
    }

    [Test]
    public void GetAllUsers_FourPerPage_ReturnsFourRecords()
    {
      TestUser u = Users[0];
      int total = 0;
      //We should at least get four of our five test users
      MembershipUserCollection users = Provider.GetAllUsers(0, 4, out total);
      Assert.AreEqual(total, 4);
    }

    [Test]
    public void GetAllUsers_TwoUsers_ReturnsTwoUsers()
    {
      int expected = 2;
      int tot = -1;
      //Act
      MembershipUserCollection actual = Provider.GetAllUsers(0, 99, out tot);
      //Assert
      Assert.AreEqual(expected, actual.Count);
      Assert.AreEqual(expected, tot);
    }

    [Test]
    public void GetAllUsers_ZeroUsers_ReturnsZeroUsers()
    {
      int expected = 0;
      int tot = -1;
      //Act
      MembershipUserCollection actual = Provider.GetAllUsers(1, 99, out tot);
      //Assert
      Assert.AreEqual(expected, actual.Count);
      Assert.AreEqual(expected, tot);
    }

    [Test]
    public void GetNumberOfUsersOnline_FourPerPage_ReturnsFourRecords()
    {
      TestUser u = Users[0];
      int total = -1;
      //We should at least get four of our five test users
      total = Provider.GetNumberOfUsersOnline();
      Assert.IsFalse(total < 0);
    }

    [Test]
    public void GetPassword_AnswerNotRequired_ReturnsGoodPassword()
    {
      TestUser u = Users[0];
      string answer = "KittyCatsLikeTuna";
      string password;
      password = Provider.GetPassword(u.UserName, answer);
      Assert.AreEqual(password, u.Password);
    }

    [Test]
    public void GetPassword_BadAnswer_ThrowsException()
    {
      var u = Users[0];
      string answer = "KittyCatsLikeTuna";
      Provider.GetPassword(u.UserName, answer);
    }

    [Test]
    public void GetPassword_GoodUserAndBadAnswer_WithoutRequireAnswer_ReturnsPassword()
    {
      var provider = new OrmMembershipProvider()
        .With(c => c.EnablePasswordRetrieval, true)
        .With(c => c.RequiresQuestionAndAnswer, false)
        .Initialize();

      var u = Users[0];
      string actual = provider.GetPassword(u.UserName, "some invalid answer");
      //Assert
      Assert.AreEqual(u.Password, actual);
    }

    [Test]
    public void GetPassword_ValidAnswer_ReturnsGoodPassword()
    {
      var u = Users[0];
      string password;
      password = Provider.GetPassword(u.UserName, u.PasswordAnswer);
      Assert.AreEqual(password, u.Password);
    }

    [Test]
    [ExpectedException(typeof (NotSupportedException))]
    public void GetPassword_WhenRetrievalDisabled_ThrowsException()
    {
      var provider = new OrmMembershipProvider()
        .With(c => c.EnablePasswordRetrieval, false)
        .Initialize();

      provider.GetPassword("BadUser", "BadAnswer");
    }

    [Test]
    public void GetUser_GoodUserIdOnline_ReturnsUser()
    {
      throw new NotImplementedException();
      int id = 1;
      //Act
      MembershipUser actual = Provider.GetUser(id, true);
      //Assert
      Assert.IsNotNull(actual);
    }

    [Test]
    public void GetUser_GoodUserOnline_ReturnsUser()
    {
      var u = Users[0];
      var actual = Provider.GetUser(u.UserName, true);
      Assert.IsNotNull(actual);
    }

    [Test]
    [ExpectedException(typeof (ProviderException))]
    public void Initialize_CheckEncryptionKeyFails_ThrowsProviderException()
    {
      throw new NotImplementedException();
//      var tmpProv = new EncryptionErrorProvider();
//      //Act
//      var config = new NameValueCollection();
//      config.Add("passwordFormat", "Hashed");
//      tmpProv.Initialize("", config);
//      //Assert
    }

    [Test]
    public void Initialize_NullName_SetsDefaultName()
    {
      var provider = new OrmMembershipProvider();
      string expected = Wellknown.MembershipProviderName;
      provider.Initialize("", new NameValueCollection());
      string actual = Provider.Name;
      Assert.AreEqual(expected, actual);
    }

    [Test]
    public void ProviderConfiguration_CheckAllProperties()
    {
      var provider = new OrmMembershipProvider()
        .With(c => c.ApplicationName, "SomeName")
        .With(c => c.MaxInvalidPasswordAttempts, 3)
        .With(c => c.MinRequiredPasswordLength, 7)
        .With(c => c.MinRequiredNonAlphanumericCharacters, 1)
        .With(c => c.PasswordAttemptWindow, 10)
        .With(c => c.PasswordStrengthRegularExpression, "^.*(?=.{6,})(?=.*[a-z])(?=.*[A-Z]).*$")
        .Initialize();

      Assert.AreEqual(provider.ApplicationName, "SomeName");
      Assert.AreEqual(provider.MaxInvalidPasswordAttempts, 3);
      Assert.AreEqual(provider.MinRequiredNonAlphanumericCharacters, 1);
      Assert.AreEqual(provider.MinRequiredPasswordLength, 7);
      Assert.AreEqual(provider.PasswordAttemptWindow, 10);
      Assert.AreEqual(provider.PasswordStrengthRegularExpression, "^.*(?=.{6,})(?=.*[a-z])(?=.*[A-Z]).*$");
    }

    [Test]
    public void ResetPassword_AnswerNotRequired_ReturnsNewPassword()
    {
      var provider = new OrmMembershipProvider()
        .With(c => c.RequiresQuestionAndAnswer, false)
        .With(c => c.EnablePasswordReset, true)
        .Initialize();

      TestUser u = Users[0];
      string password = provider.ResetPassword(u.UserName, "Some string");
      // Cleanup
      provider.ChangePassword(u.UserName, password, u.Password);
      Assert.IsNotEmpty(password);
    }

    [Test]
    [ExpectedException(typeof (MembershipPasswordException))]
    public void ResetPassword_GoodUserBadAnswer_ThrowsException()
    {
      var provider = new OrmMembershipProvider()
        .With(c => c.RequiresQuestionAndAnswer, true)
        .With(c => c.EnablePasswordReset, true)
        .Initialize();

      var u = Users[0];
      provider.ResetPassword(u.UserName, "BadAnswer");
    }

    [Test]
    [ExpectedException(typeof (ProviderException))]
    public void ResetPassword_BadUserBadAnswer_ThrowsException()
    {
      Provider.ResetPassword("InvalidUser", "InvalidAnswer");
    }

    [Test]
    public void ResetPassword_GoodUser_ReturnsNewPassword()
    {
      TestUser u = Users[0];
      string newPass = Provider.ResetPassword(u.UserName, u.PasswordAnswer);
      // Cleanup
      Provider.ChangePassword(u.UserName, newPass, u.Password);
      Assert.AreNotEqual(newPass, "");
    }

    [Test]
    [ExpectedException(typeof (MembershipPasswordException))]
    public void ResetPassword_LockedUser_ThrowsException()
    {
      string name = "LockedUser";
      //Act
      string actual = Provider.ResetPassword(name, null);
      //Assert
    }

    [Test]
    [ExpectedException(typeof (ArgumentException))]
    public void ResetPassword_NullAnswer_ThrowsException()
    {
      var provider = new OrmMembershipProvider()
        .With(c => c.RequiresQuestionAndAnswer, true)
        .With(c => c.EnablePasswordReset, true)
        .Initialize();

      var u = Users[0];
      provider.ResetPassword(u.UserName, null);
    }

    [Test]
    [ExpectedException(typeof (NotSupportedException))]
    public void ResetPassword_WhenResetDisabled_ThrowsException()
    {
      var provider = new OrmMembershipProvider()
        .With(c => c.EnablePasswordReset, false)
        .With(c => c.EnablePasswordRetrieval, false)
        .Initialize();

      provider.ResetPassword("BadUser", "BadAnswer");
    }

    [Test]
    public void UnlockUser_GoodUser_ReturnsTrue()
    {
      var u = Users[0];
      bool actual = Provider.UnlockUser(u.UserName);
      Assert.IsTrue(actual);
    }

    [Test]
    public void UnlockUser_InvalidUser_ReturnsFalse()
    {
      bool result = false;
      result = Provider.UnlockUser("TheKingOfFrance");
      Assert.IsFalse(result);
    }

    [Test]
    public void UnlockUser_ValidUser_ReturnsTrue()
    {
      TestUser u = Users[0];
      bool result = Provider.UnlockUser(u.UserName);
      Assert.IsTrue(result);
    }

    [Test]
    public void UpdateUser_GoodUser_DoesNotThrowError()
    {
      var u = Users[0];
      MembershipUser m = Provider.GetUser(u.UserName, true);
      //Act
      Provider.UpdateUser(m);
      //Assert
      Assert.IsTrue(true);
    }

    [Test]
    public void ValidateUser_GoodUserEmptyPassword_ReturnsFalse()
    {
      TestUser u = Users[0];
      bool result = Provider.ValidateUser(u.UserName, String.Empty);
      Assert.IsFalse(result);
    }


    [Test]
    public void ValidateUser_BadUserEmptyPassword_ReturnsFalse()
    {
      bool result = Provider.ValidateUser("TheKingOfFrance", String.Empty);
      Assert.IsFalse(result);
    }

    [Test]
    public void ValidateUser_BadUserBadPassword_ReturnsFalse()
    {
      //Act
      bool actual = Provider.ValidateUser("BadUser", "BadPass");
      //Assert
      Assert.IsFalse(actual);
    }

    [Test]
    public void ValidateUser_GoodUserBadPassword_ReturnsFalse()
    {
      var u = Users[0];
      //Act
      bool actual = Provider.ValidateUser(u.UserName, "BadPass");
      //Assert
      Assert.IsFalse(actual);
    }

    [Test]
    public void ValidateUser_GoodPassword_ReturnsTrue()
    {
      var u = Users[0];
      bool result = Provider.ValidateUser(u.UserName, u.Password);
      Assert.IsTrue(result);
    }
  }
}