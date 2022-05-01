using System;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
sealed class RpcAttribute : Attribute
{
    // // See the attribute guidelines at
    // //  http://go.microsoft.com/fwlink/?LinkId=85236
    public readonly string RpcName;

    // This is a positional argument
    public RpcAttribute(string rpcName = null)
    {
        RpcName = rpcName;
    }

    // public string PositionalString
    // {
    //     get { return positionalString; }
    // }

    // // This is a named argument
    // public int NamedInt { get; set; }
}