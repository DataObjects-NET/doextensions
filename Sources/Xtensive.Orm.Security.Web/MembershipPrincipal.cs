using System;
using System.Configuration.Provider;
using System.Security.Cryptography;
using Xtensive.Orm.Security.Configuration;
using Xtensive.Orm.Validation;

namespace Xtensive.Orm.Security.Web
{
  // Determined these required fields for a custom MembershipUser implementation
  // http://msdn.microsoft.com/en-us/library/za59c2aa

  public abstract class MembershipPrincipal : GenericPrincipal
  {
    [Field]
    [EmailConstraint]
    public virtual string Email { get; set; }

    //General comment field for the user for administrative notes
    [Field]
    public string Comment { get; set; }

    [Field]
    [NotEmptyConstraint]
    public string PasswordQuestion { get; set; }

    //Use same implementation as SetPassword method
    [Field]
    [NotEmptyConstraint]
    public string PasswordAnswerHash { get; set; }

    //IsApproved value for a membership user is checked during the call to ValidateUser by the MembershipProvider
    //If the IsApproved property is set to false, the ValidateUser method returns false even if the supplied user name and password are correct.
    [Field]
    public bool IsApproved { get; set; }

    //date and time when the membership user was last authenticated or accessed the application.
    //Should be updated on entity access, if to fully conform I think, but may be too much effort to reward?
    [Field]
    public DateTime LastActivityDate { get; set; }

    //The datetime the user last changed their password
    [Field]
    public DateTime LastPasswordChangedDate { get; set; }

    //The datetime the user was created
    [Field]
    public DateTime CreationDate { get; set; }

    //This field should prevent authentication from being allowed
    [Field]
    public bool IsLockedOut { get; set; }

    //Record of the last time the user was lockedout
    [Field]
    public DateTime LastLockOutDate { get; set; }

    //Record of the last time the user logged in
    [Field]
    public DateTime LastLoginDate { get; set; }

    //Number of times the user has failed to enter the correct password
    // within the FailedPasswordAttemptWindow
    // Range: FailedPasswordAttemptWindowStart to FailedPasswordAttemptWindowStart + passwordAttemptWindow
    [Field]
    public int FailedPasswordAttemptCount { get; set; }

    //The time of the last failed password attempt or
    // the time of the last failed password attempt after the last fail +
    //passwordAttemptWindow (set on web.config) value in minutes
    [Field]
    public DateTime FailedPasswordAttemptWindowStart { get; set; }

    //Number of times the user has failed to enter the correct answer
    // within the FailedPasswordAnswerAttemptWindow
    // Range: FailedPasswordAnswerAttemptWindowStart to FailedPasswordAnswerAttemptWindowStart + passwordAttemptWindow
    [Field]
    public int FailedPasswordAnswerAttemptCount { get; set; }

    //The time of the last failed answer attempt or
    // the time of the last failed answer attempt after the last fail +
    //passwordAttemptWindow (set on web.config) value in minutes
    [Field]
    public DateTime FailedPasswordAnswerAttemptWindowStart { get; set; }

    protected MembershipPrincipal()
    {
    }

    protected MembershipPrincipal(Session session)
      : base(session)
    {
    }
  }
}