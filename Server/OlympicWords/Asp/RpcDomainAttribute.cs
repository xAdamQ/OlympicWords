namespace OlympicWords.Services;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class RpcDomainAttribute : Attribute
{
    public Type Domain { get; }

    public RpcDomainAttribute(Type domain)
    {
        Domain = domain;
    }
}