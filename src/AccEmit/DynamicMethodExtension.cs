using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace AccEmit;

internal static class DynamicMethodExtension
{
	extension (DynamicMethod dm) 
	{
		public F Create<F>() where F: Delegate => (F)dm.CreateDelegate(typeof(F));
	}
}