
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Maintainability",
    "RCS1139:Add summary element to documentation comment.",
    Justification = "Summary is unnecessary if the member just throws an exception.")]
[assembly: SuppressMessage(
    "Design",
    "RCS1194:Implement exception constructors.",
    Justification = "Why?")]
[assembly: SuppressMessage(
    "Performance",
    "RCS1096:Use bitwise operation instead of calling 'HasFlag'.",
    Justification = "Unnecessicary in latest .NET Core versions.")]