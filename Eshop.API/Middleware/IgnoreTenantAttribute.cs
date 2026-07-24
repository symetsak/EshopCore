namespace Eshop.API.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class IgnoreTenantAttribute : Attribute
    {
    }
}