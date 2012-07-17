using System;
using System.Collections.Specialized;
using System.Configuration.Provider;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Web.Security;
using Xtensive.Core;
using Xtensive.Disposing;
using Xtensive.Orm.Model;
using Xtensive.Orm.Services;
using Xtensive.Orm.Web;

namespace Xtensive.Orm.Security.Web
{
  public class OrmMembershipProvider : MembershipProvider
  {
    internal Configuration Configuration { get; private set; }

    private Type rootPrincipalType;

    private Domain Domain
    {
      get { return SessionManager.Domain; }
    }

    private IQueryable<MembershipPrincipal> GetQueryRoot(Session session)
    {
      return session.Query.All(rootPrincipalType) as IQueryable<MembershipPrincipal>;
    }

    #region Settings

    public override string ApplicationName
    {
      get { return Configuration.ApplicationName; }
      set { Configuration.ApplicationName = value; }
    }

    public override int MinRequiredPasswordLength
    {
      get { return Configuration.MinRequiredPasswordLength; }
    }

    public override int MinRequiredNonAlphanumericCharacters
    {
      get { return Configuration.MinRequiredNonAlphanumericCharacters; }
    }

    public override int MaxInvalidPasswordAttempts
    {
      get { return Configuration.MaxInvalidPasswordAttempts; }
    }

    public override int PasswordAttemptWindow
    {
      get { return Configuration.PasswordAttemptWindow; }
    }

    public override MembershipPasswordFormat PasswordFormat
    {
      get { return Configuration.PasswordFormat; }
    }

    public override string PasswordStrengthRegularExpression
    {
      get { return Configuration.PasswordStrengthRegularExpression; }
    }

    public override bool EnablePasswordReset
    {
      get { return Configuration.EnablePasswordReset; }
    }

    public override bool EnablePasswordRetrieval
    {
      get { return Configuration.EnablePasswordRetrieval; }
    }

    public override bool RequiresQuestionAndAnswer
    {
      get { return Configuration.RequiresQuestionAndAnswer; }
    }

    public override bool RequiresUniqueEmail
    {
      get { return Configuration.RequiresUniqueEmail; }
    }

    #endregion

    public override void Initialize(string name, NameValueCollection settings)
    {
      if (String.IsNullOrEmpty(name))
        name = Wellknown.MembershipProviderName;

      base.Initialize(name, settings);
      Configuration = new Configuration(settings);

      rootPrincipalType = Domain.Model.Hierarchies
        .Select(h => h.Root.UnderlyingType)
        .FirstOrDefault(t => typeof (MembershipPrincipal).IsAssignableFrom(t));

      if (rootPrincipalType == null)
        throw new InvalidOperationException("No descendants of MembershipPrincipal type are found in domain model");
    }

    public override bool ChangePassword(string username, string oldPassword, string newPassword)
    {
      CheckParameter(ref username, "username");
      CheckParameter(ref oldPassword, "oldPassword");
      CheckPassword(ref newPassword, "newPassword");

      var args = new ValidatePasswordEventArgs(username, newPassword, false);
      OnValidatingPassword(args);

      if (args.Cancel)
        if (args.FailureInformation!=null)
          throw args.FailureInformation;
        else
          throw new ArgumentException("Custom password validation failure", "newPassword");

      using (var session = Domain.OpenSession())
      using (var t = session.OpenTransaction()) {
        var user = GetUserByName(session, username);
        if (user==null)
          return false;

        var service = session.GetHashingService();
        if (!service.VerifyHash(oldPassword, user.PasswordHash))
          throw new MembershipPasswordException();

        user.SetPassword(newPassword);
        t.Complete();
      }
      return true;
    }

    public override bool ChangePasswordQuestionAndAnswer(string username, string password, string newPasswordQuestion, string newPasswordAnswer)
    {
      CheckParameter(ref username, "username");
      CheckParameter(ref password, "password");
      CheckParameter(ref newPasswordQuestion, "newPasswordQuestion");
      CheckParameter(ref newPasswordAnswer, "newPasswordAnswer");

      using (var session = Domain.OpenSession())
      using (var t = session.OpenTransaction()) {
        var user = session.Authenticate(username, password) as MembershipPrincipal;
        if (user==null)
          return false;

        user.PasswordQuestion = newPasswordQuestion;
        var service = session.GetHashingService();
        user.PasswordAnswerHash = service.ComputeHash(newPasswordAnswer);
        t.Complete();
      }
      return true;
    }

    public override MembershipUser CreateUser(string username, string password, string email, string passwordQuestion, string passwordAnswer, bool isApproved, object providerUserKey, out MembershipCreateStatus status)
    {
      status = MembershipCreateStatus.InvalidUserName;
      if (!CheckParameter(ref username, "username", false))
        return null;

      status = MembershipCreateStatus.InvalidEmail;
      if (!CheckParameter(ref email, "email", false))
        return null;

      status = MembershipCreateStatus.InvalidPassword;
      if (!CheckPassword(ref password, "password", false))
        return null;

      if (RequiresQuestionAndAnswer) {
        status = MembershipCreateStatus.InvalidQuestion;
        if (!CheckParameter(ref passwordQuestion, "passwordQuestion", false))
          return null;

        status = MembershipCreateStatus.InvalidAnswer;
        if (!CheckParameter(ref passwordAnswer, "passwordAnswer", false))
          return null;
      }

      var args = new ValidatePasswordEventArgs(username, password, false);
      OnValidatingPassword(args);

      if (args.Cancel)
        if (args.FailureInformation!=null)
          throw args.FailureInformation;
        else
          throw new ArgumentException("Custom password validation failure", "password");

      MembershipUser result;
      using (var session = Domain.OpenSession())
      using (var t = session.OpenTransaction()) {

        var user = GetUserByName(session, username);
        if (user != null) {
          status = MembershipCreateStatus.DuplicateUserName;
          return null;
        }

        user = GetUserByEmail(session, email, false);
        if (user != null) {
          status = MembershipCreateStatus.DuplicateEmail;
          return null;
        }

        using (var validation = session.DisableValidation()) {
          var accessor = session.Services.Get<DirectEntityAccessor>();
          user = (MembershipPrincipal)accessor.CreateEntity(rootPrincipalType);

          DateTime utcNow = DateTime.UtcNow;

          user.Name = username;
          user.Email = email;
          user.IsApproved = isApproved;
          user.CreationDate = utcNow;
          user.LastActivityDate = utcNow;
          user.LastPasswordChangedDate = utcNow;

          user.SetPassword(password);

          if (RequiresQuestionAndAnswer) {
            user.PasswordQuestion = passwordQuestion;
            var service = session.GetHashingService();
            user.PasswordAnswerHash = service.ComputeHash(passwordAnswer);
          }
          validation.Complete();
        }
        session.Validate();
        result = ToMembershipUser(user);
        t.Complete();
      }
      status = MembershipCreateStatus.Success;
      return result;
    }

    public override bool DeleteUser(string username, bool deleteAllRelatedData)
    {
      CheckParameter(ref username, "username");

      using (var session = Domain.OpenSession())
      using (var t = session.OpenTransaction()) {
        
        var user = GetUserByName(session, username);

        if (user==null)
          return false;

        user.Remove();
        t.Complete();
      }
      return true;
    }

    public override MembershipUserCollection FindUsersByEmail(string emailToMatch, int pageIndex, int pageSize, out int totalRecords)
    {
      CheckParameter(ref emailToMatch, "emailToMatch");

      return FindUsers(w => w.Email.ToLower() == emailToMatch, pageIndex, pageSize, out totalRecords);
    }

    public override MembershipUserCollection FindUsersByName(string usernameToMatch, int pageIndex, int pageSize, out int totalRecords)
    {
      CheckParameter(ref usernameToMatch, "usernameToMatch");

      return FindUsers(w => w.Name == usernameToMatch, pageIndex, pageSize, out totalRecords);
    }

    public override MembershipUserCollection GetAllUsers(int pageIndex, int pageSize, out int totalRecords)
    {
      return FindUsers(null, pageIndex, pageSize, out totalRecords);
    }

    public override int GetNumberOfUsersOnline()
    {
      var timeWindow = Membership.UserIsOnlineTimeWindow;
      var boundary = DateTime.Now.AddMinutes(-1.0 * timeWindow);

      using (var session = Domain.OpenSession())
      using (var t = session.OpenTransaction())
        return GetQueryRoot(session)
          .Count(m => m.LastActivityDate > boundary);
    }

    public override string GetPassword(string username, string answer)
    {
      if (!EnablePasswordRetrieval)
        throw new NotSupportedException("Password retrieval is not supported");

      CheckParameter(ref username, "username");

      if (PasswordFormat == MembershipPasswordFormat.Hashed)
        throw new ProviderException("Unable to retrieve password as hashed password format is used");

      using (var session = Domain.OpenSession()) {
        using (var t = session.OpenTransaction()) {

          var user = GetUserByName(session, username);

          if (!RequiresQuestionAndAnswer)
            return user.PasswordHash;

          var service = session.GetHashingService();
          if (service.VerifyHash(answer, user.PasswordAnswerHash))
            return user.PasswordHash;

          throw new MembershipPasswordException("Invalid answer");
        }
      }
    }

    public override MembershipUser GetUser(string username, bool userIsOnline)
    {
      CheckParameter(ref username, "username");

      using (var session = Domain.OpenSession()) {
        using (var t = session.OpenTransaction()) {

          var user = GetUserByName(session, username);
          if (user == null)
            return null;

          var result = ToMembershipUser(user);
          if (userIsOnline) {
            user.LastActivityDate = DateTime.UtcNow;
            t.Complete();
          }
          return result;
        }
      }
    }

    public override MembershipUser GetUser(object providerUserKey, bool userIsOnline)
    {
      throw new NotImplementedException();
//      CheckParameter(providerUserKey, "providerUserKey");
//
//      using (var session = Domain.OpenSession()) {
//        using (var t = session.OpenTransaction()) {
//
//          var user = GetUser(session, providerUserKey);
//          if (user == null)
//            return null;
//
//          var result = ToMembershipUser(user);
//          if (userIsOnline) {
//            user.LastActivityDate = DateTime.UtcNow;
//            t.Complete();
//          }
//          return result;
//        }
//      }
   }

    public override string GetUserNameByEmail(string email)
    {
      CheckParameter(ref email, "email");

      using (var session = Domain.OpenSession()) {
        using (var t = session.OpenTransaction()) {
          var user = GetUserByEmail(session, email, true);
          if (user != null)
            return user.Name;
        }
      }
      return null;
    }

    public override string ResetPassword(string username, string answer)
    {
      if (!EnablePasswordReset)
        throw new NotSupportedException("Password resetting is not supported");

      CheckParameter(ref username, "username");
      if (!CheckParameter(ref answer, "answer", false) && RequiresQuestionAndAnswer)
        throw new ArgumentException("answer");

      string password = GeneratePassword();
      var args = new ValidatePasswordEventArgs(username, password, false);
      OnValidatingPassword(args);

      if (args.Cancel)
        if (args.FailureInformation!=null)
          throw args.FailureInformation;
        else
          throw new ArgumentException("Custom password validation failure", "password");

      using (var session = Domain.OpenSession())
      using (var t = session.OpenTransaction()) {
        var user = GetUserByName(session, username);
        if (user==null)
            throw new ProviderException(string.Format("User with name {0} is not found", username));

        if (RequiresQuestionAndAnswer) {
          var service = session.GetHashingService();
          if (!service.VerifyHash(answer, user.PasswordAnswerHash))
            throw new MembershipPasswordException("Password answer is invalid");
        }

        user.SetPassword(password);
        t.Complete();
        return password;
      }
    }

    public override bool UnlockUser(string username)
    {
      if (!CheckParameter(ref username, "username", false))
        return false;

      using (var session = Domain.OpenSession())
      using (var t = session.OpenTransaction()) {
        var user = GetUserByName(session, username);
        if (user==null)
          return false;

        user.IsLockedOut = false;
        t.Complete();
        return true;
      }
    }

    public override void UpdateUser(MembershipUser user)
    {
      CheckParameter(user, "user");
      var username = user.UserName;
      CheckParameter(ref username, "username");
      var email = user.Email;
      CheckParameter(ref email, "e-mail");

      try {
        using (var session = Domain.OpenSession())
        using (var t = session.OpenTransaction()) {
          var target = GetUserByName(session, username);
          if (target==null)
            throw new ProviderException(string.Format("User with name {0} is not found", username));

          target.Email = user.Email;
          target.Comment = user.Comment;
          target.IsApproved = user.IsApproved;
          target.IsLockedOut = user.IsLockedOut;
          target.LastActivityDate = user.LastActivityDate;
          target.LastLockOutDate = user.LastLockoutDate;
          target.LastLoginDate = user.LastLoginDate;
          target.LastPasswordChangedDate = user.LastPasswordChangedDate;
          target.PasswordQuestion = user.PasswordQuestion;

          t.Complete();
        }
      }
      catch (StorageException exception) {
        throw new ProviderException("Unable to update user. See inner exception for details", exception);
      }
    }

    public override bool ValidateUser(string username, string password)
    {
      if (!CheckParameter(ref username, "username", false))
        return false;
      if (!CheckParameter(ref password, "password", false))
        return false;

      using (var session = Domain.OpenSession())
      using (var t = session.OpenTransaction()) {
        var user = session.Authenticate(username, password) as MembershipPrincipal;
        if (user==null)
          return false;

        if (!user.IsApproved)
          return false;

        user.LastActivityDate = DateTime.UtcNow;
        t.Complete();
      }
      return true;
    }

    private MembershipPrincipal GetUserByName(Session session, string username)
    {
      return GetQueryRoot(session)
        .SingleOrDefault(s => s.Name==username);
    }

    private MembershipPrincipal GetUserByEmail(Session session, string email, bool throwOnDuplicate)
    {
      var users = GetQueryRoot(session)
        .Where(s => s.Email==email)
        .Take(2)
        .ToList();

      if (throwOnDuplicate && RequiresUniqueEmail && users.Count > 1)
        throw new ProviderException(string.Format("More that one user with '{0}' is found", email));

      return users.FirstOrDefault();
    }

    private MembershipUserCollection FindUsers(Expression<Func<MembershipPrincipal, bool>> condition, int pageIndex, int pageSize, out int totalRecords)
    {
      var result = new MembershipUserCollection();

      using (var session = Domain.OpenSession()) {
        using (var t = session.OpenTransaction()) {
          
          var users = GetQueryRoot(session);
          if (condition != null)
            users = users.Where(condition);

          totalRecords = users.Count();

          var skipNumber = pageIndex * pageSize;
          users = users.Skip(skipNumber)
            .Take(pageSize);
          foreach (var user in users)
            result.Add(ToMembershipUser(user));
        }
      }
      return result;
    }

    private MembershipUser ToMembershipUser(MembershipPrincipal principal)
    {
      var user = new MembershipUser(
        Name,
        principal.Name,
        principal.Key,
        principal.Email,
        principal.PasswordQuestion,
        principal.Comment,
        principal.IsApproved,
        principal.IsLockedOut,
        principal.CreationDate,
        principal.LastLoginDate,
        principal.LastActivityDate,
        principal.LastPasswordChangedDate,
        principal.LastLockOutDate);

      return user;
    }

    protected virtual string GeneratePassword() 
    {
      return Membership.GeneratePassword(MinRequiredPasswordLength, MinRequiredNonAlphanumericCharacters);
    }

    #region Validation

    private static void CheckParameter(object value, string parameterName)
    {
      ArgumentValidator.EnsureArgumentNotNull(value, parameterName);
    }

    private static bool CheckParameter(ref string value, string parameterName, bool throwOnError = true)
    {
      if (string.IsNullOrEmpty(value)) {
        if (throwOnError)
          throw new ArgumentNullException(parameterName);
        return false;
      }
      value = value.Trim();
      return true;
    }

    private bool CheckPassword(ref string value, string parameterName, bool throwOnError = true)
    {
      if (!CheckParameter(ref value, parameterName, throwOnError))
        return false;

      if (MinRequiredPasswordLength > 0) {
        if (value.Length < MinRequiredPasswordLength)
          throw new ArgumentException(
            String.Format("New password is too short. Min length of {0} symbols is required", MinRequiredPasswordLength), parameterName);
      }

      if (MinRequiredNonAlphanumericCharacters > 0) {
        int count = 0;

        for (int i = 0; i < value.Length; i++)
          if (!Char.IsLetterOrDigit(value, i))
            count++;

        if (count < MinRequiredNonAlphanumericCharacters)
          throw new ArgumentException(
            String.Format("Password needs more non alphanumeric chars. Min number of {0} such chars is required", MinRequiredNonAlphanumericCharacters),
            parameterName);
      }

      if (!String.IsNullOrEmpty(PasswordStrengthRegularExpression))
        if (!Regex.IsMatch(value, PasswordStrengthRegularExpression))
          throw new ArgumentException("Password doens't meet regular expression", parameterName);

      return true;
    }

    #endregion
  }
}