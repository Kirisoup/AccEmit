using System.Reflection;
using System.Reflection.Emit;

namespace AccEmit;

public static partial class Emit
{
	extension (FieldInfo field) 
	{
		public FuncRef<Inst, Val> EmitLoadAddr<Inst, Val>() {
			if (field.DeclaringType != typeof(Inst)) 
				throw new ArgumentException($"field is not declared in type {typeof(Inst)}", nameof(field));
			if (field.FieldType != typeof(Val)) 
				throw new ArgumentException($"field is not of type {typeof(Val)}", nameof(field));
			return LdfldaDm<Inst, Val>(field);
		}

		public FuncRef<object, Val> EmitLoadAddrBoxAcc<Val>() {
			if (field.FieldType != typeof(Val)) 
				throw new ArgumentException($"field is not of type {typeof(Val)}", nameof(field));
			return LdfldaDm<object, Val>(field,
				mapInst: !field.DeclaringType.IsValueType ? null :
					il => il.Emit(OpCodes.Unbox_Any, field.DeclaringType));
		}
	}

	public delegate ref Ret FuncRef<Inst, Ret>(Inst instance);

	static FuncRef<Inst, Ret> LdfldaDm<Inst, Ret>(FieldInfo fi,
		Action<ILGenerator>? mapInst = null) 
	{
		if (fi.IsStatic) throw new ArgumentException($"cannot call ldfld on static field");
		var dm = new DynamicMethod(
			name: $"<{fi.DeclaringType.FullName}>ldflda_{fi.Name}",
			returnType: typeof(Ret).MakeByRefType(),
			parameterTypes: [typeof(Inst)],
			owner: fi.DeclaringType,
			skipVisibility: true);
		var il = dm.GetILGenerator();
		il.Emit(OpCodes.Ldarg_0);
		mapInst?.Invoke(il);
		il.Emit(OpCodes.Ldflda, fi);
		il.Emit(OpCodes.Ret);
		return dm.Create<FuncRef<Inst, Ret>>();
	}
}