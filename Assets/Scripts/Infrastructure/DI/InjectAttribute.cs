using System;

/// <summary>
/// 标记需要由 ServiceContainer 自动注入的字段或属性。
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public sealed class InjectAttribute : Attribute
{
}