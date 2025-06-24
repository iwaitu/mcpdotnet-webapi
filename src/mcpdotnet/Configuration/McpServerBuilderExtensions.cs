using McpDotNet.Server;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using System.Reflection;

namespace McpDotNet.Configuration
{
    /// <summary>
    /// Registers API functions defined in controller classes within the specified assembly to the MCP server builder.
    /// </summary>
    public static class McpServerBuilderExtensions
    {
        /// <summary>
        /// Registers API functions defined in controller classes within the specified assembly to the MCP server
        /// builder.
        /// </summary>
        /// <remarks>This method scans the specified assembly for classes whose names end with
        /// "Controller" and are public, non-abstract, and instantiable. It identifies public instance methods within
        /// these classes that return an <see cref="IActionResult"/> and are decorated with the <see
        /// cref="McpToolAttribute"/>. These methods are registered as API functions in the MCP server
        /// builder.</remarks>
        /// <param name="builder">The <see cref="IMcpServerBuilder"/> to which the API functions will be added.</param>
        /// <param name="assembly">The assembly to scan for controller classes containing API functions. If <see langword="null"/>, the calling
        /// assembly is used.</param>
        /// <returns>The <see cref="IMcpServerBuilder"/> instance with the registered API functions.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="builder"/> is <see langword="null"/>.</exception>
        public static IMcpServerBuilder WithApiFunctions(this IMcpServerBuilder builder, Assembly? assembly = null)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            assembly ??= Assembly.GetCallingAssembly();

            List<AIFunction> list = new List<AIFunction>();
            Type[] types = assembly.GetTypes();
            foreach (Type type in types)
            {
                if (!type.IsClass || !type.Name.EndsWith("Controller", StringComparison.Ordinal))
                {
                    continue;
                }

                object? obj = null;
                try
                {
                    obj = Activator.CreateInstance(type);
                }
                catch (Exception)
                {
                    continue;
                }

                MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public);
                foreach (MethodInfo methodInfo in methods)
                {
                    if (!(methodInfo.DeclaringType != type) && typeof(IActionResult).IsAssignableFrom(methodInfo.ReturnType) && methodInfo.GetCustomAttribute<McpToolAttribute>() != null)
                    {
                        AIFunction item = AIFunctionFactory.Create(methodInfo, obj, new AIFunctionFactoryOptions
                        {
                            Name = type.Name + "." + methodInfo.Name
                        });
                        list.Add(item);
                    }
                }
            }

            return builder.WithTools(list);
        }
    }
}
