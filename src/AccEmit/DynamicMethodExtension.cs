using System.Reflection.Emit;

namespace AccEmit;

internal static class DynamicMethodExtension
{
	extension (DynamicMethod dm) 
	{
		public F Create<F>() where F: Delegate => (F)dm.CreateDelegate(typeof(F));
	}
}
