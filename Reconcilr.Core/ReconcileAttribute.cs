using System;

namespace Reconcilr;

[AttributeUsage(AttributeTargets.Field)]
public sealed class ReconcileAttribute : Attribute
{
}