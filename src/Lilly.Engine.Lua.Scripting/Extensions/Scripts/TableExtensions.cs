using System.Reflection;
using Lilly.Engine.Lua.Scripting.Proxies;
using MoonSharp.Interpreter;

namespace Lilly.Engine.Lua.Scripting.Extensions.Scripts;

public static class TableExtensions
{
    public static TInterface ToProxy<TInterface>(this Table table)
        where TInterface : class
    {
        var proxy = DispatchProxy.Create<TInterface, LuaProxy<TInterface>>();
        ((LuaProxy<TInterface>)(object)proxy).Table = table;
        return proxy;
    }

}
