using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CodingRange.Steam.WebAPI
{
	public class JITEngine
	{
		static JITEngine()
		{
			var assemblyName = new AssemblyName("CodingRange.Steam.WebAPI.JIT");
			var appDomain = AppDomain.CurrentDomain;
			assemblyBuilder = appDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave, @"C:\Users\Yaakov\Development\CodingRange.Steam.WebAPI\CodingRange.Steam.WebAPI\bin\Debug");

			// Stolen from Steam4Net (JIT version)
#if DEBUG
			Type daType = typeof(DebuggableAttribute);
			ConstructorInfo daCtor = daType.GetConstructor(new Type[] { typeof(DebuggableAttribute.DebuggingModes) });
			CustomAttributeBuilder daBuilder = new CustomAttributeBuilder(daCtor, new object[]
			{ 
				DebuggableAttribute.DebuggingModes.DisableOptimizations | 
				DebuggableAttribute.DebuggingModes.Default
			});
			assemblyBuilder.SetCustomAttribute(daBuilder);
#endif
			moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name, emitSymbolInfo: true);
		}

		private static AssemblyBuilder assemblyBuilder;
		private static ModuleBuilder moduleBuilder;

		public static TInterface GetInterface<TInterface>()
		{
			return (TInterface)GetInterface(typeof(TInterface));
		}
		
		static object GetInterface(Type interfaceType)
		{
			if (!interfaceType.IsInterface)
			{
				throw new ArgumentException("TInterface is not an interface");
			}

			var className = string.Format("JITImpl_{0}", interfaceType.Name);

			// See if it already exists
			var jitImplClass = moduleBuilder.Assembly.GetType(className);
			if (jitImplClass == null)
			{
				// Fine, emit it
				jitImplClass = EmitJITImplementation(className, interfaceType);
			}

			var instance = Activator.CreateInstance(jitImplClass);

			return instance;
		}

		static Type EmitJITImplementation(string className, Type interfaceType)
		{
			var typeBuilder = moduleBuilder.DefineType(className, TypeAttributes.Class, typeof(APIBase));
			typeBuilder.AddInterfaceImplementation(interfaceType);

			foreach (var method in interfaceType.GetMethods())
			{
				ProcessClassMethod(typeBuilder, method, interfaceType.Name);
			}

			return typeBuilder.CreateType();
		}

		static void ProcessClassMethod(TypeBuilder typeBuilder, MethodInfo method, string interfaceName)
		{
			var callInfo = method.GetCustomAttribute<SteamAPICallAttribute>();
			if (callInfo == null)
			{
				throw new InvalidOperationException(string.Format("Method '{0}' is missing a {1} attribute", method.Name, typeof(SteamAPICallAttribute).Name));
			}

			var returnType = method.ReturnType;

			// If the return type is a Task<T>, make this method async
			var isAsync = (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>));

			EmitRunMethod(typeBuilder, method, callInfo, interfaceName, isAsync);
		}

		static MethodBuilder EmitClassMethodBase(TypeBuilder typeBuilder, MethodInfo method)
		{
			var methodBuilder = typeBuilder.DefineMethod(method.Name, MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual);

			methodBuilder.SetReturnType(method.ReturnType);
			methodBuilder.SetParameters(method.GetParameters().Select(x => x.ParameterType).ToArray());
			
			return methodBuilder;
		}

		static void EmitRunMethod(TypeBuilder typeBuilder, MethodInfo method, SteamAPICallAttribute callInfo, string interfaceName, bool async)
		{
			var methodBuilder = EmitClassMethodBase(typeBuilder, method);
			typeBuilder.DefineMethodOverride(methodBuilder, method);
			var il = methodBuilder.GetILGenerator();
			
			var dictBuilder = il.DeclareLocal(typeof(Dictionary<string, object>));
			dictBuilder.SetLocalSymInfo("params");

			// Create the params dictionary and store it in location 0
			var dictConstructor = typeof(Dictionary<string, object>).GetConstructor(new Type[] { });
			il.Emit(OpCodes.Newobj, dictConstructor);
			il.Emit(OpCodes.Stloc_0);

			// For each parameter, add it to the dictionary.
			// The key is the name of the param
			var methodParams = method.GetParameters();
			var dictAddMethod = typeof(Dictionary<string, object>).GetMethod("Add");
			for (int i = 0; i < methodParams.Count(); i++)
			{
				var param = methodParams[i];

				il.Emit(OpCodes.Ldloc_0);
				il.Emit(OpCodes.Ldstr, param.Name);
				il.Emit(OpCodes.Ldarg, i + 1);

				// Scalars need to be boxed
				if (!param.ParameterType.IsClass)
				{
					il.Emit(OpCodes.Box, param.ParameterType);
				}

				il.Emit(OpCodes.Call, dictAddMethod);
				il.Emit(OpCodes.Nop);
			}

			// Set up parameters for calling the run method
			il.Emit(OpCodes.Ldc_I4, (int)callInfo.Method);
			il.Emit(OpCodes.Ldstr, interfaceName);
			il.Emit(OpCodes.Ldstr, callInfo.Name);
			il.Emit(OpCodes.Ldc_I4_S, callInfo.Version);
			il.Emit(OpCodes.Ldloc_0); // parameters dictionary

			// If we're emitting an async method, we want to call RunAsync internally.
			// Otherwise, call Run.
			var runMethodName = async ? "RunAsync" : "Run";

			// If we're async, make RunAsync generic for the inner type of the return type (Task<TResult) like so:
			// Task<TResult> RunAsync<TResult>(...) - NOT Task<TResult> RunAsync<Task<TResult>>(...)
			// If we're not async, just make it generic for the return type like so:
			// TResult Run<TResult>(...)
			var runMethodType = async ? method.ReturnType.GetGenericArguments().Single() : method.ReturnType;

			// Call the parent class's protected Run* method
			var baseMethod = typeof(APIBase).GetMethod(runMethodName, BindingFlags.NonPublic | BindingFlags.Static);
			baseMethod = baseMethod.MakeGenericMethod(runMethodType);
			il.Emit(OpCodes.Call, baseMethod);
			
			// Since what we want to return is already where we need it on the evaluation stack, just return.
			il.Emit(OpCodes.Ret);
		}
	}
}
