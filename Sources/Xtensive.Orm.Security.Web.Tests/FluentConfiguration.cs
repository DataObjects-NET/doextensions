using System;
using System.Collections.Specialized;
using System.Linq.Expressions;
using System.Reflection;
using System.Web.Security;

namespace Xtensive.Orm.Security.Web.Tests
{
  public class FluentConfiguration
  {
    private readonly OrmMembershipProvider target;
    private readonly NameValueCollection values = new NameValueCollection();

    public FluentConfiguration(OrmMembershipProvider target)
    {
      this.target = target;
    }
    public string ApplicationName { get; set; }

    public bool EnablePasswordReset { get; private set; }

    public bool EnablePasswordRetrieval { get; private set; }

    public bool RequiresQuestionAndAnswer { get; private set; }

    public bool RequiresUniqueEmail { get; private set; }

    public int MaxInvalidPasswordAttempts { get; private set; }

    public int PasswordAttemptWindow { get; private set; }

    public MembershipPasswordFormat PasswordFormat { get; private set; }

    public int MinRequiredNonAlphanumericCharacters { get; private set; }

    public int MinRequiredPasswordLength { get; private set; }

    public int MaxRequiredPasswordLength { get; private set; }

    public string PasswordStrengthRegularExpression { get; private set; }

    public FluentConfiguration With<T>(Expression<Func<FluentConfiguration, T>> expression, T value)
    {
      var me = expression.Body as MemberExpression;
      if (me == null)
        throw new ArgumentException();

      var property = me.Member as PropertyInfo;
      if (property == null)
        throw new ArgumentException();

      string propertyName = char.ToLower(property.Name[0]) + property.Name.Substring(1);
      values.Set(propertyName, value.ToString());

      return this;
    }

    public OrmMembershipProvider Initialize()
    {
      target.Initialize(string.Empty, values);

      return target;
    }
  }

  public static class OrmMembershipProviderExtensions
  {
    public static FluentConfiguration With<T>(this OrmMembershipProvider target, Expression<Func<FluentConfiguration, T>> expression, T value)
    {
      return new FluentConfiguration(target).With(expression, value);
    }
  }
}
