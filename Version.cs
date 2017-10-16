using System.Reflection;

[assembly: AssemblyVersion(Product.Version)]
[assembly: AssemblyFileVersion(Product.Version)]
[assembly: AssemblyInformationalVersion("0.9.1-beta")]

internal static class Product
{
    public const string Version = "0.9.1.0";
}

