using System;

namespace ToughnessSkillMod;

/// <summary>
/// Local replacement for vanilla's <c>Exception.IsFatal()</c> extension, which does not
/// exist in 1.7's Assembly-CSharp — used to keep Harmony postfixes and the test harness
/// from swallowing process-level faults in <c>catch</c> filters.
/// </summary>
internal static class ExceptionUtil
{
    /// <summary>
    /// True for exceptions that must never be caught-and-logged: they indicate VM/process
    /// corruption rather than a gameplay-level failure.
    /// </summary>
    /// <remarks>
    /// <c>AccessViolationException</c> is included because Waylith sets
    /// <c>CatchableAccessViolations</c> — AVs surface as managed exceptions and would
    /// otherwise be silently deduped by <c>ErrorOnce</c>. <c>ThreadAbortException</c>
    /// doesn't exist on CoreCLR (no Thread.Abort), so it can't appear here.
    /// </remarks>
    public static bool IsFatal(this Exception e) =>
        e is OutOfMemoryException or StackOverflowException or AccessViolationException;
}
