using System.Reflection;
using System.Reflection.Emit;

namespace AccEmit;

public static partial class Emit
{
	extension (FieldInfo field) 
	{
		public Action<Inst, object> EmitBoxedSet<Inst>() {
			if (field.DeclaringType != typeof(Inst)) 
				throw new ArgumentException($"field is not declared in type {typeof(Inst)}", nameof(field));
			return StfldDmd<Inst, object>(field,
				mapVal: !field.DeclaringType.IsValueType ? null : 
					il => il.Emit(OpCodes.Unbox_Any, field.FieldType));
		}

		public Action<object, object> EmitBoxedSet() => StfldDmd<object, object>(
			field,
			mapInst: !field.DeclaringType.IsValueType ? null : 
				il => il.Emit(OpCodes.Unbox_Any, field.DeclaringType),
			mapVal: !field.FieldType.IsValueType ? null : 
				il => il.Emit(OpCodes.Unbox_Any, field.FieldType));

	}

	static Action<Inst, Val> StfldDmd<Inst, Val>(FieldInfo fi,
		Action<ILGenerator>? mapInst = null,
		Action<ILGenerator>? mapVal = null) 
	{
		if (fi.IsStatic) throw new ArgumentException($"cannot call stfld on a static field");
		var dm = new DynamicMethod(
			name: $"<{fi.DeclaringType.FullName}>stfld_{fi.Name}",
			returnType: typeof(void),
			parameterTypes: [typeof(Inst), typeof(Val)],
			owner: fi.DeclaringType,
			skipVisibility: true);
		var il = dm.GetILGenerator();
		il.Emit(OpCodes.Ldarg_0);
		mapInst?.Invoke(il);
		il.Emit(OpCodes.Ldarg_1);
		mapVal?.Invoke(il);
		il.Emit(OpCodes.Stfld, fi);
		il.Emit(OpCodes.Ret);
		return dm.Create<Action<Inst, Val>>();
	}
}