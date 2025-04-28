using System.Reflection;
using System.Reflection.Emit;

namespace AccEmit;

public static partial class Emit
{
	// pure sugaring bcz why not
	extension (FieldInfo field) 
	{
		public (Func<Inst, Val> load, Action<Inst, Val> set) EmitPair<Inst, Val>() => (
			field.EmitLoad<Inst, Val>(), field.EmitSet<Inst, Val>());

		public (Func<object, Val> load, Action<object, Val> set) EmitPairBoxAcc<Val>() => (
			field.EmitLoadBoxAcc<Val>(), field.EmitSetBoxAcc<Val>());

		public (Func<Inst, object> load, Action<Inst, object> set) EmitPairBoxRV<Inst>() => (
			field.EmitLoadBoxRet<Inst>(), field.EmitSetBoxVal<Inst>());

		public (Func<object, object> load, Action<object, object> set) EmitPairBox() => (
			field.EmitLoadBox(), field.EmitSetBox());

		public (Func<Val> load, Action<Val> set) EmitStaticLoadSet<Inst, Val>() => (
			field.EmitStaticLoad<Val>(), field.EmitStaticSet<Val>());

		public (Func<object> load, Action<object> set) EmitStaticLoadSetBox() => (
			field.EmitStaticLoadBox(), field.EmitStaticSetBox());
	}
}