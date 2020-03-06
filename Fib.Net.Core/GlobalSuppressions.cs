
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
    Justification = "I see no reason for this to be necessary.")]
[assembly: SuppressMessage(
    "Performance",
    "RCS1096:Use bitwise operation instead of calling 'HasFlag'.",
    Justification = "Unnecessicary in latest .NET Core versions.")]
[assembly: SuppressMessage(
    "Formatting",
    "RCS1029:Format binary operator on next line.",
    Justification = "Style choice I disagree with.")]
[assembly: SuppressMessage(
    "Usage",
    "CA2227:Collection properties should be read only",
    Justification = "Does not work with existing Jib code.")]
[assembly: SuppressMessage(
    "Design",
    "CA1034:Nested types should not be visible",
    Justification = "Disagree")]
[assembly: SuppressMessage(
    "Design",
    "CA1062:Validate arguments of public methods",
    Justification = "Causes false positives.")]
[assembly: SuppressMessage("Naming",
    "CA1710:Identifiers should have correct suffix",
    Justification = "Requried suffixes do not help with readability.")]
