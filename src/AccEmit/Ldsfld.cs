using System.Reflection;
using System.Reflection.Emit;

namespace AccEmit;

public static partial class AccEmit
{
	public static Func<Val> Ldsfld<Val>(FieldInfo field) {
		if (!field.IsStatic) throw new ArgumentException($"cannot call ldsfld on a static field", nameof(field));
		return LdsfldDmd<Val>(field);
	}

	[Obsolete("shit is unsafe, dont use unless you have to")]
	public static Func<object> LdsfldBox(FieldInfo field) => LdsfldDmd<object>(
		field,
		mapRet: !field.FieldType.IsValueType ? null : 
			il => il.Emit(OpCodes.Box, field.FieldType));

	static Func<Ret> LdsfldDmd<Ret>(FieldInfo fi, Action<ILGenerator>? mapRet = null) {
		if (fi.FieldType != typeof(Ret)) 
			throw new ArgumentException($"field is not of type {typeof(Ret)}");
		var dm = new DynamicMethod(
			name: $"<{fi.DeclaringType.FullName}>ldsfld_{fi.Name}",
			returnType: typeof(Ret),
			parameterTypes: [],
			owner: fi.DeclaringType,
			skipVisibility: true);
		var il = dm.GetILGenerator();
		il.Emit(OpCodes.Ldsfld, fi);
		mapRet?.Invoke(il);
		il.Emit(OpCodes.Ret);
		return dm.Create<Func<Ret>>();
	}
}