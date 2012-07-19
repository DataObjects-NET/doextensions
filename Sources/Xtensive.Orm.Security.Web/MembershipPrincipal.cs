using System;
using System.Web.Security;
using Xtensive.Orm.Validation;

namespace Xtensive.Orm.Security.Web
{
  /// <summary>
  /// Persistent implementation of <see cref="MembershipUser"/> type.
  /// </summary>
  public abstract class MembershipPrincipal : GenericPrincipal
  {
    /// <summary>
    /// Gets or sets the e-mail address for the membership user.
    /// </summary>
    [Field]
    [EmailConstraint]
    public virtual string Email { get; set; }

    /// <summary>
    /// Gets or sets application-specific information for the membership user.
    /// </summary>
    [Field]
    public string Comment { get; set; }

    /// <summary>
    /// Gets the password question for the membership user.
    /// </summary>
    [Field]
    [NotEmptyConstraint]
    public string PasswordQuestion { get; set; }

    /// <summary>
    /// Gets or sets the hash of password answer.
    /// </summary>
    [Field]
    [NotEmptyConstraint]
    public string PasswordAnswerHash { get; set; }

    /// <summary>
    /// Gets or sets whether the membership user can be authenticated.
    /// </summary>
    [Field]
    public bool IsApproved { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the membership user was last authenticated or accessed the application.
    /// </summary>
    [Field]
    public DateTime LastActivityDate { get; set; }

    /// <summary>
    /// Gets the date and time when the membership user's password was last updated.
    /// </summary>
    [Field]
    public DateTime LastPasswordChangedDate { get; set; }

    /// <summary>
    /// Gets the date and time when the user was added to the membership data store.
    /// </summary>
    [Field]
    public DateTime CreationDate { get; set; }

    /// <summary>
    /// Gets a value indicating whether the membership user is locked out and unable to be validated.
    /// </summary>
    [Field]
    public bool IsLockedOut { get; set; }

    /// <summary>
    /// Gets the most recent date and time that the membership user was locked out.
    /// </summary>
    [Field]
    public DateTime LastLockOutDate { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the user was last authenticated.
    /// </summary>
    [Field]
    public DateTime LastLoginDate { get; set; }

    /// <summary>
    /// Gets or sets number of times the user has failed to enter the correct password within the FailedPasswordAttemptWindow 
    /// Range: FailedPasswordAttemptWindowStart to FailedPasswordAttemptWindowStart + passwordAttemptWindow
    /// </summary>
    [Field]
    public int FailedPasswordAttemptCount { get; set; }

    /// <summary>
    /// Gets or sets the time of the last failed password attempt or
    /// the time of the last failed password attempt after the last fail +
    /// passwordAttemptWindow (set on web.config) value in minutes
    /// </summary>
    [Field]
    public DateTime FailedPasswordAttemptWindowStart { get; set; }

    /// <summary>
    /// Gets or sets number of times the user has failed to enter the correct answer
    /// within the FailedPasswordAnswerAttemptWindow
    /// Range: FailedPasswordAnswerAttemptWindowStart to FailedPasswordAnswerAttemptWindowStart + passwordAttemptWindow
    /// </summary>
    [Field]
    public int FailedPasswordAnswerAttemptCount { get; set; }

    /// <summary>
    /// Gets or sets the time of the last failed answer attempt or
    /// the time of the last failed answer attempt after the last fail +
    /// passwordAttemptWindow (set on web.config) value in minutes
    /// </summary>
    [Field]
    public DateTime FailedPasswordAnswerAttemptWindowStart { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MembershipPrincipal"/> class.
    /// </summary>
    protected MembershipPrincipal()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MembershipPrincipal"/> class.
    /// </summary>
    /// <param name="session">The session.</param>
    protected MembershipPrincipal(Session session)
      : base(session)
    {
    }
  }
}