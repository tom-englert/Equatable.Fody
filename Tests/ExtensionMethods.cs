namespace Tests
{
    using System;
    using System.IO;
    using System.Reflection;

    public static class ExtensionMethods
    {
        public static dynamic GetInstance(this Assembly assembly, string className, params object[] args)
        {
            try
            {
                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

                var type = assembly.GetType(className, true);

                return Activator.CreateInstance(type, args);
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            }
        }

        private static Assembly? CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var name = new AssemblyName(args.Name);
            var location = Path.GetDirectoryName(args.RequestingAssembly.Location);
            var fullName = Path.Combine(location, name.Name + ".dll");

            if (File.Exists(fullName))
                return Assembly.LoadFrom(fullName);
            
            return null;
        }
    }
}
