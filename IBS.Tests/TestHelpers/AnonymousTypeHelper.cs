namespace IBS.Tests.TestHelpers
{
    // ponytail: centralized reflection for anonymous type access from service returns
    // replace with a named DTO in IBS.DTOs if these properties change often
    internal static class AnonymousTypeHelper
    {
        public static T Get<T>(object obj, string propertyName)
            => (T)obj.GetType().GetProperty(propertyName)!.GetValue(obj)!;
    }
}
