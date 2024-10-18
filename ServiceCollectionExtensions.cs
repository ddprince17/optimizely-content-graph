using System.Reflection;
using System.Reflection.Emit;
using EPiServer.ContentApi.Core.Serialization;
using EPiServer.ContentApi.Core.Serialization.Models;
using EPiServer.Scheduler;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ServiceDescriptor = Microsoft.Extensions.DependencyInjection.ServiceDescriptor;

namespace Example;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection CustomizeContentGraphContentConverter(this IServiceCollection services)
    {
        // We look at the service name because the type is internal, and we can't reference it directly.
        var existingServiceDescriptor =
            services.SingleOrDefault(descriptor => descriptor.ServiceType.Name.Equals("ContentGraphContentConverter", StringComparison.OrdinalIgnoreCase));

        // An assembly name and public key "known" by the "ContentGraphContentConverter" needs to be used here, otherwise we get an unauthorized access exception.
        var contentGraphServiceType = existingServiceDescriptor!.ServiceType;
        var contentGraphConstructor = contentGraphServiceType.GetConstructors().FirstOrDefault(info => info.GetParameters().Length > 6);
        var assemblyName = new AssemblyName(
            "DynamicProxyGenAssembly2, PublicKey=0024000004800000940000000602000000240000525341310004000001000100c547cac37abd99c8db225ef2f6c8a3602f3b3606cc9891605d02baa56104f4cfc0734aa39b93bf7852f7d9266654753cc297e7d2edfe0bac1cdcf9f717241550e0a7b191195b7667bb4f64bcb8e2121380fd1d9d46ad2d92d2d15605093924cceaf74c4861eff62abf69b9291ed0a340e113be11e6a7d3113e92484cf7045cc7");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
        var typeBuilder = moduleBuilder.DefineType("CustomContentGraphContentConverter",
            TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout,
            contentGraphServiceType);
        var ctorBuilder = typeBuilder.DefineConstructor(
            MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName |
            MethodAttributes.RTSpecialName, CallingConventions.Standard | CallingConventions.HasThis,
            contentGraphConstructor!.GetParameters().Select(info => info.ParameterType).ToArray());
        var ctorIlGenerator = ctorBuilder.GetILGenerator();

        // This is the equivalent of creating a constructor having the same parameters as ContentGraphContentConverter and then calling its base with it. 
        ctorIlGenerator.Emit(OpCodes.Ldarg_0);
        ctorIlGenerator.Emit(OpCodes.Ldarg_1);
        ctorIlGenerator.Emit(OpCodes.Ldarg_2);
        ctorIlGenerator.Emit(OpCodes.Ldarg_3);
        ctorIlGenerator.Emit(OpCodes.Ldarg_S, 4);
        ctorIlGenerator.Emit(OpCodes.Ldarg_S, 5);
        ctorIlGenerator.Emit(OpCodes.Ldarg_S, 6);
        ctorIlGenerator.Emit(OpCodes.Ldarg_S, 7);
        ctorIlGenerator.Emit(OpCodes.Ldarg_S, 8);
        ctorIlGenerator.Emit(OpCodes.Ldarg_S, 9);
        ctorIlGenerator.Emit(OpCodes.Ldarg_S, 10);
        ctorIlGenerator.Emit(OpCodes.Ldarg_S, 11);
        ctorIlGenerator.Emit(OpCodes.Call, contentGraphConstructor);
        ctorIlGenerator.Emit(OpCodes.Ret);

        var convertMethodBuilder = typeBuilder.DefineMethod("Convert",
            MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig, typeof(ContentApiModel),
            [typeof(IContent), typeof(ConverterContext)]);
        var convertIlGenerator = convertMethodBuilder.GetILGenerator();

        // This is the equivalent of calling the base method with the same parameters, then the extension method "AddReadonlyProperties".
        convertIlGenerator.Emit(OpCodes.Ldarg_0);
        convertIlGenerator.Emit(OpCodes.Ldarg_1);
        convertIlGenerator.Emit(OpCodes.Ldarg_2);
        convertIlGenerator.Emit(OpCodes.Call, contentGraphServiceType.GetMethod("Convert")!);
        convertIlGenerator.Emit(OpCodes.Ldarg_1);
        convertIlGenerator.Emit(OpCodes.Call, typeof(ContentApiModelExtensions).GetMethod(nameof(ContentApiModelExtensions.AddReadonlyProperties))!);
        convertIlGenerator.Emit(OpCodes.Ret);

        var dynamicType = typeBuilder.CreateType();

        // This removes the existing "ContentGraphContentConverter" service from the IoC container and replaces it with the dynamic type.
        services.Remove(existingServiceDescriptor);
        services.AddTransient(dynamicType);
        typeof(ServiceCollectionExtensions).GetMethod(nameof(ForwardDynamicType), BindingFlags.Static | BindingFlags.NonPublic)!
            .MakeGenericMethod(dynamicType, contentGraphServiceType).Invoke(null, [services]);

        return services;
    }

    /// <summary>
    /// Helper function that is solely used via reflection to forward the dynamic type to the existing type.
    /// </summary>
    private static IServiceCollection ForwardDynamicType<TExisting, TAdditional>(this IServiceCollection services) where TExisting : class where TAdditional : class
    {
        return services.Forward<TExisting, TAdditional>();
    }
}