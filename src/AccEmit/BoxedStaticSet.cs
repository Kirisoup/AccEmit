using System.Reflection;
using System.Reflection.Emit;

namespace AccEmit;

public static partial class Emit
{
	extension (FieldInfo field) 
	{
		public Action<object> EmitBoxedStaticSet() => StsfldDmd<object>(
			field,
			valMap: !field.FieldType.IsValueType ? null : 
				il => il.Emit(OpCodes.Unbox_Any, field.FieldType));		
	}

	static Action<Val> StsfldDmd<Val>(FieldInfo fi, Action<ILGenerator>? valMap = null) {
		if (!fi.IsStatic) throw new ArgumentException($"cannot call ldfld on non static field");
		var dm = new DynamicMethod(
			name: $"<{fi.DeclaringType.FullName}>stsfld_{fi.Name}",
			returnType: typeof(void),
			parameterTypes: [typeof(Val)],
			owner: fi.DeclaringType,
			skipVisibility: true);
		var il = dm.GetILGenerator();
		il.Emit(OpCodes.Ldarg_0);
		valMap?.Invoke(il);
		il.Emit(OpCodes.Stfld, fi);
		il.Emit(OpCodes.Ret);
		return dm.Create<Action<Val>>();
	}
}