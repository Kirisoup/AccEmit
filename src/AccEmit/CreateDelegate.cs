using System.Reflection;
using System.Reflection.Emit;

namespace AccEmit;

public static partial class Emit
{
	extension (MethodInfo method) 
	{
		public F CreateDelegate<F>() where F: Delegate => (F)method.CreateDelegate(typeof(F));
	}
}