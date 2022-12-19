namespace OlympicWords.Filters;

/// <summary>
/// this is different from the rpc domain in the type of the possible domain types
/// to enforce this in compile time I use a generic type parameter but it's available for props
/// since dotnet7 and linux app service don't support it yet
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class ActionDomainAttribute : Attribute
{
    public Type Domain { get; }

    public ActionDomainAttribute(Type domain)
    {
        Domain = domain;
    }

    // private readonly Type domain;
    // public void OnResourceExecuting(ResourceExecutingContext context)
    // {
    //     var descriptor = (ControllerActionDescriptor)context.ActionDescriptor!;
    //     var methodDomain = MethodDomains.Actions[descriptor.MethodInfo];
    //     
    //     Console.WriteLine($"action {descriptor.ActionName} is being called");
    //     
    //     // if(domain == typeof(UserDomain.Stateless) && )
    //     
    //     if (domain != methodDomain)
    //     {
    //         context.Result = new BadRequestResult();
    //         Console.WriteLine($"action {descriptor.ActionName} has {domain} but called with {methodDomain}");
    //     }
    // }
    //
    // public void OnResourceExecuted(ResourceExecutedContext context)
    // {
    // }
    //
    // public ActionDomainAttribute(Type domain) : base(domain)
    // {
    //     this.domain = domain;
    // }
}