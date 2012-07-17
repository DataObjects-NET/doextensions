using System;
using System.Collections.Specialized;
using System.Web.Security;
using Xtensive.Orm.Security.Configuration;

namespace Xtensive.Orm.Security.Web
{
  internal class Configuration
  {
    //Defaults
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

//
//    public static IMembershipPrincipal GetPrincipalByEmail(string email)
//    {
//      IQueryable<IMembershipPrincipal> principals = Query.All<IMembershipPrincipal>()
//        .Where(w => w.Email.ToLower()==email.ToLower());
//
//      if (principals.Count() > 1)
//        throw new ProviderException("GetPrincipalByEmail(string email) failed.  Email found is not unique");
//
//      return principals.Any() ? principals.First() : null;
//    }
    private static int GetValue(NameValueCollection settings, string name, int defaultValue)
    {
      var s = settings[name];
      if (String.IsNullOrEmpty(s))
        return defaultValue;
      
      int value;
      return int.TryParse(s, out value) ? value : defaultValue;
    }

    private static bool GetValue(NameValueCollection settings, string name, bool defaultValue)
    {
      var s = settings[name];
      if (String.IsNullOrEmpty(s))
        return defaultValue;
      
      bool value;
      return bool.TryParse(s, out value) ? value : defaultValue;
    }

    private static string GetValue(NameValueCollection settings, string name, string defaultValue)
    {
      var s = settings[name];
      return String.IsNullOrEmpty(s) ? defaultValue : s;
    }

    public Configuration(NameValueCollection settings)
    {
      ApplicationName                         = GetValue(settings, "applicationName", "default");
      EnablePasswordReset                     = GetValue(settings, "enablePasswordReset", true);
      EnablePasswordRetrieval                 = GetValue(settings, "enablePasswordRetrieval", true);
      MaxInvalidPasswordAttempts              = GetValue(settings, "maxInvalidPasswordAttempts", 5);
      PasswordAttemptWindow                   = GetValue(settings, "passwordAttemptWindow", 10);
      PasswordStrengthRegularExpression       = GetValue(settings, "passwordStrengthRegularExpression", null);
      MinRequiredNonAlphanumericCharacters    = GetValue(settings, "minRequiredNonAlphanumericCharacters", 1);
      MinRequiredPasswordLength               = GetValue(settings, "minRequiredPasswordLength", 8);
      MaxRequiredPasswordLength               = GetValue(settings, "maxRequiredPasswordLength", 16);
      RequiresQuestionAndAnswer               = GetValue(settings, "requiresQuestionAndAnswer", false);
      RequiresUniqueEmail                     = GetValue(settings, "requiresUniqueEmail", true);

//      if (!String.IsNullOrEmpty(settings["passwordFormat"]))
//        throw new ProviderException("passwordFormat must be defined in Xtensive.Orm.Security config section");

      //Get Xtensive.Orm.Security configuration
      var securityConfig = SecurityConfiguration.Load();
      //If the HashingServiceName is not "plain" then it must be a hash type
      PasswordFormat = securityConfig.HashingServiceName=="plain" ? MembershipPasswordFormat.Clear : MembershipPasswordFormat.Hashed;
    }
  }
}