using System.Reflection;

[assembly: AssemblyVersion(Product.Version)]
[assembly: AssemblyFileVersion(Product.Version)]
[assembly: AssemblyInformationalVersion("0.9.0-beta")]

internal static class Product
{
    public const string Version = "0.9.0.0";
}

