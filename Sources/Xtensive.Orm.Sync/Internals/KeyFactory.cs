using System.Reflection;
using Xtensive.Orm.Model;
using Xtensive.Tuples;

namespace Xtensive.Orm.Sync
{
  internal static class KeyFactory
  {
    private static readonly MethodInfo CreateKeyMethod;

    public static Key CreateKey(Domain domain, TypeInfo typeInfo, Tuple targetTuple)
    {
      return (Key) CreateKeyMethod.Invoke(null, new object[] {domain, typeInfo, TypeReferenceAccuracy.ExactType, targetTuple});
    }

    static KeyFactory()
    {
      CreateKeyMethod = typeof (Key).GetMethod("Create", BindingFlags.NonPublic | BindingFlags.Static, null,
        new[] {typeof (Domain), typeof (TypeInfo), typeof (TypeReferenceAccuracy), typeof (Tuple)}, null);
    }
  }
}