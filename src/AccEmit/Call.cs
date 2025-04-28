using System.Reflection;
using System.Reflection.Emit;

namespace AccEmit;

// using Debug = System.Debug;
public static partial class Emit
{
	public static Action CallVoid(MethodInfo method) {
		if (method.ReturnType != typeof(void)) throw new ArgumentException(
			"method must return void", nameof(method));
		return CallDmd(null, typeof(void), method).Create<Action>();
	}

	public static Action<Args> CallVoid<Args>(MethodInfo method) => 
		CallDmd(typeof(Args), typeof(void), method).Create<Action<Args>>();

	public static Func<Ret> Call<Ret>(MethodInfo method) {
		if (method.ReturnType != typeof(Ret)) throw new ArgumentException(
			$"method does not return {typeof(Ret)}", nameof(method));
		return CallDmd(null, typeof(Ret), method).Create<Func<Ret>>();
	}

	public static Func<Args, Ret> Call<Args, Ret>(MethodInfo method) {
		if (method.ReturnType != typeof(Ret)) throw new ArgumentException(
			$"method does not return {typeof(Ret)}", nameof(method));
		return CallDmd(typeof(Args), typeof(Ret), method).Create<Func<Args, Ret>>();
	}

	public static Func<Args, object> CallBox<Args>(MethodInfo method) => 
		CallDmd(typeof(Args), typeof(object), method, 
			mapRet: il => il.Emit(OpCodes.Box, method.ReturnType))
		.Create<Func<Args, object>>();

	public static Func<object> CallBox(MethodInfo method) => 
		CallDmd(null, typeof(object), method,
			mapRet: il => il.Emit(OpCodes.Box, method.ReturnType))
		.Create<Func<object>>();

	static DynamicMethod CallDmd(
		Type? args,
		Type ret,
		MethodInfo method,
		Action<ILGenerator>? mapRet = null)
	{
		var argCount = method.GetParameters().Length;
		if (!method.IsStatic) argCount += 1;
	
		if (args is not null && argCount < 1) 
			throw new ArgumentException("method must accept at least one argument");

		var dm = new DynamicMethod(
			name: $"<{method.DeclaringType.FullName}>call_{method.Name}",
			returnType: ret,
			parameterTypes: args is null ? null : [args],
			owner: method.DeclaringType,
			skipVisibility: true);

		var il = dm.GetILGenerator();

		switch (argCount) {
		case 0:	break;
		case 1:
			Type argType = method.IsStatic 
				? method.GetParameters().Single().GetType()
				: method.DeclaringType;
			if (argType.IsByRef) throw new NotImplementedException("by-ref argument in method is not yet implemented");
			bool unbox = false;
			if (argType == typeof(object)) unbox = true;
			else if (argType != args) break;
			il.Emit(OpCodes.Ldarg_0);
			if (argType.IsValueType && unbox) il.Emit(OpCodes.Unbox, argType);
			break;
		case >1:
			int i = 1;
			if (!method.IsStatic) 
				EmitLoadArg(i++, args!, method.DeclaringType, il);
			foreach (var type in method.GetParameters().Select(p => p.ParameterType)) 
				EmitLoadArg(i++, args!, type, il);
			break;
		}

        il.Emit((method.IsStatic || !method.IsVirtual) ? OpCodes.Call : OpCodes.Callvirt, method);
		mapRet?.Invoke(il);
		il.Emit(OpCodes.Ret);

		return dm;
	}

	static void EmitLoadArg(int i, Type source, Type type, ILGenerator il) 
	{
		const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

		if (type.IsByRef) throw new NotImplementedException("by-ref argument in method is not yet implemented");

		il.Emit(OpCodes.Ldarg_0);
		bool boxed = type.IsValueType;

		if (source.GetField($"Item{i}", flags) is FieldInfo fld) {
			bool unbox = false;
			if (fld.FieldType == typeof(object)) unbox = true;
			else if (fld.FieldType != type) goto @continue;
			
			il.Emit(OpCodes.Ldfld, fld);
			if (boxed && unbox) il.Emit(OpCodes.Unbox_Any, type);
			return;
		}

		@continue:
		if (source.GetProperty($"Item{i}", flags) is { CanRead: true } prt) {
			bool unbox = false;
			if (prt.PropertyType == typeof(object)) unbox = true;
			else if (prt.PropertyType != type) goto @throw;
			
			var getter = prt.GetGetMethod();
			il.EmitCall(
				opcode: getter.IsVirtual ? OpCodes.Call : OpCodes.Callvirt,
				methodInfo: getter,
				[]);
			
			if (boxed && unbox) il.Emit(OpCodes.Unbox_Any, type);
			return;
		}
		
		@throw:
		throw new ArgumentException($"type {source} does not contain a field or readable property with name 'Item{i}' and type {type} or {typeof(object)}");
	}
}
