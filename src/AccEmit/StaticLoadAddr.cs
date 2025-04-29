using System.Reflection;
using System.Reflection.Emit;

namespace AccEmit;

public static partial class Emit
{
	extension (FieldInfo field) 
	{
		public FuncRef<Val> EmitStaticLoadAddr<Val>() {
			if (field.FieldType != typeof(Val)) 
				throw new ArgumentException($"field is not of type {typeof(Val)}", nameof(field));
			return LdsfldaDm<Val>(field);
		}
	}

	public delegate ref Ret FuncRef<Ret>();

	static FuncRef<Ret> LdsfldaDm<Ret>(FieldInfo fi) 
	{
		if (!fi.IsStatic) throw new ArgumentException($"cannot call ldfld on non static field");
		var dm = new DynamicMethod(
			name: $"<{fi.DeclaringType.FullName}>ldsflda_{fi.Name}",
			returnType: typeof(Ret).MakeByRefType(),
			parameterTypes: [],
			owner: fi.DeclaringType,
			skipVisibility: true);
		var il = dm.GetILGenerator();
		il.Emit(OpCodes.Ldsflda, fi);
		il.Emit(OpCodes.Ret);
		return dm.Create<FuncRef<Ret>>();
	}
}