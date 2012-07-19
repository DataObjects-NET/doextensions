using System;
using System.Collections.Specialized;
using System.Configuration.Provider;
using System.Linq;
using System.Linq.Expressions;
using System.Web.Security;
using Xtensive.Orm.Services;
using Xtensive.Orm.Web;

namespace Xtensive.Orm.Security.Web
{
  /// <summary>
  /// Implementation of <see cref="MembershipProvider"/>.
  /// </summary>
  public class OrmMembershipProvider : MembershipProvider
  {
    private Type rootPrincipalType;
    private Configuration configuration;

    private Domain Domain
    {
      get { return SessionManager.Domain; }
    }

    private IQueryable<MembershipPrincipal> GetQueryRoot(Session session)
    {
      return session.Query.All(rootPrincipalType) as IQueryable<MembershipPrincipal>;
    }

    #region Settings

    /// <summary>
    /// The name of the application using the custom membership provider.
    /// </summary>
    /// <returns>The name of the application using the custom membership provider.</returns>
    public override string ApplicationName
    {
      get { return configuration.ApplicationName; }
      set { configuration.ApplicationName = value; }
    }

    /// <summary>
    /// Gets the minimum length required for a password.
    /// </summary>
    /// <returns>The minimum length required for a password. </returns>
    public override int MinRequiredPasswordLength
    {
      get { return configuration.MinRequiredPasswordLength; }
    }

    /// <summary>
    /// Gets the minimum number of special characters that must be present in a valid password.
    /// </summary>
    /// <returns>The minimum number of special characters that must be present in a valid password.</returns>
    public override int MinRequiredNonAlphanumericCharacters
    {
      get { return configuration.MinRequiredNonAlphanumericCharacters; }
    }

    /// <summary>
    /// Gets the number of invalid password or password-answer attempts allowed before the membership user is locked out.
    /// </summary>
    /// <returns>The number of invalid password or password-answer attempts allowed before the membership user is locked out.</returns>
    public override int MaxInvalidPasswordAttempts
    {
      get { return configuration.MaxInvalidPasswordAttempts; }
    }

    /// <summary>
    /// Gets the number of minutes in which a maximum number of invalid password or password-answer attempts are allowed before the membership user is locked out.
    /// </summary>
    /// <returns>The number of minutes in which a maximum number of invalid password or password-answer attempts are allowed before the membership user is locked out.</returns>
    public override int PasswordAttemptWindow
    {
      get { return configuration.PasswordAttemptWindow; }
    }

    /// <summary>
    /// Gets a value indicating the format for storing passwords in the membership data store.
    /// </summary>
    /// <returns>One of the <see cref="T:System.Web.Security.MembershipPasswordFormat"/> values indicating the format for storing passwords in the data store.</returns>
    public override MembershipPasswordFormat PasswordFormat
    {
      get { return configuration.PasswordFormat; }
    }

    /// <summary>
    /// Gets the regular expression used to evaluate a password.
    /// </summary>
    /// <returns>A regular expression used to evaluate a password.</returns>
    public override string PasswordStrengthRegularExpression
    {
      get { return configuration.PasswordStrengthRegularExpression; }
    }

    /// <summary>
    /// Indicates whether the membership provider is configured to allow users to reset their passwords.
    /// </summary>
    /// <returns>true if the membership provider supports password reset; otherwise, false. The default is true.</returns>
    public override bool EnablePasswordReset
    {
      get { return configuration.EnablePasswordReset; }
    }

    /// <summary>
    /// Indicates whether the membership provider is configured to allow users to retrieve their passwords.
    /// </summary>
    /// <returns>true if the membership provider is configured to support password retrieval; otherwise, false. The default is false.</returns>
    public override bool EnablePasswordRetrieval
    {
      get { return configuration.EnablePasswordRetrieval; }
    }

    /// <summary>
    /// Gets a value indicating whether the membership provider is configured to require the user to answer a password question for password reset and retrieval.
    /// </summary>
    /// <returns>true if a password answer is required for password reset and retrieval; otherwise, false. The default is true.</returns>
    public override bool RequiresQuestionAndAnswer
    {
      get { return configuration.RequiresQuestionAndAnswer; }
    }

    /// <summary>
    /// Gets a value indicating whether the membership provider is configured to require a unique e-mail address for each user name.
    /// </summary>
    /// <returns>true if the membership provider requires a unique e-mail address; otherwise, false. The default is true.</returns>
    public override bool RequiresUniqueEmail
    {
      get { return configuration.RequiresUniqueEmail; }
    }

    #endregion

    /// <summary>
    /// Initializes the specified name.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="settings">The settings.</param>
    public override void Initialize(string name, NameValueCollection settings)
    {
      if (String.IsNullOrEmpty(name))
        name = Wellknown.MembershipProviderName;

      base.Initialize(name, settings);
      configuration = new Configuration(settings);

      rootPrincipalType = Domain.Model.Hierarchies
        .Select(h => h.Root.UnderlyingType)
        .FirstOrDefault(t => typeof (MembershipPrincipal).IsAssignableFrom(t));

      if (rootPrincipalType == null)
        throw new InvalidOperationException("No descendants of MembershipPrincipal type are found in domain model");
    }

    /// <summary>
    /// Processes a request to update the password for a membership user.
    /// </summary>
    /// <param name="username">The user to update the password for.</param>
    /// <param name="oldPassword">The current password for the specified user.</param>
    /// <param name="newPassword">The new password for the specified user.</param>
    /// <returns>
    /// true if the password was updated successfully; otherwise, false.
    /// </returns>
    public override bool ChangePassword(string username, string oldPassword, string newPassword)
    {
      Validation.CheckParameter(ref username, "username");
      Validation.CheckParameter(ref oldPassword, "oldPassword");
      Validation.CheckPassword(ref newPassword, "newPassword", configuration);

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

    /// <summary>
    /// Processes a request to update the password question and answer for a membership user.
    /// </summary>
    /// <param name="username">The user to change the password question and answer for.</param>
    /// <param name="password">The password for the specified user.</param>
    /// <param name="newPasswordQuestion">The new password question for the specified user.</param>
    /// <param name="newPasswordAnswer">The new password answer for the specified user.</param>
    /// <returns>
    /// true if the password question and answer are updated successfully; otherwise, false.
    /// </returns>
    public override bool ChangePasswordQuestionAndAnswer(string username, string password, string newPasswordQuestion, string newPasswordAnswer)
    {
      Validation.CheckParameter(ref username, "username");
      Validation.CheckParameter(ref password, "password");
      Validation.CheckParameter(ref newPasswordQuestion, "newPasswordQuestion");
      Validation.CheckParameter(ref newPasswordAnswer, "newPasswordAnswer");

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

    /// <summary>
    /// Adds a new membership user to the data source.
    /// </summary>
    /// <param name="username">The user name for the new user.</param>
    /// <param name="password">The password for the new user.</param>
    /// <param name="email">The e-mail address for the new user.</param>
    /// <param name="passwordQuestion">The password question for the new user.</param>
    /// <param name="passwordAnswer">The password answer for the new user</param>
    /// <param name="isApproved">Whether or not the new user is approved to be validated.</param>
    /// <param name="providerUserKey">The unique identifier from the membership data source for the user.</param>
    /// <param name="status">A <see cref="T:System.Web.Security.MembershipCreateStatus"/> enumeration value indicating whether the user was created successfully.</param>
    /// <returns>
    /// A <see cref="T:System.Web.Security.MembershipUser"/> object populated with the information for the newly created user.
    /// </returns>
    public override MembershipUser CreateUser(string username, string password, string email, string passwordQuestion, string passwordAnswer, bool isApproved, object providerUserKey, out MembershipCreateStatus status)
    {
      status = MembershipCreateStatus.InvalidUserName;
      if (!Validation.CheckParameter(ref username, "username", false))
        return null;

      status = MembershipCreateStatus.InvalidEmail;
      if (!Validation.CheckParameter(ref email, "email", false))
        return null;

      status = MembershipCreateStatus.InvalidPassword;
      if (!Validation.CheckPassword(ref password, "password", configuration, false))
        return null;

      if (RequiresQuestionAndAnswer) {
        status = MembershipCreateStatus.InvalidQuestion;
        if (!Validation.CheckParameter(ref passwordQuestion, "passwordQuestion", false))
          return null;

        status = MembershipCreateStatus.InvalidAnswer;
        if (!Validation.CheckParameter(ref passwordAnswer, "passwordAnswer", false))
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

    /// <summary>
    /// Removes a user from the membership data source.
    /// </summary>
    /// <param name="username">The name of the user to delete.</param>
    /// <param name="deleteAllRelatedData">true to delete data related to the user from the database; false to leave data related to the user in the database.</param>
    /// <returns>
    /// true if the user was successfully deleted; otherwise, false.
    /// </returns>
    public override bool DeleteUser(string username, bool deleteAllRelatedData)
    {
      Validation.CheckParameter(ref username, "username");

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

    /// <summary>
    /// Gets a collection of membership users where the e-mail address contains the specified e-mail address to match.
    /// </summary>
    /// <param name="emailToMatch">The e-mail address to search for.</param>
    /// <param name="pageIndex">The index of the page of results to return. <paramref name="pageIndex"/> is zero-based.</param>
    /// <param name="pageSize">The size of the page of results to return.</param>
    /// <param name="totalRecords">The total number of matched users.</param>
    /// <returns>
    /// A <see cref="T:System.Web.Security.MembershipUserCollection"/> collection that contains a page of <paramref name="pageSize"/><see cref="T:System.Web.Security.MembershipUser"/> objects beginning at the page specified by <paramref name="pageIndex"/>.
    /// </returns>
    public override MembershipUserCollection FindUsersByEmail(string emailToMatch, int pageIndex, int pageSize, out int totalRecords)
    {
      Validation.CheckParameter(ref emailToMatch, "emailToMatch");

      return FindUsers(w => w.Email.ToLower() == emailToMatch, pageIndex, pageSize, out totalRecords);
    }

    /// <summary>
    /// Gets a collection of membership users where the user name contains the specified user name to match.
    /// </summary>
    /// <param name="usernameToMatch">The user name to search for.</param>
    /// <param name="pageIndex">The index of the page of results to return. <paramref name="pageIndex"/> is zero-based.</param>
    /// <param name="pageSize">The size of the page of results to return.</param>
    /// <param name="totalRecords">The total number of matched users.</param>
    /// <returns>
    /// A <see cref="T:System.Web.Security.MembershipUserCollection"/> collection that contains a page of <paramref name="pageSize"/><see cref="T:System.Web.Security.MembershipUser"/> objects beginning at the page specified by <paramref name="pageIndex"/>.
    /// </returns>
    public override MembershipUserCollection FindUsersByName(string usernameToMatch, int pageIndex, int pageSize, out int totalRecords)
    {
      Validation.CheckParameter(ref usernameToMatch, "usernameToMatch");

      return FindUsers(w => w.Name == usernameToMatch, pageIndex, pageSize, out totalRecords);
    }

    /// <summary>
    /// Gets a collection of all the users in the data source in pages of data.
    /// </summary>
    /// <param name="pageIndex">The index of the page of results to return. <paramref name="pageIndex"/> is zero-based.</param>
    /// <param name="pageSize">The size of the page of results to return.</param>
    /// <param name="totalRecords">The total number of matched users.</param>
    /// <returns>
    /// A <see cref="T:System.Web.Security.MembershipUserCollection"/> collection that contains a page of <paramref name="pageSize"/><see cref="T:System.Web.Security.MembershipUser"/> objects beginning at the page specified by <paramref name="pageIndex"/>.
    /// </returns>
    public override MembershipUserCollection GetAllUsers(int pageIndex, int pageSize, out int totalRecords)
    {
      return FindUsers(null, pageIndex, pageSize, out totalRecords);
    }

    /// <summary>
    /// Gets the number of users currently accessing the application.
    /// </summary>
    /// <returns>
    /// The number of users currently accessing the application.
    /// </returns>
    public override int GetNumberOfUsersOnline()
    {
      var timeWindow = Membership.UserIsOnlineTimeWindow;
      var boundary = DateTime.Now.AddMinutes(-1.0 * timeWindow);

      using (var session = Domain.OpenSession())
      using (var t = session.OpenTransaction())
        return GetQueryRoot(session)
          .Count(m => m.LastActivityDate > boundary);
    }

    /// <summary>
    /// Gets the password for the specified user name from the data source.
    /// </summary>
    /// <param name="username">The user to retrieve the password for.</param>
    /// <param name="answer">The password answer for the user.</param>
    /// <returns>
    /// The password for the specified user name.
    /// </returns>
    public override string GetPassword(string username, string answer)
    {
      if (!EnablePasswordRetrieval)
        throw new NotSupportedException("Password retrieval is not supported");

      Validation.CheckParameter(ref username, "username");

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

    /// <summary>
    /// Gets information from the data source for a user. Provides an option to update the last-activity date/time stamp for the user.
    /// </summary>
    /// <param name="username">The name of the user to get information for.</param>
    /// <param name="userIsOnline">true to update the last-activity date/time stamp for the user; false to return user information without updating the last-activity date/time stamp for the user.</param>
    /// <returns>
    /// A <see cref="T:System.Web.Security.MembershipUser"/> object populated with the specified user's information from the data source.
    /// </returns>
    public override MembershipUser GetUser(string username, bool userIsOnline)
    {
      Validation.CheckParameter(ref username, "username");

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

    /// <summary>
    /// Gets user information from the data source based on the unique identifier for the membership user. Provides an option to update the last-activity date/time stamp for the user.
    /// </summary>
    /// <param name="providerUserKey">The unique identifier for the membership user to get information for.</param>
    /// <param name="userIsOnline">true to update the last-activity date/time stamp for the user; false to return user information without updating the last-activity date/time stamp for the user.</param>
    /// <returns>
    /// A <see cref="T:System.Web.Security.MembershipUser"/> object populated with the specified user's information from the data source.
    /// </returns>
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

    /// <summary>
    /// Gets the user name associated with the specified e-mail address.
    /// </summary>
    /// <param name="email">The e-mail address to search for.</param>
    /// <returns>
    /// The user name associated with the specified e-mail address. If no match is found, return null.
    /// </returns>
    public override string GetUserNameByEmail(string email)
    {
      Validation.CheckParameter(ref email, "email");

      using (var session = Domain.OpenSession()) {
        using (var t = session.OpenTransaction()) {
          var user = GetUserByEmail(session, email, true);
          if (user != null)
            return user.Name;
        }
      }
      return null;
    }

    /// <summary>
    /// Resets a user's password to a new, automatically generated password.
    /// </summary>
    /// <param name="username">The user to reset the password for.</param>
    /// <param name="answer">The password answer for the specified user.</param>
    /// <returns>
    /// The new password for the specified user.
    /// </returns>
    public override string ResetPassword(string username, string answer)
    {
      if (!EnablePasswordReset)
        throw new NotSupportedException("Password resetting is not supported");

      Validation.CheckParameter(ref username, "username");
      if (!Validation.CheckParameter(ref answer, "answer", false) && RequiresQuestionAndAnswer)
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

    /// <summary>
    /// Unlocks the user.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <returns></returns>
    public override bool UnlockUser(string username)
    {
      if (!Validation.CheckParameter(ref username, "username", false))
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

    /// <summary>
    /// Updates information about a user in the data source.
    /// </summary>
    /// <param name="user">A <see cref="T:System.Web.Security.MembershipUser"/> object that represents the user to update and the updated information for the user.</param>
    public override void UpdateUser(MembershipUser user)
    {
      Validation.CheckParameter(user, "user");
      var username = user.UserName;
      Validation.CheckParameter(ref username, "username");
      var email = user.Email;
      Validation.CheckParameter(ref email, "e-mail");

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

    /// <summary>
    /// Verifies that the specified user name and password exist in the data source.
    /// </summary>
    /// <param name="username">The name of the user to validate.</param>
    /// <param name="password">The password for the specified user.</param>
    /// <returns>
    /// true if the specified username and password are valid; otherwise, false.
    /// </returns>
    public override bool ValidateUser(string username, string password)
    {
      if (!Validation.CheckParameter(ref username, "username", false))
        return false;
      if (!Validation.CheckParameter(ref password, "password", false))
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

    /// <summary>
    /// Generates the password.
    /// </summary>
    protected virtual string GeneratePassword() 
    {
      return Membership.GeneratePassword(MinRequiredPasswordLength, MinRequiredNonAlphanumericCharacters);
    }
  }
}