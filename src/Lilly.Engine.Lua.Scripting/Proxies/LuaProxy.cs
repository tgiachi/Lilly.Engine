using System.Reflection;
using MoonSharp.Interpreter;

namespace Lilly.Engine.Lua.Scripting.Proxies;

public class LuaProxy<T> : DispatchProxy
{
    public Table Table { get; set; }

    protected override object Invoke(MethodInfo targetMethod, object[] args)
    {
        var fn = Table.Get(targetMethod.Name);

        if (fn.Type != DataType.Function)
        {
            throw new MissingMethodException(targetMethod.Name);
        }

        var dynArgs = args
                      .Select(a => DynValue.FromObject(null, a))
                      .ToArray();
        var result = fn.Function.Call(dynArgs);

        return result.ToObject(targetMethod.ReturnType);
    }
}
