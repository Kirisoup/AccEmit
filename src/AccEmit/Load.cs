using System.Reflection;
using System.Reflection.Emit;

namespace AccEmit;

public static partial class Emit
{
	extension (FieldInfo field) 
	{
		public Func<Inst, Val> EmitLoad<Inst, Val>() {
			if (field.DeclaringType != typeof(Inst)) 
				throw new ArgumentException($"field is not declared in type {typeof(Inst)}", nameof(field));
			if (field.FieldType != typeof(Val)) 
				throw new ArgumentException($"field is not of type {typeof(Val)}", nameof(field));
			return LdfldDm<Inst, Val>(field);
		}

		public Func<Inst, object> EmitLoadBoxRet<Inst>() {
			if (field.DeclaringType != typeof(Inst)) 
				throw new ArgumentException($"field is not declared in type {typeof(Inst)}", nameof(field));
			return LdfldDm<Inst, object>(field,
				mapRet: !field.FieldType.IsValueType ? null: 
					il => il.Emit(OpCodes.Box, field.FieldType));
		}

		public Func<object, Val> EmitLoadBoxAcc<Val>() {
			if (field.FieldType != typeof(Val)) 
				throw new ArgumentException($"field is not of type {typeof(Val)}", nameof(field));
			return LdfldDm<object, Val>(field,
				mapInst: !field.DeclaringType.IsValueType ? null :
					il => il.Emit(OpCodes.Unbox_Any, field.DeclaringType));
		}

		public Func<object, object> EmitLoadBox() => LdfldDm<object, object>(
			field,
			mapInst: !field.DeclaringType.IsValueType ? null : 
				il => il.Emit(OpCodes.Unbox_Any, field.DeclaringType),
			mapRet: !field.FieldType.IsValueType ? null: 
				il => il.Emit(OpCodes.Box, field.FieldType));
	}

	static Func<Inst, Ret> LdfldDm<Inst, Ret>(FieldInfo fi,
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