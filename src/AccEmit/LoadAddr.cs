using System.Reflection;
using System.Reflection.Emit;

namespace AccEmit;

public delegate ref Ret RefFunc<Inst, Ret>(Inst instance);

public static partial class Emit
{
	extension (FieldInfo field) 
	{
		public RefFunc<Inst, Val> EmitLoadAddr<Inst, Val>() {
			if (field.DeclaringType != typeof(Inst)) 
				throw new ArgumentException($"field is not declared in type {typeof(Inst)}", nameof(field));
			if (field.FieldType != typeof(Val)) 
				throw new ArgumentException($"field is not of type {typeof(Val)}", nameof(field));
			return LdfldaDm<Inst, Val>(field);
		}

		public RefFunc<object, Val> EmitUnboxLoadAddr<Val>() {
			if (field.FieldType != typeof(Val)) 
				throw new ArgumentException($"field is not of type {typeof(Val)}", nameof(field));
			return LdfldaDm<object, Val>(field,
				mapInst: !field.DeclaringType.IsValueType ? null :
					il => il.Emit(OpCodes.Unbox_Any, field.DeclaringType));
		}
	}

	static unsafe RefFunc<Inst, Ret> LdfldaDm<Inst, Ret>(FieldInfo fi,
		Action<ILGenerator>? mapInst = null) 
	{
		if (fi.IsStatic) throw new ArgumentException($"cannot call ldfld on static field");
		var dm = new DynamicMethod(
			name: $"<{fi.DeclaringType.FullName}>ldflda_{fi.Name}",
			returnType: typeof(IntPtr),
			parameterTypes: [typeof(Inst)],
			owner: fi.DeclaringType,
			skipVisibility: true);
		var il = dm.GetILGenerator();
		il.Emit(OpCodes.Ldarg_0);
		mapInst?.Invoke(il);
		il.Emit(OpCodes.Ldflda, fi);
		il.Emit(OpCodes.Conv_I);
		il.Emit(OpCodes.Ret);
		var fn = dm.Create<Func<Inst, IntPtr>>();
#pragma warning disable CS8500
		unsafe { return inst => ref *(Ret*)fn(inst).ToPointer(); }
#pragma warning restore CS8500
	}
}