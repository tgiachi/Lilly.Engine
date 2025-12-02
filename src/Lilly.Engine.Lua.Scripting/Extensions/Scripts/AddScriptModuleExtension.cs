using DryIoc;
using Lilly.Engine.Core.Data.Scripts.Container;
using Lilly.Engine.Core.Extensions.Container;
using MoonSharp.Interpreter;

namespace Lilly.Engine.Lua.Scripting.Extensions.Scripts;

/// <summary>
/// Extension methods for registering Lua script modules in the dependency injection container.
/// </summary>
public static class AddScriptModuleExtension
{
    /// <summary>
    /// Registers a user data type with the container for Lua scripting.
    /// </summary>
    public static IContainer RegisterLuaUserData(this IContainer container, Type userDataType)
    {
        if (userDataType == null)
        {
            throw new ArgumentNullException(nameof(userDataType), "User data type cannot be null.");
        }

        container.AddToRegisterTypedList(new ScriptUserData { UserType = userDataType });

        return container;
    }

    /// <summary>
    /// Registers a user data type with the container for Lua scripting using generics.
    /// </summary>
    public static IContainer RegisterLuaUserData<TUserData>(this IContainer container)
    {
        UserData.RegisterType<TUserData>();

        return container.RegisterLuaUserData(typeof(TUserData));
    }

    /// <summary>
    /// Registers a Lua script module type with the container.
    /// </summary>
    /// <param name="container">The dependency injection container.</param>
    /// <param name="scriptModule">The type of the script module to register.</param>
    /// <returns>The container for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when scriptModule is null.</exception>
    public static IContainer RegisterScriptModule(this IContainer container, Type scriptModule)
    {
        if (scriptModule == null)
        {
            throw new ArgumentNullException(nameof(scriptModule), "Script module type cannot be null.");
        }

        container.AddToRegisterTypedList(new ScriptModuleData(scriptModule));

        container.Register(scriptModule, Reuse.Singleton);

        return container;
    }

    /// <summary>
    /// Registers a Lua script module type with the container using a generic type parameter.
    /// </summary>
    /// <typeparam name="TScriptModule">The type of the script module to register.</typeparam>
    /// <param name="container">The dependency injection container.</param>
    /// <returns>The container for method chaining.</returns>
    public static IContainer RegisterScriptModule<TScriptModule>(this IContainer container) where TScriptModule : class
        => container.RegisterScriptModule(typeof(TScriptModule));
}
