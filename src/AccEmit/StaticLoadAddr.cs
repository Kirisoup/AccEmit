using System.Reflection;
using System.Reflection.Emit;

namespace AccEmit;

public delegate ref Ret RefFunc<Ret>();

public static partial class Emit
{
	extension (FieldInfo field) 
	{
		public RefFunc<Val> EmitStaticLoadAddr<Val>() {
			if (field.FieldType != typeof(Val)) 
				throw new ArgumentException($"field is not of type {typeof(Val)}", nameof(field));
			return LdsfldaDm<Val>(field);
		}
	}

	static RefFunc<Ret> LdsfldaDm<Ret>(FieldInfo fi) 
	{
		if (!fi.IsStatic) throw new ArgumentException($"cannot call ldsflda on non static field");
		var dm = new DynamicMethod(
			name: $"<{fi.DeclaringType.FullName}>ldsflda_{fi.Name}",
			returnType: typeof(Ret).MakeByRefType(),
			parameterTypes: [],
			owner: fi.DeclaringType,
			skipVisibility: true);
		var il = dm.GetILGenerator();
		il.Emit(OpCodes.Ldsflda, fi);
		il.Emit(OpCodes.Ret);
		return dm.Create<RefFunc<Ret>>();
	}
}