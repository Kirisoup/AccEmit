using System.Reflection;
using System.Reflection.Emit;

namespace AccEmit;

public static partial class Emit
{
	public static Func<Inst, Val> Ldfld<Inst, Val>(FieldInfo field) {
		if (field.DeclaringType != typeof(Inst)) 
			throw new ArgumentException($"field is not declared in type {typeof(Inst)}", nameof(field));
		if (field.FieldType != typeof(Val)) 
			throw new ArgumentException($"field is not of type {typeof(Val)}", nameof(field));
		return LdfldDmd<Inst, Val>(field);
	}

	public static Func<object, Val> LdfldBoxVal<Val>(FieldInfo field) {
		if (field.FieldType != typeof(Val)) 
			throw new ArgumentException($"field is not of type {typeof(Val)}", nameof(field));
		return LdfldDmd<object, Val>(field,
			mapInst: !field.DeclaringType.IsValueType ? null :
				il => il.Emit(OpCodes.Unbox_Any, field.DeclaringType));
	}

	public static Func<Inst, object> LdfldBoxInst<Inst>(FieldInfo field) {
		if (field.DeclaringType != typeof(Inst)) 
			throw new ArgumentException($"field is not declared in type {typeof(Inst)}", nameof(field));
		return LdfldDmd<Inst, object>(field,
			mapRet: !field.FieldType.IsValueType ? null: 
				il => il.Emit(OpCodes.Box, field.FieldType));
	}

	public static Func<object, object> LdfldBox(FieldInfo field) => LdfldDmd<object, object>(
		field,
		mapInst: !field.DeclaringType.IsValueType ? null : 
			il => il.Emit(OpCodes.Unbox_Any, field.DeclaringType),
		mapRet: !field.FieldType.IsValueType ? null: 
			il => il.Emit(OpCodes.Box, field.FieldType));

	static Func<Inst, Ret> LdfldDmd<Inst, Ret>(FieldInfo fi,
		Action<ILGenerator>? mapInst = null,
		Action<ILGenerator>? mapRet = null) 
	{
		if (fi.IsStatic) throw new ArgumentException($"cannot call ldfld on static field");
		var dm = new DynamicMethod(
			name: $"<{fi.DeclaringType.FullName}>ldfld_{fi.Name}",
			returnType: typeof(Ret),
			parameterTypes: [typeof(Inst)],
			owner: fi.DeclaringType,
			skipVisibility: true);
		var il = dm.GetILGenerator();
		il.Emit(OpCodes.Ldarg_0);
		mapInst?.Invoke(il);
		il.Emit(OpCodes.Ldfld, fi);
		mapRet?.Invoke(il);
		il.Emit(OpCodes.Ret);
		return dm.Create<Func<Inst, Ret>>();
	}
}