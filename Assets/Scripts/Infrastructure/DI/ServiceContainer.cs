using System;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// 服务容器：管理所有基础设施服务的注册与解析。
/// 在 Bootstrap 流程中创建实例，通过构造函数或方法参数注入到各模块。
/// 非静态、非单例——仅作为轻量级的服务注册表，生命周期由调用方控制。
/// </summary>
public class ServiceContainer
{
    private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
    private static readonly BindingFlags _injectFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public void Register<T>(T service) where T : class
    {
        Type type = typeof(T);
        if (_services.ContainsKey(type))
            throw new InvalidOperationException($"服务 {type.Name} 已被注册，请勿重复注册。");
        _services[type] = service;
    }

    public T Resolve<T>() where T : class
    {
        if (_services.TryGetValue(typeof(T), out var service))
            return service as T;
        throw new InvalidOperationException($"服务 {typeof(T).Name} 尚未注册，请先通过 Register 注册。");
    }

    public T TryResolve<T>() where T : class
    {
        if (_services.TryGetValue(typeof(T), out var service))
            return service as T;
        return null;
    }

    public bool IsRegistered<T>() where T : class
    {
        return _services.ContainsKey(typeof(T));
    }

    /// <summary>
    /// 扫描 target 上所有标记 [Inject] 的字段/属性，从容器中注入对应类型的服务实例。
    /// 返回值标记是否全部注入成功（未注册的类型跳过，不抛异常）。
    /// </summary>
    public bool TryInject(object target)
    {
        if (target == null) return false;
        var type = target.GetType();
        bool allResolved = true;

        // 字段注入
        var fields = type.GetFields(_injectFlags);
        for (int i = 0; i < fields.Length; i++)
        {
            var field = fields[i];
            if (!field.IsDefined(typeof(InjectAttribute), false)) continue;
            var fieldType = field.FieldType;
            if (_services.TryGetValue(fieldType, out var service))
            {
                field.SetValue(target, service);
            }
            else
            {
                allResolved = false;
            }
        }

        // 属性注入
        var props = type.GetProperties(_injectFlags);
        for (int i = 0; i < props.Length; i++)
        {
            var prop = props[i];
            if (!prop.IsDefined(typeof(InjectAttribute), false)) continue;
            if (!prop.CanWrite) continue;
            var propType = prop.PropertyType;
            if (_services.TryGetValue(propType, out var service))
            {
                prop.SetValue(target, service);
            }
            else
            {
                allResolved = false;
            }
        }

        return allResolved;
    }

    public void DisposeAll()
    {
        foreach (var kvp in _services)
        {
            if (kvp.Value is IDisposable disposable)
                disposable.Dispose();
        }
        _services.Clear();
    }
}