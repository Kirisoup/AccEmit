using System.Reflection;
using System.Reflection.Emit;

namespace AccEmit;

public static partial class Emit
{
	extension (FieldInfo field) {
		public Func<Val> EmitStaticLoad<Val>() {
			if (!field.IsStatic) throw new ArgumentException($"cannot call ldsfld on a static field", nameof(field));
			return LdsfldDm<Val>(field);
		}

		public Func<object> EmitStaticLoadBox() => LdsfldDm<object>(
			field,
			mapRet: !field.FieldType.IsValueType ? null : 
				il => il.Emit(OpCodes.Box, field.FieldType));
	}

	static Func<Ret> LdsfldDm<Ret>(FieldInfo fi, Action<ILGenerator>? mapRet = null) {
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