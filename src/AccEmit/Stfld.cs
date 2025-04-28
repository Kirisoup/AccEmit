using System.Reflection;
using System.Reflection.Emit;

namespace AccEmit;

public static partial class AccEmit
{
	public static Action<Inst, Val> Stfld<Inst, Val>(FieldInfo field) {
		if (field.DeclaringType != typeof(Inst)) 
			throw new ArgumentException($"field is not declared in type {typeof(Inst)}", nameof(field));
		if (field.FieldType != typeof(Val)) 
			throw new ArgumentException($"field is not of type {typeof(Val)}", nameof(field));
		return StfldDmd<Inst, Val>(field);
	}

	public static Action<Inst, object> StfldBoxInst<Inst>(FieldInfo field) {
		if (field.DeclaringType != typeof(Inst)) 
			throw new ArgumentException($"field is not declared in type {typeof(Inst)}", nameof(field));
		return StfldDmd<Inst, object>(field,
			mapInst: !field.DeclaringType.IsValueType ? null : 
				il => il.Emit(OpCodes.Unbox_Any, field.DeclaringType));
	}

	public static Action<object, Val> StfldBoxVal<Val>(FieldInfo field) {
		if (field.FieldType != typeof(Val)) 
			throw new ArgumentException($"field is not of type {typeof(Val)}", nameof(field));
		return StfldDmd<object, Val>(field,
			mapInst: !field.DeclaringType.IsValueType ? null :
				il => il.Emit(OpCodes.Unbox_Any, field.DeclaringType));
	}

	public static Action<object, object> StfldBox(FieldInfo field) => StfldDmd<object, object>(
		field,
		mapInst: !field.DeclaringType.IsValueType ? null : 
			il => il.Emit(OpCodes.Unbox_Any, field.DeclaringType),
		mapVal: !field.FieldType.IsValueType ? null : 
			il => il.Emit(OpCodes.Unbox_Any, field.FieldType));

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