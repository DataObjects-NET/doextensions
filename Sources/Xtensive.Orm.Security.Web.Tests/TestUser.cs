using System;

namespace Xtensive.Orm.Security.Web.Tests
{
  public class TestUser
  {
    public string Comment;
    public DateTime CreationDate;
    public string Email;
    public bool IsApproved;
    public bool IsLockedOut;
    public DateTime LastActivityDate;
    public DateTime LastLockoutDate;
    public DateTime LastLoginDate;
    public DateTime LastPasswordChangedDate;
    public string Password;
    public string PasswordAnswer;
    public string PasswordQuestion;
    public string ProviderName;
    public object ProviderUserKey;
    public string UserName;
  }
}