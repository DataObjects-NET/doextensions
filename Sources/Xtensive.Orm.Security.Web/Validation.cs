using System;
using System.Text.RegularExpressions;
using Xtensive.Core;

namespace Xtensive.Orm.Security.Web
{
  internal static class Validation
  {
    public static void CheckParameter(object value, string parameterName)
    {
      ArgumentValidator.EnsureArgumentNotNull(value, parameterName);
    }

    public static bool CheckParameter(ref string value, string parameterName, bool throwOnError = true)
    {
      if (string.IsNullOrEmpty(value)) {
        if (throwOnError)
          throw new ArgumentNullException(parameterName);
        return false;
      }
      value = value.Trim();
      return true;
    }

    public static bool CheckPassword(ref string value, string parameterName, Configuration configuration, bool throwOnError = true)
    {
      if (!CheckParameter(ref value, parameterName, throwOnError))
        return false;

      if (configuration.MinRequiredPasswordLength > 0) {
        if (value.Length < configuration.MinRequiredPasswordLength)
          throw new ArgumentException(
            string.Format("New password is too short. Min length of {0} symbols is required", configuration.MinRequiredPasswordLength), parameterName);
      }

      if (configuration.MinRequiredNonAlphanumericCharacters > 0) {
        int count = 0;

        for (int i = 0; i < value.Length; i++)
          if (!Char.IsLetterOrDigit(value, i))
            count++;

        if (count < configuration.MinRequiredNonAlphanumericCharacters)
          throw new ArgumentException(
            string.Format("Password needs more non alphanumeric chars. Min number of {0} such chars is required", configuration.MinRequiredNonAlphanumericCharacters),
            parameterName);
      }

      if (!string.IsNullOrEmpty(configuration.PasswordStrengthRegularExpression))
        if (!Regex.IsMatch(value, configuration.PasswordStrengthRegularExpression))
          throw new ArgumentException("Password doesn't meet regular expression", parameterName);

      return true;
    }
  }
}
